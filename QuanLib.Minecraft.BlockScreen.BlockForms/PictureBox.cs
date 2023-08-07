﻿using QuanLib.Minecraft.Block;
using QuanLib.Minecraft.BlockScreen.Event;
using QuanLib.Minecraft.BlockScreen.Frame;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.BlockScreen.BlockForms
{
    public class PictureBox : Control
    {
        public PictureBox()
        {
            DefaultResizeOptions = ImageFrame.DefaultResizeOptions.Clone();
            DefaultResizeOptions.Size = ClientSize;
            _ImageFrame = new(new Image<Rgba32>(DefaultResizeOptions.Size.Width, DefaultResizeOptions.Size.Height, GetBlockAverageColor(BlockManager.Concrete.White)), GetScreenPlaneSize().NormalFacing, DefaultResizeOptions.Clone());
            ClientSize = new(64, 64);

            AutoSize = true;
            ContentAnchor = AnchorPosition.Centered;

            ImageFrameChanged += OnImageFrameChanged;

            _autosetsizeing = false;
        }

        protected bool _autosetsizeing;

        public ResizeOptions DefaultResizeOptions { get; }

        public ImageFrame ImageFrame
        {
            get => _ImageFrame;
            set
            {
                if (_ImageFrame != value)
                {
                    ImageFrame temp = _ImageFrame;
                    _ImageFrame = value;
                    ImageFrameChanged.Invoke(this, new(temp, _ImageFrame));
                    RequestUpdateFrame();
                }
            }
        }
        private ImageFrame _ImageFrame;

        public event EventHandler<PictureBox, ImageFrameChangedEventArgs> ImageFrameChanged;

        public override IFrame RenderingFrame()
        {
            if (ImageFrame.FrameSize != ClientSize)
            {
                ImageFrame.ResizeOptions.Size = ClientSize;
                ImageFrame.Update();
            }

            return ImageFrame.GetFrameClone();
        }

        protected override void OnResize(Control sender, SizeChangedEventArgs e)
        {
            base.OnResize(sender, e);

            if (_autosetsizeing)
                return;

            Size offset = e.NewSize - e.OldSize;
            ImageFrame.ResizeOptions.Size += offset;
            DefaultResizeOptions.Size += offset;
            ImageFrame.Update();
            if (AutoSize)
                AutoSetSize();
        }

        protected virtual void OnImageFrameChanged(PictureBox sender, ImageFrameChangedEventArgs e)
        {
            e.OldImageFrame.Dispose();
            if (AutoSize)
                AutoSetSize();
        }

        public override void AutoSetSize()
        {
            _autosetsizeing = true;
            ClientSize = ImageFrame.FrameSize;
            _autosetsizeing = false;
        }

        public Image<Rgba32> CreateSolidColorImage(Size size, string blockID)
        {
            return CreateSolidColorImage(size, GetBlockAverageColor(blockID));
        }

        public Image<Rgba32> CreateSolidColorImage(int width, int height, string blockID)
        {

            return CreateSolidColorImage(width, height, GetBlockAverageColor(blockID));
        }

        public Image<Rgba32> CreateSolidColorImage(Size size, Rgba32 color)
        {
            return CreateSolidColorImage(size.Width, size.Height, color);
        }

        public Image<Rgba32> CreateSolidColorImage(int width, int height, Rgba32 color)
        {
            return new Image<Rgba32>(width, height, GetBlockAverageColor(BlockManager.Concrete.White));
        }

        public void SetImage(Image<Rgba32> image)
        {
            ImageFrame = new(image, GetScreenPlaneSize().NormalFacing, DefaultResizeOptions.Clone());
        }

        public bool TryReadImageFile(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                ImageFrame = new(Image.Load<Rgba32>(File.ReadAllBytes(path)), GetScreenPlaneSize().NormalFacing, DefaultResizeOptions.Clone());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}