﻿using log4net.Core;
using QuanLib.Minecraft.BlockScreen.Event;
using QuanLib.Minecraft.BlockScreen.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.BlockScreen
{
    public class ApplicationManager
    {
        private static readonly LogImpl LOGGER = LogUtil.MainLogger;

        public ApplicationManager()
        {
            Items = new(this);
            AddedApplication += OnAddedApplication;
            //RemovedApplication += OnRemovedApplication;
        }

        public ApplicationCollection Items { get; }

        public event EventHandler<ApplicationManager, ApplicationInfoEventArgs> AddedApplication;

        //public event EventHandler<ApplicationManager, ApplicationInfoEventArgs> RemovedApplication;

        protected virtual void OnAddedApplication(ApplicationManager sender, ApplicationInfoEventArgs e)
        {
            string dir = MCOS.MainDirectory.Applications.GetApplicationDirectory(e.ApplicationInfo.ID);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            LOGGER.Info($"应用程序“{e.ApplicationInfo}”已加载");
        }

        //protected virtual void OnRemovedApplication(ApplicationManager sender, ApplicationInfoEventArgs e) { }

        public class ApplicationCollection : IDictionary<string, ApplicationInfo>
        {
            public ApplicationCollection(ApplicationManager owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _items = new();
            }

            private readonly ApplicationManager _owner;

            private readonly ConcurrentDictionary<string, ApplicationInfo> _items;

            public ApplicationInfo this[string id] => _items[id];

            ApplicationInfo IDictionary<string, ApplicationInfo>.this[string key] { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public ICollection<string> Keys => _items.Keys;

            public ICollection<ApplicationInfo> Values => _items.Values;

            public int Count => _items.Count;

            public bool IsReadOnly => false;

            public void Add(ApplicationInfo applicationInfo)
            {
                if (applicationInfo is null)
                    throw new ArgumentNullException(nameof(applicationInfo));

                _items.TryAdd(applicationInfo.ID, applicationInfo);
                _owner.AddedApplication.Invoke(_owner, new(applicationInfo));
            }

            public bool ContainsKey(string key)
            {
                return _items.ContainsKey(key);
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out ApplicationInfo value)
            {
                return _items.TryGetValue(key, out value);
            }

            public IEnumerator<KeyValuePair<string, ApplicationInfo>> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_items).GetEnumerator();
            }
            void ICollection<KeyValuePair<string, ApplicationInfo>>.Add(KeyValuePair<string, ApplicationInfo> item)
            {
                ((ICollection<KeyValuePair<string, ApplicationInfo>>)_items).Add(item);
            }

            bool ICollection<KeyValuePair<string, ApplicationInfo>>.Remove(KeyValuePair<string, ApplicationInfo> item)
            {
                return ((ICollection<KeyValuePair<string, ApplicationInfo>>)_items).Remove(item);
            }

            bool ICollection<KeyValuePair<string, ApplicationInfo>>.Contains(KeyValuePair<string, ApplicationInfo> item)
            {
                return ((ICollection<KeyValuePair<string, ApplicationInfo>>)_items).Contains(item);
            }

            void ICollection<KeyValuePair<string, ApplicationInfo>>.CopyTo(KeyValuePair<string, ApplicationInfo>[] array, int arrayIndex)
            {
                ((ICollection<KeyValuePair<string, ApplicationInfo>>)_items).CopyTo(array, arrayIndex);
            }

            void IDictionary<string, ApplicationInfo>.Add(string key, ApplicationInfo value)
            {
                throw new NotSupportedException();
            }

            bool IDictionary<string, ApplicationInfo>.Remove(string key)
            {
                throw new NotSupportedException();
            }

            void ICollection<KeyValuePair<string, ApplicationInfo>>.Clear()
            {
                throw new NotSupportedException();
            }
        }
    }
}
