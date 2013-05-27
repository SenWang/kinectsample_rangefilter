using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.DepthStream.Disable();
                kinect.Stop();
                kinect.ColorFrameReady -= myKinect_ColorFrameReady;
                kinect.DepthFrameReady -= mykinect_DepthFrameReady;
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private WriteableBitmap _DepthImageBitmap;
        private Int32Rect _DepthImageBitmapRect;
        private int _DepthImageStride;
        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                ColorImageStream colorStream = kinect.ColorStream;
                kinect.ColorStream.Enable();
                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,colorStream.FrameHeight, 96, 96,PixelFormats.Bgr32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;
                kinect.ColorFrameReady += myKinect_ColorFrameReady;

                DepthImageStream depthStream = kinect.DepthStream;
                kinect.DepthStream.Enable();   
                _DepthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _DepthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _DepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthData.Source = _DepthImageBitmap;              
                kinect.DepthFrameReady += mykinect_DepthFrameReady;

                kinect.Start();
            }
        }

        short[] depthpixelData;
        void mykinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                     depthpixelData= new short[frame.PixelDataLength];
                    frame.CopyPixelDataTo(depthpixelData);
                    _DepthImageBitmap.WritePixels(_DepthImageBitmapRect, depthpixelData, _DepthImageStride, 0);
                }
            }
        }
        
        void RangeFilter(byte[] pixelData,int BytesPerPixel)
        {
            int min =  kinect.DepthStream.MinDepth;
            int max = kinect.DepthStream.MaxDepth;

            for (int i = 0; i < pixelData.Length; i += BytesPerPixel)
            {
                if (!PixelInRange(i / BytesPerPixel,min, max))
                {
                    pixelData[i] = 0x00; 
                    pixelData[i + 1] = 0x00; 
                    pixelData[i + 2] = 0x00; 
                }
            }
        }

        Boolean PixelInRange(int index,int min,int max)
        {
            int depth = depthpixelData[index] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            if (depth >= min && depth <= max)
                return true;
            else
                return false;
        }

        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if(frame == null)
                    return ;
                    
                byte[] pixelData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(pixelData);

                if(depthpixelData != null)
                    RangeFilter(pixelData, frame.BytesPerPixel);

                _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, pixelData,_ColorImageStride, 0);
            }
        }
    }
}
