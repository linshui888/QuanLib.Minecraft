﻿using QuanLib.Minecraft.DirectoryManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.Instance
{
    public abstract class MinecraftClient : MinecraftInstance
    {
        protected MinecraftClient(string clientPath) : base(clientPath)
        {
            ClientDirectory = new(clientPath);
        }

        public override InstanceType InstanceType => InstanceType.Client;

        public override MinecraftDirectory MinecraftDirectory => ClientDirectory is null ? base.MinecraftDirectory : ClientDirectory;

        public virtual MinecraftClientDirectory ClientDirectory { get; }
    }
}