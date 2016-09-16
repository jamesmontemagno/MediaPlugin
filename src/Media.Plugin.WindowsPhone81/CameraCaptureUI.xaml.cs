using Plugin.Media.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Plugin.Media
{

    internal enum CameraCaptureUIMode
    {
        PhotoOrVideo,
        Photo,
        Video
    }

    internal sealed partial class CameraCaptureUI : UserControl
    {
        // store the pic here
        StorageFile file;

        DisplayInformation displayInfo = DisplayInformation.GetForCurrentView();

        VideoRotation rotation;

        //ihuete
        CameraCaptureUIMode Mode;
        bool IsRecording = false;

        // stop flag - needed to find when to get back to former page
        bool stopFlag = false;

        public bool StopFlag
        {
            get { return stopFlag; }
            set { stopFlag = value; }
        }

        // the root grid of our camera ui page
        Grid mainGrid;

        private MediaCapture mediaCapture;
        public MediaCapture MyMediaCapture
        {
            get
            {
                return mediaCapture;
            }
            set { mediaCapture = value; }
        }
        Frame originalFrame;
        private const short WaitForClickLoopLength = 1000;


        /// <summary>
        /// Navigates to the CameraCaptureUIPage in a new Frame and show the control
        /// </summary>
        public CameraCaptureUI()
        {
            InitializeComponent();

            // get current app
            app = Application.Current;

            // get current frame
            originalFrame = (Frame)Window.Current.Content;

            CurrentWindow = Window.Current;
            NewCamCapFrame = new Frame();
            CurrentWindow.Content = NewCamCapFrame;

            // navigate to Capture UI page 
            NewCamCapFrame.Navigate(typeof(CameraCaptureUIPage));

            Unloaded += CameraCaptureUI_Unloaded;
#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif

            // set references current CCUI page
            myCcUiPage = ((CameraCaptureUIPage)NewCamCapFrame.Content);

            myCcUiPage.MyCCUCtrl = this;

            app.Suspending += AppSuspending;
            app.Resuming += AppResuming;


            // set content
            mainGrid = (Grid)(myCcUiPage).Content;

            // Remove all children, if any exist
            mainGridChildren = mainGrid.Children;
            foreach (var item in mainGridChildren)
            {
                mainGrid.Children.Remove(item);
            }

            // Show Ctrl
            mainGrid.Children.Add(this);
        }

        public async void AppResuming(object sender, object e)
        {
            // get current frame
            NewCamCapFrame = (Frame)Window.Current.Content;

            // make sure you are on CCUIPage
            var ccuipage = NewCamCapFrame.Content as CameraCaptureUIPage;
            if (ccuipage != null)
            {
                var ccu = ccuipage.MyCCUCtrl;

                // start captureing again
                await ccu.CaptureFileAsync(CameraCaptureUIMode.Photo, Options);
            }
            else
            {
                app.Resuming -= AppResuming;
            }
        }

        public async void AppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await CleanUpAsync();
            deferral.Complete();
        }

        private void CameraCaptureUI_Unloaded(object sender, RoutedEventArgs e)
        {
#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
            displayInfo.OrientationChanged -= DisplayInfo_OrientationChanged;
#endif
        }

        async void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            await GoBackAsync(e);
        }

        private async Task GoBackAsync(Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            await CleanUpAsync();

            e.Handled = true;

            CurrentWindow.Content = originalFrame;
        }

        public async Task CleanUpAsync()
        {
            if (myCaptureElement != null)
            {
                myCaptureElement.Source = null;
            }

            if (MyMediaCapture != null)
            {
                try
                {
                    await MyMediaCapture.StopPreviewAsync();
                }
                catch (ObjectDisposedException o)
                {
                    Debug.WriteLine(o.Message);
                }
            }

            if (MyMediaCapture != null)
            {
                try
                {
                    MyMediaCapture.Dispose();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }



        StoreCameraMediaOptions Options;
        /// <summary>
        /// This method takes a picture. 
        /// Right now the parameter is not evaluated.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<StorageFile> CaptureFileAsync(CameraCaptureUIMode mode, StoreCameraMediaOptions options)
        {
            var t = IsStopped();
            Mode = mode;
            if (Mode == CameraCaptureUIMode.Photo)
            {
                camerButton.Icon = new SymbolIcon(Symbol.Camera);
            }
            else if (Mode == CameraCaptureUIMode.Video)
            {
                camerButton.Icon = new SymbolIcon(Symbol.Video);
            }
            Options = options;
            // Create new MediaCapture 
            MyMediaCapture = new MediaCapture();
            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var backCamera = videoDevices.FirstOrDefault(
                item => item.EnclosureLocation != null
                && item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);

            var frontCamera = videoDevices.FirstOrDefault(
                  item => item.EnclosureLocation != null
                  && item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);

            var captureSettings = new MediaCaptureInitializationSettings();
            if (options.DefaultCamera == CameraDevice.Front && frontCamera != null)
            {
                captureSettings.VideoDeviceId = frontCamera.Id;
            }
            else if (options.DefaultCamera == CameraDevice.Rear && backCamera != null)
            {
                captureSettings.VideoDeviceId = backCamera.Id;
            }
            await MyMediaCapture.InitializeAsync(captureSettings);


            displayInfo.OrientationChanged += DisplayInfo_OrientationChanged;

            DisplayInfo_OrientationChanged(displayInfo, null);

            // Assign to Xaml CaptureElement.Source and start preview
            myCaptureElement.Source = MyMediaCapture;

            // show preview
            await MyMediaCapture.StartPreviewAsync();

            // now wait until stopflag shows that someone took a picture
            await t;

            // picture has been taken
            // stop preview

            await CleanUpAsync();

            // go back
            CurrentWindow.Content = originalFrame;

            mainGrid.Children.Remove(this);

            return file;
        }

        private void DisplayInfo_OrientationChanged(DisplayInformation sender, object args)
        {
            if (mediaCapture != null)
            {
                rotation = VideoRotationLookup(sender.CurrentOrientation, false);
                mediaCapture.SetPreviewRotation(rotation);
                mediaCapture.SetRecordRotation(rotation);
            }
        }

        private VideoRotation VideoRotationLookup(DisplayOrientations displayOrientation, bool counterclockwise)
        {
            switch (displayOrientation)
            {
                case DisplayOrientations.Landscape:
                    return VideoRotation.None;

                case DisplayOrientations.Portrait:
                    return (counterclockwise) ? VideoRotation.Clockwise270Degrees : VideoRotation.Clockwise90Degrees;

                case DisplayOrientations.LandscapeFlipped:
                    return VideoRotation.Clockwise180Degrees;

                case DisplayOrientations.PortraitFlipped:
                    return (counterclockwise) ? VideoRotation.Clockwise90Degrees :
                    VideoRotation.Clockwise270Degrees;

                default:
                    return VideoRotation.None;
            }
        }

        private BitmapRotation GetBitmapRotationFromVideoRotation()
        {
            switch (rotation)
            {
                case VideoRotation.None:
                    return BitmapRotation.None;
                case VideoRotation.Clockwise90Degrees:
                    return BitmapRotation.Clockwise90Degrees;
                case VideoRotation.Clockwise180Degrees:
                    return BitmapRotation.Clockwise180Degrees;
                case VideoRotation.Clockwise270Degrees:
                    return BitmapRotation.Clockwise270Degrees;
                default:
                    return BitmapRotation.None;
            }
        }

        /// <summary>
        /// This is a loop which waits async until the flag has been set.
        /// </summary>
        /// <returns></returns>
        async private Task IsStopped()
        {
            while (!StopFlag)
            {
                await Task.Delay(WaitForClickLoopLength);
            }
        }

        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRecording)
            {
                // Create new file in the pictures library     
                if (Mode == CameraCaptureUIMode.Photo)
                {
                    string photoPath = string.Empty;
                    // create a jpeg image
                    var imgEncodingProperties = ImageEncodingProperties.CreateJpeg();

                    using (var imageStream = new InMemoryRandomAccessStream())
                    {
                        await MyMediaCapture.CapturePhotoToStreamAsync(imgEncodingProperties, imageStream);

                        var decoder = await BitmapDecoder.CreateAsync(imageStream);
                        var encoder = await BitmapEncoder.CreateForTranscodingAsync(imageStream, decoder);

                        encoder.BitmapTransform.Rotation = GetBitmapRotationFromVideoRotation();

                        await encoder.FlushAsync();

                        var capturefile = await ApplicationData.Current.LocalFolder.CreateFileAsync("_____ccuiphoto.jpg", CreationCollisionOption.ReplaceExisting);
                        photoPath = capturefile.Name;

                        using (var fileStream = await capturefile.OpenStreamForWriteAsync())
                        {
                            try
                            {
                                await RandomAccessStream.CopyAsync(imageStream, fileStream.AsOutputStream());
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        }
                    }

                    file = await ApplicationData.Current.LocalFolder.GetFileAsync("_____ccuiphoto.jpg");

                    // when pic has been taken, set stopFlag
                    StopFlag = true;
                }
                else if (Mode == CameraCaptureUIMode.Video)
                {
                    IsRecording = true;
                    camerButton.Icon = new SymbolIcon(Symbol.Stop);

                    file = await ApplicationData.Current.LocalFolder.CreateFileAsync("_____ccuivideo.mp4", CreationCollisionOption.ReplaceExisting);

                    // create a jpeg image
                    var videoEncodingProperties = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Vga);

                    await MyMediaCapture.StartRecordToStorageFileAsync(videoEncodingProperties, file);

                    // when pic has been taken, set stopFlag
                    StopFlag = false;
                }
            }
            else
            {
                await MyMediaCapture.StopRecordAsync();
                StopFlag = true;
            }
        }

        public UIElementCollection mainGridChildren { get; set; }

        public bool locker = false;

        public Application app { get; set; }

        private CameraCaptureUIPage myCcUiPage;

        public CameraCaptureUIPage MyCciPage
        {
            get { return myCcUiPage; }
            set { myCcUiPage = value; }
        }

        public Window CurrentWindow { get; set; }

        public Frame NewCamCapFrame { get; set; }
    }
}


