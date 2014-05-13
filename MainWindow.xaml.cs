// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace FaceTrackingBasics
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private WriteableBitmap colorImageWritableBitmap;
        private byte[] colorImageData;
        private ColorImageFormat currentColorImageFormat = ColorImageFormat.Undefined;
        private double currentDepth = 0;
        private int depthRange = 400;

        public MainWindow()
        {
            InitializeComponent();

            var faceTrackingViewerBinding = new Binding("Kinect") { Source = sensorChooser };
            faceTrackingViewer.SetBinding(FaceTrackingViewer.KinectProperty, faceTrackingViewerBinding);

            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;

            sensorChooser.Start();
        }

        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs kinectChangedEventArgs)
        {
            KinectSensor oldSensor = kinectChangedEventArgs.OldSensor;
            KinectSensor newSensor = kinectChangedEventArgs.NewSensor;

            if (oldSensor != null)
            {
                oldSensor.AllFramesReady -= KinectSensorOnAllFramesReady;
                oldSensor.ColorStream.Disable();
                oldSensor.DepthStream.Disable();
                oldSensor.DepthStream.Range = DepthRange.Default;
                oldSensor.SkeletonStream.Disable();
                oldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                oldSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }

            if (newSensor != null)
            {
                try
                {
                    newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    try
                    {
                        // This will throw on non Kinect For Windows devices.
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    newSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    newSensor.SkeletonStream.Enable();
                    newSensor.AllFramesReady += KinectSensorOnAllFramesReady;
                }
                catch (InvalidOperationException)
                {
                    // This exception can be thrown when we are trying to
                    // enable streams on a device that has gone away.  This
                    // can occur, say, in app shutdown scenarios when the sensor
                    // goes away between the time it changed status and the
                    // time we get the sensor changed notification.
                    //
                    // Behavior here is to just eat the exception and assume
                    // another notification will come along if a sensor
                    // comes back.
                }
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            sensorChooser.Stop();
            faceTrackingViewer.Dispose();
        }

        private void KinectSensorOnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
            var colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame();
            var depthImageFrame = allFramesReadyEventArgs.OpenDepthImageFrame();
            var skeltImageFrame = allFramesReadyEventArgs.OpenSkeletonFrame();
            try
            {
                {
                    if (colorImageFrame == null)
                    {
                        return;
                    }
                    if (depthImageFrame == null)
                    {
                        return;
                    }
                    if (skeltImageFrame == null)
                    {
                        return;
                    }
                    DepthImagePixel[] depthPixels = new DepthImagePixel[depthImageFrame.PixelDataLength];
                    depthImageFrame.CopyDepthImagePixelDataTo(depthPixels);
                    int minDepth = depthImageFrame.MinDepth;
                    int maxDepth = depthImageFrame.MaxDepth;
                    var ratio = colorImageFrame.PixelDataLength / depthImageFrame.PixelDataLength;
                    var heightC = colorImageFrame.Height;
                    var heightD = depthImageFrame.Height;
                    var LengthC = colorImageFrame.PixelDataLength;
                    var LengthD = depthPixels.Length;
                    var ratH = colorImageFrame.Height / depthImageFrame.Height;
                    var ratW = colorImageFrame.Width / depthImageFrame.Width;

                    // Make a copy of the color frame for displaying.
                    var haveNewFormat = this.currentColorImageFormat != colorImageFrame.Format;
                    if (haveNewFormat)
                    {

                        this.currentColorImageFormat = colorImageFrame.Format;
                        this.colorImageData = new byte[colorImageFrame.PixelDataLength];

                        this.colorImageWritableBitmap = new WriteableBitmap(
                            colorImageFrame.Width, colorImageFrame.Height, 96, 96, PixelFormats.Bgr32, null);
                        ColorImage.Source = this.colorImageWritableBitmap;
                    }

                    colorImageFrame.CopyPixelDataTo(this.colorImageData);
                    for (int i = 0; i < colorImageFrame.Width; ++i)
                    {
                        for (int j = 0; j < colorImageFrame.Height; ++j)
                        {
                            //                      var ratH = colorImageFrame.Height / depthImageFrame.Height;
                            //                    var ratW = colorImageFrame.Width / depthImageFrame.Width;

                            //        var srcX = (i / ratH) + (i / ratW);
                            //                        var srcPixel = i * depthImageFrame.Height / colorImageFrame.Height + ((int)(i * depthImageFrame.Height / colorImageFrame.Height) % depthImageFrame.Height);
                            //                        var srcY = 1;
                            int srcX = i / ratW;
                            int srcY = j / ratH;
                            int srcPixel = srcX + 2 + ((srcY - 15) * depthImageFrame.Width);
                            int tgtPixel = (i + (j * colorImageFrame.Width));
                            if (srcPixel >= 0 && srcPixel < depthPixels.Length)
                            {
                               currentDepth = currentDepth + .00001;
                            short depth = depthPixels[(int)srcPixel].Depth;

                                if (depth < (int)minDepth + currentDepth)
                                {
                                    changePixel(tgtPixel, 255);
//                                  changePixel(tgtPixel, new byte[]{255, 255, 255});
                                }
                                //else if (depth > maxDepth)
                                else if (depth > (int) (minDepth + currentDepth + depthRange))
                                {
                                    changePixel(tgtPixel, 255);
                                }
                                else
                                {
//                                    changePixel(tgtPixel, 0);
                                }
                                if (currentDepth + depthRange>=maxDepth + 300)
                                {
                                    currentDepth = minDepth;
                                }
                            }
                            else
                            {
                                
                            }
                        }
                    }
                    this.colorImageWritableBitmap.WritePixels(
                        new Int32Rect(0, 0, colorImageFrame.Width, colorImageFrame.Height),
                        this.colorImageData,
                        colorImageFrame.Width * Bgr32BytesPerPixel,
                        0);
                }
            }
            finally
            {
                if (colorImageFrame != null)
                {
                    colorImageFrame.Dispose();
                }
                if (depthImageFrame != null)
                {
                    depthImageFrame.Dispose();
                }
                if (skeltImageFrame != null)
                {
                    skeltImageFrame.Dispose();
                }
            }
        }

        private void changePixel(int pixel, byte value)
        {
            changePixel(pixel, new byte[]{value, value, value});
        }

        private void changePixel(int pixel, byte[] values)
        {
            int tgt = pixel * 4;
            if (tgt >= 0 && tgt < this.colorImageData.Length)
            {
                for (int k = 0; k < 3; k++)
                {
                    this.colorImageData[tgt++] = values[k];
                }
            }
        }
    }
}
