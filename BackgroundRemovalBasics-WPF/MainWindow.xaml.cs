﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BackgroundRemovalBasics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.BackgroundRemoval;
  //  using System.Drawing.Image;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        /// <summary>
        /// Format we will use for the depth stream
        /// </summary>
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap foregroundBitmap;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensorChooser sensorChooser;

        private KinectSensor ks;

        /// <summary>
        /// Our core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovedColorStream;

        /// <summary>
        /// Intermediate storage for the skeleton data received from the sensor
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// the skeleton that is currently tracked by the app
        /// </summary>
        private int currentlyTrackedSkeletonId;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            this.sensorChooser.Start();
        }

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Dispose the allocated frame buffers and reconstruction.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all memory associated with the FusionImageFrame.
        /// </summary>
        /// <param name="disposing">Whether the function was called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (null != this.backgroundRemovedColorStream)
                {
                    this.backgroundRemovedColorStream.Dispose();
                    this.backgroundRemovedColorStream = null;
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
            this.sensorChooser = null;
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, or lingering events from previous sensor, do nothing here.
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect || this.sensorChooser.Kinect != sender)
            {
                return;
            }

            try
            {
                using (var depthFrame = e.OpenDepthImageFrame())
                {
                    if (null != depthFrame)
                    {
                        this.backgroundRemovedColorStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                    }
                }

                using (var colorFrame = e.OpenColorImageFrame())
                {
                    if (null != colorFrame)
                    {
                        this.backgroundRemovedColorStream.ProcessColor(colorFrame.GetRawPixelData(), colorFrame.Timestamp);
                    }
                }

                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (null != skeletonFrame)
                    {
                        skeletonFrame.CopySkeletonDataTo(this.skeletons);
                        this.backgroundRemovedColorStream.ProcessSkeleton(this.skeletons, skeletonFrame.Timestamp);
                    }
                }

                this.ChooseSkeleton();
            }
            catch (InvalidOperationException)
            {
                // Ignore the exception. 
            }
        }

        /// <summary>
        /// Handle the background removed color frame ready event. The frame obtained from the background removed
        /// color stream is in RGBA format.
        /// </summary>
        /// <param name="sender">object that sends the event</param>
        /// <param name="e">argument of the event</param>
        private int sizeIm = 500;
        private double original = 0;
        private Boolean flipped = false;
        private int stage = 1;
        private int counter = 0;
        private JointCollection jc;
        private WriteableBitmap wb;
        private BackgroundRemovedColorFrame br;
        private void BackgroundRemovedFrameReadyHandler(object sender, BackgroundRemovedColorFrameReadyEventArgs e)
        {
            using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
            {
                if (backgroundRemovedFrame != null)
                {
                    if (null == this.foregroundBitmap || this.foregroundBitmap.PixelWidth != backgroundRemovedFrame.Width 
                        || this.foregroundBitmap.PixelHeight != backgroundRemovedFrame.Height)
                    {
                        
                    //    WriteableBitmap bit = WriteableBitmap(Happy.Source);
                        this.foregroundBitmap = new WriteableBitmap(backgroundRemovedFrame.Width, backgroundRemovedFrame.Height, 96.0, 96.0, PixelFormats.Bgra32, null);

                        // Set the image we display to point to the bitmap where we'll put the image data
                        this.MaskedColor.Source = this.foregroundBitmap;
                        
                        
                    }

                    if (original == 0)
                    {
                        original = Happy.Width;
                    }
                    
                    if (stage == 0)
                    {
                        if (s != null && currentlyTrackedSkeletonId >= 0)
                        {
                            stage = 1;
                        }
                    }
                    else if (stage == 1) {
                        counter++;
                        Matching.FontSize = 30;
                        Matching.Text = "Initializing";
                        if (counter == 20) { stage = 2; counter = 0; }
                    }
                    else if (stage == 2)
                    {
                        counter++;
                        Matching.FontSize = 60;
                        Matching.Text = ((105 - counter) / 35 + 1).ToString();
                        if (counter == 105) { stage = 3; counter = 0; }
                    }
                    else if (stage == 3 || stage == 4)
                    {
                        if (stage == 3 && counter < 40)
                        {
                            counter++;
                            Matching.FontSize = 60 - counter;
                            Matching.Text = "Go";
                            if (counter == 40) { stage = 4; counter = 0; }
                        }
                        if (stage == 4)
                        {
                            Happy.Width = sizeIm;
                            if (sizeIm < foregroundBitmap.PixelWidth + 100)
                            {
                                sizeIm += 1;

                                var total = 0;
                                if (s != null)
                                {
                                    //  jc = s.Joints;
                                    foreach (Joint joint in s.Joints)
                                    {
                                        ColorImagePoint point = ks.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
                                        if (point.X > -1000000 && point.X < 1000000 && point.Y > -1000000 && point.Y < 1000000)
                                        {
                                            Joint j = joint;
                                            int pos = point.Y * stride + 4 * point.X;
                                            if (pos >= 0 && pos < sil.Length && sil[pos] == 255 && sil[pos + 1] == 255 && sil[pos + 2] == 255)
                                            {
                                                total += 1;
                                            }
                                        }
                                    }
                                    Matching.Text = "Matching: " + (total * 100 / s.Joints.Count).ToString() + "%";
                                }
                                else
                                {
                                    Matching.Text = "Matching: 0%";
                                }
                                Matching.FontSize = 30;
                                
                            }
                            else
                            {
                                stage = 5;
                             //   wb = this.foregroundBitmap;
                                br = backgroundRemovedFrame;
                            //    UpdateImage("Images//Bent.png");
                            //    Happy.Width = sizeIm;
                            //    Happy.UpdateLayout();
                            //    flipped = true;
                                
                            }
                        }

                    }
                    else if (stage == 5)
                    {
                        Happy.Visibility = System.Windows.Visibility.Hidden;
                        HappyEnd.Visibility = System.Windows.Visibility.Visible;
                     //   this.MaskedColor.Source = wb;
                     //   backgroundRemovedFrame = br;
                     /*   foreach (Joint joint in jc)
                        {
                            ColorImagePoint point = ks.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
                            if (point.X > -1000000 && point.X < 1000000 && point.Y > -1000000 && point.Y < 1000000)
                            {
                                Joint j = joint;
                                int pos = point.Y * stride + 4 * point.X;
                                if (pos >= 0 && pos < sil.Length && sil[pos] == 255 && sil[pos + 1] == 255 && sil[pos + 2] == 255)
                                {
                                   // MaskedColor.
                                }
                            }
                        }*/
                    }
                    if (br == null)
                    {
                        // Write the pixel data into our bitmap
                        this.foregroundBitmap.WritePixels(
                            new Int32Rect(0, 0, this.foregroundBitmap.PixelWidth, this.foregroundBitmap.PixelHeight),
                            backgroundRemovedFrame.GetRawPixelData(),
                            this.foregroundBitmap.PixelWidth * sizeof(int),
                            0);
                    }
                    else
                    {
                        // Write the pixel data into our bitmap
                        this.foregroundBitmap.WritePixels(
                            new Int32Rect(0, 0, this.foregroundBitmap.PixelWidth, this.foregroundBitmap.PixelHeight),
                            br.GetRawPixelData(),
                            this.foregroundBitmap.PixelWidth * sizeof(int),
                            0);
                    }
                }
            }
        }

        private void UpdateImage(String uri)
        {
        //    Image myImage3 = new Image();
            BitmapImage bi3 = new BitmapImage();
            bi3.BeginInit();
            bi3.UriSource = new Uri(uri, UriKind.Relative);
            bi3.EndInit();
            Happy.Stretch = Stretch.Fill; 
            Happy.Source = bi3;
        }

        private byte[] sil;
        int stride;
        private void LoadImage()
        {
            BitmapImage img = new BitmapImage(new Uri("..//..//Images//Happy.bmp", UriKind.Relative));
          //  BitmapImage img = new BitmapImage(new Uri("..//..//Images//Mad.png", UriKind.Relative));
            img.CreateOptions = BitmapCreateOptions.None;
            stride = img.PixelWidth * 4;
            int size = img.PixelHeight * stride;
            byte[] pixels = new byte[size];
            img.CopyPixels(pixels, stride, 0);
            sil = pixels;

            /*
            BitmapSource bms = Happy.Source as BitmapSource;
            WriteableBitmap wb = new WriteableBitmap(bms);
            for (int i = 300; i < 400; ++i)
            {
                for (int j = 200; j < 300; ++j)
                {
                    //Console.WriteLine(i + "\t" + j);
                    Color color = getPixelColor(wb, i, j);
                    //Console.WriteLine(color);
                    if (color == Colors.White)
                    {
                        Console.WriteLine(i + "\t" + j);
                        Console.WriteLine("Found white!");
                    }
                }
            }
             * */
        }

        /// <summary>
        /// Use the sticky skeleton logic to choose a player that we want to set as foreground. This means if the app
        /// is tracking a player already, we keep tracking the player until it leaves the sight of the camera, 
        /// and then pick the closest player to be tracked as foreground.
        /// </summary>
        private Skeleton s;
        private void ChooseSkeleton()
        {
            var isTrackedSkeletonVisible = false;
            var nearestDistance = float.MaxValue;
            var nearestSkeleton = 0;

            foreach (var skel in this.skeletons)
            {
                if (null == skel)
                {
                    continue;
                }
          //      Console.WriteLine(skel);
                if (skel.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (skel.TrackingId == this.currentlyTrackedSkeletonId)
                {
                    isTrackedSkeletonVisible = true;
                    s = skel;
                    break;
                }

                if (skel.Position.Z < nearestDistance)
                {
                    nearestDistance = skel.Position.Z;
                    nearestSkeleton = skel.TrackingId;
                    s = skel;
                }
            }

            if (!isTrackedSkeletonVisible || nearestSkeleton != 0)
            {
                this.backgroundRemovedColorStream.SetTrackedPlayer(nearestSkeleton);
                this.currentlyTrackedSkeletonId = nearestSkeleton;
            }
            else
            {
             //   s = null;
            }

        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.AllFramesReady -= this.SensorAllFramesReady;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.ColorStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();

                    // Create the background removal stream to process the data and remove background, and initialize it.
                    if (null != this.backgroundRemovedColorStream)
                    {
                        this.backgroundRemovedColorStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyHandler;
                        this.backgroundRemovedColorStream.Dispose();
                        this.backgroundRemovedColorStream = null;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    LoadImage();
                    ks = args.NewSensor;
                    args.NewSensor.DepthStream.Enable(DepthFormat);
                    args.NewSensor.ColorStream.Enable(ColorFormat);
                    args.NewSensor.SkeletonStream.Enable();

                    this.backgroundRemovedColorStream = new BackgroundRemovedColorStream(args.NewSensor);
                    this.backgroundRemovedColorStream.Enable(ColorFormat, DepthFormat);

                    // Allocate space to put the depth, color, and skeleton data we'll receive
                    if (null == this.skeletons)
                    {
                        this.skeletons = new Skeleton[args.NewSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    // Add an event handler to be called when the background removed color frame is ready, so that we can
                    // composite the image and output to the app
                    this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyHandler;

                    // Add an event handler to be called whenever there is new depth frame data
                    args.NewSensor.AllFramesReady += this.SensorAllFramesReady;

                    try
                    {
                        args.NewSensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    this.statusBarText.Text = Properties.Resources.ReadyForScreenshot;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
 //           Happy.Width = sizeIm;
            sizeIm -= 100;
            /*
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            int colorWidth = this.foregroundBitmap.PixelWidth;
            int colorHeight = this.foregroundBitmap.PixelHeight;

            // create a render target that we'll render our controls to
            var renderBitmap = new RenderTargetBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // render the backdrop
                var backdropBrush = new VisualBrush(Backdrop);
                dc.DrawRectangle(backdropBrush, null, new Rect(new Point(), new Size(colorWidth, colorHeight)));
                
                // render Smaller Box
                Happy.Height = 300;
                Happy.Width = 100;
                Happy.RenderSize = new Size(300,100);
                var backdropBrush2 = new VisualBrush(Happy);
                dc.DrawRectangle(backdropBrush2, null, new Rect(new Point(), new Size(colorWidth/4, colorHeight/16)));

                // render the color image masked out by players
                var colorBrush = new VisualBrush(MaskedColor);
                dc.DrawRectangle(colorBrush, null, new Rect(new Point(), new Size(colorWidth, colorHeight)));

                dc.DrawText(new FormattedText("Drawing Text",
                      CultureInfo.GetCultureInfo("en-us"),
                      FlowDirection.LeftToRight,
                      new Typeface("Verdana"),
                      36, Brushes.Red),
                      new Point(0, 0));
             }

            renderBitmap.Render(dv);
    
            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            var time = DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            var myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            try
            {
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, Properties.Resources.ScreenshotWriteFailed, path);
            }
             * */
        }
        
        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            /*
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect)
            {
                return;
            }

            // will not function on non-Kinect for Windows devices
            try
            {
                this.sensorChooser.Kinect.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
            }
            catch (InvalidOperationException)
            {
            }
             */
        }
    }
}