﻿using QuanLib.Minecraft.BlockScreen;
using QuanLib.Minecraft.BlockScreen.Datas;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.BlockScreen
{
    public class Cursor
    {
        public Cursor(Json json)
        {
            if (json is null)
                throw new ArgumentNullException(nameof(json));

            CursorType = json.CursorType;
            Offset = new(json.XOffset, json.YOffset);
            Frame = Frame.FromJson(json.Frame);
        }

        public CursorType CursorType { get; }

        public Point Offset { get; }

        public Frame Frame { get; }

        public class Json
        {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            public CursorType CursorType { get; set; }

            public int XOffset { get; set; }

            public int YOffset { get; set; }

            public FrameJson Frame { get; set; }
        }
    }
}
