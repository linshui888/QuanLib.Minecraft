﻿using log4net.Core;
using log4net.Repository.Hierarchy;
using QuanLib.Core;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuanLib.Minecraft.Command.Senders
{
    public class RconOnewayCommandSender : UnmanagedRunnable, IOnewayCommandSender
    {
        public RconOnewayCommandSender(IPAddress address, ushort port, string password, Func<Type, LogImpl> logger, int clientCount = 6) : base(logger)
        {
            if (address is null)
                throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException($"“{nameof(password)}”不能为 null 或空。", nameof(password));
            ThrowHelper.ArgumentOutOfMin(0, clientCount, nameof(clientCount));

            _logger = logger;
            _address = address;
            _port = port;
            _password = password;
            _clientCount = clientCount;
            _clients = new();
            _synchronized = new();
            _index = 0;
            _id = -1;

            IsConnected = false;
        }

        private readonly Func<Type, LogImpl> _logger;

        private readonly IPAddress _address;

        private readonly ushort _port;

        private readonly string _password;

        private readonly int _clientCount;

        private readonly List<RconClient> _clients;

        private readonly Synchronized _synchronized;

        private int _index;

        private int _id;

        public bool IsConnected { get; private set; }

        protected override void Run()
        {
            Task[] tasks = new Task[_clientCount];
            for (int i = 0; i < _clientCount; i++)
            {
                RconClient client = new(_address, _port, _password, _logger);
                client.Start("RconClient Thread #" + i);
                _clients.Add(client);
                tasks[i] = client.WaitForStopAsync();
            }

            Task.WaitAll(tasks);
        }

        protected override void DisposeUnmanaged()
        {
            foreach (var client in _clients)
                client.Stop();
            _clients.Clear();
        }

        public void SendOnewayCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException($"“{nameof(command)}”不能为 null 或空。", nameof(command));

            byte[] packet = ToPacket(GetNextIndex(), 2, command);
            _synchronized.Invoke(() => _clients[GetNextIndex()].SendPacket(packet));
        }

        public async Task SendOnewayCommandAsync(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException($"“{nameof(command)}”不能为 null 或空。", nameof(command));

            byte[] packet = ToPacket(GetNextIndex(), 2, command);
            await _synchronized.InvokeAsync(() => _clients[GetNextIndex()].SendPacketAsync(packet));
        }

        public void SendOnewayBatchCommand(IEnumerable<string> commands)
        {
            if (commands is null)
                throw new ArgumentNullException(nameof(commands));

            ConcurrentBag<byte[]> packets = ToPacketBag(commands);
            _synchronized.Invoke(() => Task.WaitAll(HandleAllCommand(packets)));
        }

        public async Task SendOnewayBatchCommandAsync(IEnumerable<string> commands)
        {
            if (commands is null)
                throw new ArgumentNullException(nameof(commands));

            ConcurrentBag<byte[]> packets = ToPacketBag(commands);
            await _synchronized.InvokeAsync(() => Task.WhenAll(HandleAllCommand(packets)));
        }

        public void SendOnewayBatchSetBlock(IEnumerable<ISetBlockArgument> arguments)
        {
            if (arguments is null)
                throw new ArgumentNullException(nameof(arguments));

            ConcurrentBag<byte[]> packets = ToPacketBag(arguments);
            _synchronized.Invoke(() => Task.WaitAll(HandleAllCommand(packets)));
        }

        public async Task SendOnewayBatchSetBlockAsync(IEnumerable<ISetBlockArgument> arguments)
        {
            if (arguments is null)
                throw new ArgumentNullException(nameof(arguments));

            ConcurrentBag<byte[]> packets = ToPacketBag(arguments);
            await _synchronized.InvokeAsync(() => Task.WhenAll(HandleAllCommand(packets)));
        }

        public void WaitForResponse()
        {
            _clients[GetNextIndex()].SendPacket(ToPacket(GetNextID(), 2, "time query gametime"));
        }

        public async Task WaitForResponseAsync()
        {
            await _clients[GetNextIndex()].SendPacketAsync(ToPacket(GetNextID(), 2, "time query gametime"));
        }

        private int GetNextID()
        {
            return Interlocked.Decrement(ref _id);
        }

        private int GetNextIndex()
        {
            _index++;
            if (_index >= _clients.Count)
                _index = 0;
            return _index;
        }

        private static byte[] ToPacket(int id, int type, string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body + "\0");
            int bodyLength = bytes.Length;

            using var packet = new MemoryStream(12 + bodyLength);
            packet.Write(BitConverter.GetBytes(9 + bodyLength), 0, 4);
            packet.Write(BitConverter.GetBytes(id), 0, 4);
            packet.Write(BitConverter.GetBytes(type), 0, 4);
            packet.Write(bytes, 0, bodyLength);
            packet.Write(new byte[] { 0 }, 0, 1);

            return packet.ToArray();
        }

        private ConcurrentBag<byte[]> ToPacketBag(IEnumerable<string> commands)
        {
            ConcurrentBag<byte[]> result = new();
            ParallelLoopResult parallelLoopResult = Parallel.ForEach(commands, command =>
            {
                result.Add(ToPacket(GetNextID(), 2, command));
            });

            while (!parallelLoopResult.IsCompleted)
                Thread.Yield();

            return result;
        }

        private ConcurrentBag<byte[]> ToPacketBag(IEnumerable<ISetBlockArgument> arguments)
        {
            ConcurrentBag<byte[]> result = new();
            ParallelLoopResult parallelLoopResult = Parallel.ForEach(arguments, argument =>
            {
                result.Add(ToPacket(GetNextID(), 2, argument.ToSetBlockCommand()));
            });

            while (!parallelLoopResult.IsCompleted)
                Thread.Yield();

            return result;
        }

        private Task[] HandleAllCommand(ConcurrentBag<byte[]> packets)
        {
            foreach (var packet in packets)
                _clients[GetNextIndex()].EnqueuePacket(packet);

            Task[] tasks = new Task[_clients.Count];
            for (int i = 0; i < _clients.Count; i++)
                tasks[i] = _clients[i].SendQueuePacketAsync();

            return tasks;
        }

        private class RconClient : UnmanagedRunnable
        {
            public RconClient(IPAddress address, ushort port, string password, Func<Type, LogImpl> logger) : base(logger)
            {
                if (address is null)
                    throw new ArgumentNullException(nameof(address));
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException($"“{nameof(password)}”不能为 null 或空。", nameof(password));

                _client = new();
                _client.Connect(address, port);
                _stream = _client.GetStream();
                _buffer = new byte[4096];
                _commands = new();
                _send = new(0);
                _done = new(0);
                SendPacket(ToPacket(0, 3, password));
            }

            private readonly TcpClient _client;

            private readonly NetworkStream _stream;

            private readonly byte[] _buffer;

            private readonly Queue<byte[]> _commands;

            private readonly SemaphoreSlim _send;

            private readonly SemaphoreSlim _done;

            protected override void Run()
            {
                while (IsRunning)
                {
                    _send.Wait();

                    while (_commands.Count > 0)
                    {
                        SendPacket(_commands.Dequeue());
                    }

                    _done.Release();
                }
            }

            protected override void DisposeUnmanaged()
            {
                _client.Dispose();
            }

            public void EnqueuePacket(byte[] command)
            {
                _commands.Enqueue(command);
            }

            public void SendPacket(byte[] command)
            {
                _stream.Write(command);
                _stream.Read(_buffer);
            }

            public async Task SendPacketAsync(byte[] packet)
            {
                await _stream.WriteAsync(packet);
                await _stream.ReadAsync(_buffer);
            }

            public async Task SendQueuePacketAsync()
            {
                _send.Release();
                await _done.WaitAsync();
            }
        }
    }
}
