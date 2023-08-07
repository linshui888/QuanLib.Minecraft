﻿using QuanLib.Minecraft.BlockScreen.Event;
using QuanLib.Minecraft.BlockScreen.Screens;
using QuanLib.Minecraft.BlockScreen.UI;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.BlockScreen.BlockForms
{
    public abstract class Form : ContainerControl<Control>, IForm
    {
        protected Form()
        {
            AllowSelected = true;
            AllowDeselected = true;
            AllowMove = true;
            AllowResize = true;
            Moveing = false;
            Resizeing = false;
            ResizeBorder = Direction.None;
            IsMinimize = false;
            Stretch = Direction.Bottom | Direction.Right;

            _restoreing = false;

            FormLoad += OnFormLoad;
            FormClose += OnFormClose;
            FormMinimize += OnFormMinimize;
            FormUnminimize += OnFormUnminimize;
        }

        private bool _restoreing;

        public virtual bool AllowSelected { get; set; }

        public virtual bool AllowDeselected { get; set; }

        public virtual bool AllowMove { get; set; }

        public virtual bool AllowResize { get; set; }

        public virtual bool Moveing { get; set; }

        public virtual bool Resizeing { get; set; }

        public virtual Direction ResizeBorder { get; protected set; }

        public virtual bool IsMinimize { get; protected set; }

        public virtual bool IsMaximize
        {
            get
            {
                Size maximizeSize = MaximizeSize;
                return Location == MaximizeLocation && Width == maximizeSize.Width && Height == maximizeSize.Height;
            }
        }

        public object? ReturnValue { get; }

        public virtual Point MaximizeLocation => new(0, 0);

        public virtual Size MaximizeSize => GetFormContainerSize();

        public virtual Point RestoreLocation { get; protected set; }

        public virtual Size RestoreSize { get; protected set; }

        public event EventHandler<IForm, EventArgs> FormLoad;

        public event EventHandler<IForm, EventArgs> FormClose;

        public event EventHandler<IForm, EventArgs> FormMinimize;

        public event EventHandler<IForm, EventArgs> FormUnminimize;

        protected virtual void OnFormLoad(IForm sender, EventArgs e) { }

        protected virtual void OnFormClose(IForm sender, EventArgs e)
        {
            ClearAllLayoutSyncer();
        }

        protected virtual void OnFormMinimize(IForm sender, EventArgs e) { }

        protected virtual void OnFormUnminimize(IForm sender, EventArgs e) { }

        public void HandleFormLoad(EventArgs e)
        {
            FormLoad.Invoke(this, e);
        }

        public void HandleFormClose(EventArgs e)
        {
            FormClose.Invoke(this, e);
        }

        public void HandleFormMinimize(EventArgs e)
        {
            FormMinimize.Invoke(this, e);
        }

        public void HandleFormUnminimize(EventArgs e)
        {
            FormUnminimize.Invoke(this, e);
        }

        public override void Initialize()
        {
            base.Initialize();

            Size maximizeSize = MaximizeSize;
            Width = maximizeSize.Width;
            Height = maximizeSize.Height;
            InvokeExternalCursorMove = true;
        }

        protected override void OnInitializeCallback(Control sender, EventArgs e)
        {
            base.OnInitializeCallback(sender, e);

            if (IsMaximize)
            {
                RestoreSize = ClientSize * 2 / 3;
                RestoreLocation = new(Width / 2 - RestoreSize.Width / 2, Height / 2 - RestoreSize.Height / 2);
            }
            else
            {
                RestoreSize = ClientSize;
                RestoreLocation = ClientLocation;
            }
        }

        protected override void OnCursorMove(Control sender, CursorEventArgs e)
        {
            base.OnCursorMove(sender, e);

            Point parent = this.SubPos2ParentPos(e.Position);
            if (AllowResize && IsSelected && !IsMaximize)
            {
                if (Resizeing)
                {
                    if (ResizeBorder.HasFlag(Direction.Top))
                        TopLocation = parent.Y;
                    if (ResizeBorder.HasFlag(Direction.Bottom))
                        BottomLocation = parent.Y;
                    if (ResizeBorder.HasFlag(Direction.Left))
                        LeftLocation = parent.X;
                    if (ResizeBorder.HasFlag(Direction.Right))
                        RightLocation = parent.X;
                }
                else
                {
                    ResizeBorder = Direction.None;
                    if (parent.Y >= TopLocation - 2 &&
                        parent.X >= LeftLocation - 2 &&
                        parent.Y <= BottomLocation + 2 &&
                        parent.X <= RightLocation + 2)
                    {
                        if (parent.Y <= TopLocation + 2)
                            ResizeBorder |= Direction.Top;
                        if (parent.X <= LeftLocation + 2)
                            ResizeBorder |= Direction.Left;
                        if (parent.Y >= BottomLocation - 2)
                            ResizeBorder |= Direction.Bottom;
                        if (parent.X >= RightLocation - 2)
                            ResizeBorder |= Direction.Right;
                    }

                    ScreenContext? context = MCOS.GetMCOS().ScreenContextOf(this);
                    if (context is not null)
                    {
                        context.CursorType = ResizeBorder switch
                        {
                            Direction.Top or Direction.Bottom => CursorType.VerticalResize,
                            Direction.Left or Direction.Right => CursorType.HorizontalResize,
                            Direction.Left | Direction.Top or Direction.Right | Direction.Bottom => CursorType.LeftObliqueResize,
                            Direction.Right | Direction.Top or Direction.Left | Direction.Bottom => CursorType.RightObliqueResize,
                            _ => CursorType.Default,
                        };
                    }
                }
            }
        }

        protected override void OnMove(Control sender, PositionChangedEventArgs e)
        {
            base.OnMove(sender, e);

            if (!_restoreing && !IsMaximize)
            {
                RestoreLocation = ClientLocation;
                RestoreSize = ClientSize;
            }
        }

        protected override void OnResize(Control sender, SizeChangedEventArgs e)
        {
            base.OnResize(sender, e);

            if (!_restoreing && !IsMaximize)
            {
                RestoreLocation = ClientLocation;
                RestoreSize = ClientSize;
            }
        }

        public virtual void MaximizeForm()
        {
            Size = MaximizeSize;
            Location = new(0, 0);
        }

        public virtual void RestoreForm()
        {
            _restoreing = true;
            ClientLocation = RestoreLocation;
            ClientSize = RestoreSize;
            _restoreing = false;
        }

        public virtual void MinimizeForm()
        {
            GetFormContext()?.MinimizeForm();
        }

        public virtual void CloseForm()
        {
            GetFormContext()?.CloseForm();
        }

        public FormContext? GetFormContext()
        {
            return MCOS.GetMCOS().FormContextOf(this);
        }
    }
}