﻿using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.BlockScreen.Controls
{
    /// <summary>
    /// 控件同步器
    /// </summary>
    public class ControlSyncer
    {
        public ControlSyncer(Control target, Action<Point, Point> onMove, Action<Size, Size> onResize)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            OnMove = onMove;
            OnResize = onResize;
        }

        public Control Target { get; }

        public Action<Point, Point> OnMove;

        public Action<Size, Size> OnResize;

        /// <summary>
        /// 绑定
        /// </summary>
        public void Binding()
        {
            Target.OnMoveNow += OnMove;
            Target.OnResizeNow += OnResize;
        }

        /// <summary>
        /// 解绑
        /// </summary>
        public void Unbinding()
        {
            Target.OnMoveNow -= OnMove;
            Target.OnResizeNow -= OnResize;
        }
        
        /// <summary>
        /// 调用同步委托
        /// </summary>
        public void Sync()
        {
            OnMove.Invoke(Target.ClientLocation, Target.ClientLocation);
            OnResize.Invoke(Target.ClientSize, Target.ClientSize);
        }
    }
}
