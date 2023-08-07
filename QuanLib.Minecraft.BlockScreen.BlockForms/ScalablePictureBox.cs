﻿using QuanLib.Minecraft.BlockScreen.Event;
using QuanLib.Minecraft.BlockScreen.Frame;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.BlockScreen.BlockForms
{
    public class ScalablePictureBox : PictureBox
    {
        public ScalablePictureBox()
        {
            FirstHandleCursorSlotChanged = true;
            DefaultResizeOptions.Mode = ResizeMode.Max;
            DefaultResizeOptions.Mode = ResizeMode.Max;
            Rectangle = new(0, 0, ImageFrame.ResizeOptions.Size.Width, ImageFrame.ResizeOptions.Size.Height);
            ScalingRatio = 0.2;
            EnableZoom = false;
            EnableDrag = false;
            Dragging = false;

            LastCursorPosition = new(0, 0);
        }

        private static readonly Point InvalidPosition = new(-1, -1);

        private Point LastCursorPosition;

        public Rectangle Rectangle
        {
            get => _Rectangle;
            set
            {
                if (value.Width < 1)
                    value.Width = 1;
                else if (value.Width > ImageFrame.Image.Width)
                    value.Width = ImageFrame.Image.Width;
                if (value.Height < 1)
                    value.Height = 1;
                else if (value.Height > ImageFrame.Image.Height)
                    value.Height = ImageFrame.Image.Height;

                if (value.X + value.Width > ImageFrame.Image.Width - 1)
                    value.X = ImageFrame.Image.Width - value.Width;
                if (value.Y + value.Height > ImageFrame.Image.Height - 1)
                    value.Y = ImageFrame.Image.Height - value.Height;

                if (value.X < 0)
                    value.X = 0;
                else if (value.X > ImageFrame.Image.Width - 1)
                    value.X = ImageFrame.Image.Width - 1;
                if (value.Y < 0)
                    value.Y = 0;
                else if (value.Y > ImageFrame.Image.Height - 1)
                    value.Y = ImageFrame.Image.Height - 1;

                if (_Rectangle != value)
                {
                    _Rectangle = value;
                    if (_Rectangle.Width > ClientSize.Width)
                        ImageFrame.ResizeOptions.Sampler = KnownResamplers.Bicubic;
                    else
                        ImageFrame.ResizeOptions.Sampler = KnownResamplers.NearestNeighbor;
                    ImageFrame.Update(_Rectangle);
                    AutoSetSize();
                    RequestUpdateFrame();
                }
            }
        }
        private Rectangle _Rectangle;

        public double ScalingRatio { get; set; }

        public bool EnableZoom { get; set; }

        public bool EnableDrag { get; set; }

        public bool Dragging { get; private set; }

        public override IFrame RenderingFrame()
        {
            return ImageFrame.GetFrameClone();
        }

        protected override void OnCursorMove(Control sender, CursorEventArgs e)
        {
            base.OnCursorMove(sender, e);

            if (!Dragging)
            {
                LastCursorPosition = InvalidPosition;
                return;
            }
            else if (LastCursorPosition == InvalidPosition)
            {
                LastCursorPosition = e.Position;
                return;
            }

            Point position1 = ClientPos2ImagePos(Rectangle, LastCursorPosition);
            Point position2 = ClientPos2ImagePos(Rectangle, e.Position);
            Point offset = new(position2.X - position1.X, position2.Y - position1.Y);
            Rectangle rectangle = Rectangle;
            rectangle.X -= offset.X;
            rectangle.Y -= offset.Y;
            Rectangle = rectangle;
            LastCursorPosition = e.Position;
        }

        protected override void OnRightClick(Control sender, CursorEventArgs e)
        {
            base.OnRightClick(sender, e);

            if (EnableDrag)
                Dragging = !Dragging;
        }

        protected override void OnResize(Control sender, SizeChangedEventArgs e)
        {
            if (_autosetsizeing)
                return;

            Size offset = e.NewSize - e.OldSize;
            ImageFrame.ResizeOptions.Size += offset;
            DefaultResizeOptions.Size += offset;
            Rectangle = new(0, 0, ImageFrame.Image.Size.Width, ImageFrame.Image.Size.Height);
            ImageFrame.Update(Rectangle);
            if (AutoSize)
                AutoSetSize();
        }

        protected override void OnImageFrameChanged(PictureBox sender, ImageFrameChangedEventArgs e)
        {
            base.OnImageFrameChanged(sender, e);

            if (e.OldImageFrame.Image.Size != e.NewImageFrame.Image.Size)
                Rectangle = new(0, 0, e.NewImageFrame.Image.Size.Width, e.NewImageFrame.Image.Size.Height);
        }

        protected override void OnCursorSlotChanged(Control sender, CursorSlotEventArgs e)
        {
            base.OnCursorSlotChanged(sender, e);

            if (!EnableZoom)
                return;

            Rectangle rectangle = Rectangle;

            Point position1 = ClientPos2ImagePos(rectangle, e.Position);
            Point center1 = GetImageCenter(rectangle);
            Point offset1 = new(position1.X - center1.X, position1.Y - center1.Y);

            rectangle.Width += (int)Math.Round(e.Delta * rectangle.Width * ScalingRatio);
            rectangle.Height += (int)Math.Round(e.Delta * rectangle.Height * ScalingRatio);
            rectangle.X = center1.X - (int)Math.Round(rectangle.Width / 2.0);
            rectangle.Y = center1.Y - (int)Math.Round(rectangle.Height / 2.0);

            Point position2 = ClientPos2ImagePos(rectangle, e.Position);
            Point center2 = GetImageCenter(rectangle);
            Point offset2 = new(position2.X - center2.X, position2.Y - center2.Y);

            rectangle.X += offset1.X - offset2.X;
            rectangle.Y += offset1.Y - offset2.Y;

            Rectangle = rectangle;
        }

        public Point GetImageCenter(Rectangle rectangle)
        {
            return new(rectangle.X + (int)Math.Round(rectangle.Width / 2.0), rectangle.Y + rectangle.Height / 2);
        }

        public Point ClientPos2ImagePos(Rectangle rectangle, Point position)
        {
            double pixels = (double)rectangle.Width / ClientSize.Width;
            return new(rectangle.X + (int)Math.Round(position.X * pixels), rectangle.Y + (int)Math.Round(position.Y * pixels));
        }

        public Point ImagePos2ClientPos(Rectangle rectangle, Point position)
        {
            double pixels = (double)rectangle.Width / ClientSize.Width;
            return new((int)Math.Round((position.X - rectangle.X) / pixels), (int)Math.Round((position.Y - rectangle.Y) / pixels));
        }
    }
}