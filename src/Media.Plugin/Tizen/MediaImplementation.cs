using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tizen;
using Tizen.Applications;
using Tizen.Multimedia;

namespace Plugin.Media
{
    public class MediaImplementation : IMedia
    {
        internal const string LOG_TAG = "Plugin.Media";
        internal const string OPERATION_CREATE_CONTENT = "http://tizen.org/appcontrol/operation/create_content";
        internal const string OPERATION_PICK = "http://tizen.org/appcontrol/operation/pick";
        internal const string EX_KEY_SELECTED = "http://tizen.org/appcontrol/data/selected";
        internal const string EX_KEY_ALLOW_SWITCH = "http://tizen.org/appcontrol/data/camera/allow_switch";
        internal const string EX_KEY_VIDEO_SIZE_LIMIT = "http://tizen.org/appcontrol/data/total_size";
        internal const string EX_KEY_REQ_DESTORY = "request_destroy";
        internal const string EX_KEY_SELFIE_MODE = "selfie_mode";
        internal const string EX_KEY_VIDEO_RESOLUTION = "RESOLUTION";
        internal const string EX_KEY_CROP = "crop";
        internal const string EX_VAL_FIT_TO_SCREEN = "fit_to_screen";
        internal const string EX_VAL_1X1_FIX_RATIO = "1x1_fixed_ratio";
        internal const string EX_VAL_TRUE = "true";
        internal const string EX_VAL_FALSE = "false";
        internal const string EX_VAL_DEFAULT_RESOLUTION = "VGA";
        internal const string EX_VAL_SMALL_RESOLUTION = "QCIF";
        internal const long MIN_VIDEO_DESIRE_SIZE = 500000;
        private TaskCompletionSource<MediaFile> completionSource;

        /// <summary>
        /// Implementation constructor
        /// </summary>
        public MediaImplementation()
        {
            try
            {
                var camera = new Camera(Tizen.Multimedia.CameraDevice.Rear);
                if (camera.CameraCount > 0)
                    IsCameraAvailable = true;
                else
                    IsCameraAvailable = false;
            }
            catch (NotSupportedException)
            {
                IsCameraAvailable = false;
            }
            IsTakePhotoSupported = CheckSupportOperation(OPERATION_CREATE_CONTENT, "image/jpg");
            IsPickPhotoSupported = CheckSupportOperation(OPERATION_PICK, "image/*");
            IsTakeVideoSupported = CheckSupportOperation(OPERATION_CREATE_CONTENT, "video/3gp");
            IsPickVideoSupported = CheckSupportOperation(OPERATION_PICK, "video/*");
        }

        /// <inheritdoc/>
        public bool IsCameraAvailable { get; }
        /// <inheritdoc/>
        public bool IsTakePhotoSupported { get; }
        /// <inheritdoc/>
        public bool IsPickPhotoSupported { get; }
        /// <inheritdoc/>
        public bool IsTakeVideoSupported { get; }
        /// <inheritdoc/>
        public bool IsPickVideoSupported { get; }

        /// <inheritdoc/>
        public Task<bool> Initialize() => Task.FromResult(true);

        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options, CancellationToken token = default(CancellationToken))
        {
            if (!IsCameraAvailable || !IsTakePhotoSupported)
            {
                Log.Error(LOG_TAG, "TakePhoto is not supported");
                throw new NotSupportedException();
            }
            var appControl = new AppControl();
            appControl.LaunchMode = AppControlLaunchMode.Group;
            appControl.Operation = OPERATION_CREATE_CONTENT;
            appControl.Mime = "image/jpg";
            SetOptions(options, ref appControl);
            var ntcs = new TaskCompletionSource<MediaFile>();
            Interlocked.CompareExchange(ref completionSource, ntcs, null);
            AppControl.SendLaunchRequest(appControl, AppControlReplyReceivedCallback);
            return completionSource.Task;
        }

        /// <summary>
        /// Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public Task<MediaFile> TakeVideoAsync(StoreVideoOptions options, CancellationToken token = default(CancellationToken))
        {
            if (!IsCameraAvailable || !IsTakeVideoSupported)
            {
                Log.Error(LOG_TAG, "TakeVideo is not supported");
                throw new NotSupportedException();
            }
            var appControl = new AppControl();
            appControl.Operation = OPERATION_CREATE_CONTENT;
            appControl.Mime = "video/3gp";
            appControl.LaunchMode = AppControlLaunchMode.Group;
            SetOptions(options, ref appControl);
            var ntcs = new TaskCompletionSource<MediaFile>();
            Interlocked.CompareExchange(ref completionSource, ntcs, null);
            AppControl.SendLaunchRequest(appControl, AppControlReplyReceivedCallback);
            return completionSource.Task;
        }

        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of video or null if canceled</returns>
        public Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null, CancellationToken token = default(CancellationToken))
        {
            if (!IsPickPhotoSupported)
            {
                Log.Error(LOG_TAG, "PickPhoto is not supported");
                throw new NotSupportedException();
            }
            var appControl = new AppControl();
            appControl.LaunchMode = AppControlLaunchMode.Group;
            SetOptions(options, ref appControl);
            var ntcs = new TaskCompletionSource<MediaFile>();
            Interlocked.CompareExchange(ref completionSource, ntcs, null);
            appControl.Operation = OPERATION_PICK;
            appControl.Mime = "image/*";
            AppControl.SendLaunchRequest(appControl, AppControlReplyReceivedCallback);
            return completionSource.Task;
        }

        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of video or null if canceled</returns>
        public Task<MediaFile> PickVideoAsync(CancellationToken token = default(CancellationToken))
        {
            if (!IsPickVideoSupported)
            {
                Log.Error(LOG_TAG, "PickVideo is not supported");
                throw new NotSupportedException();
            }
            var appControl = new AppControl();
            appControl.LaunchMode = AppControlLaunchMode.Group;
            var ntcs = new TaskCompletionSource<MediaFile>();
            Interlocked.CompareExchange(ref completionSource, ntcs, null);
            appControl.Operation = OPERATION_PICK;
            appControl.Mime = "video/*";
            AppControl.SendLaunchRequest(appControl, AppControlReplyReceivedCallback);
            return completionSource.Task;
        }


        /// <summary>
        /// Check that it is an operation of usable Appcontrol.
        /// </summary>
        /// <param name="operation">Appcontrol operation</param>
        /// <param name="mime">Appcontrol mime</param>
        /// <returns>Allow operation of appcontrol or not</returns>
        private bool CheckSupportOperation(string operation, string mime)
        {
            var appControl = new AppControl();
            appControl.Operation = operation;
            appControl.Mime = mime;
            var applicationIds = AppControl.GetMatchedApplicationIds(appControl);
            if (applicationIds.Count() == 0) return false;
            else return true;
        }

        /// <summary>
        /// A callback that receives the results of the photo or video taken by the camera
        /// Extract file location from result object, convert it to MediaFile, and save it as Result of Task
        /// </summary>
        /// <param name="launchRequest">Appcontrol object sent to run camera</param>
        /// <param name="replyRequest">The appcontrol object received as a result of running the camera</param>
        /// <param name="result">Success of Appcontrol Request</param>
        private void AppControlReplyReceivedCallback(AppControl launchRequest, AppControl replyRequest, AppControlReplyResult result)
        {
            var tcs = Interlocked.Exchange(ref completionSource, null);
            if (result == AppControlReplyResult.Succeeded)
            {
                var file = replyRequest.ExtraData.Get<IEnumerable<string>>(EX_KEY_SELECTED).FirstOrDefault();
                tcs.SetResult(new MediaFile(file, () => File.OpenRead(file)));
            }
            else
            {
                tcs.SetResult(null);
            }
        }

        /// <summary>
        /// Setting Camera Options for Take Photo
        /// In Tizen, the following options are not available.
        /// - options.PhotoSize
        /// - options.SaveToAlbum
        ///	- options.Location
        /// - options.CompressionQuality
        /// </summary>
        /// <param name="options">StoreCameraMediaOptions from Media.Plugin</param>
        private void SetOptions(StoreCameraMediaOptions options, ref AppControl appControl)
        {
            if (appControl == null)
                throw new ObjectDisposedException("AppControl");
            options.VerifyOptions();
            appControl.ExtraData.Add(EX_KEY_ALLOW_SWITCH, EX_VAL_FALSE);
            appControl.ExtraData.Add(EX_KEY_REQ_DESTORY, EX_VAL_TRUE);

            if (options.DefaultCamera == Abstractions.CameraDevice.Front)
                appControl.ExtraData.Add(EX_KEY_SELFIE_MODE, EX_VAL_TRUE);
            else
                appControl.ExtraData.Add(EX_KEY_SELFIE_MODE, EX_VAL_FALSE);

            if (options.AllowCropping.HasValue)
            {
                if (options.AllowCropping.Value)
                    appControl.ExtraData.Add(EX_KEY_CROP, EX_VAL_FIT_TO_SCREEN);
                else
                    appControl.ExtraData.Add(EX_KEY_CROP, EX_VAL_1X1_FIX_RATIO);
            }

        }

        /// <summary>
        /// Setting Camera Options for Take Video
        /// In Tizen, the following options are not available.
        /// - options.SaveToAlbum
        ///	- options.Location
        /// - options.Quality
        /// - options.DesiredLength
        /// </summary>
        /// <param name="options">StoreVideoOptions from Media.Plugin</param>
        private void SetOptions(StoreVideoOptions options, ref AppControl appControl)
        {
            if (appControl == null)
                throw new ObjectDisposedException("AppControl");
            options.VerifyOptions();
            appControl.ExtraData.Add(EX_KEY_ALLOW_SWITCH, EX_VAL_TRUE);
            appControl.ExtraData.Add(EX_KEY_REQ_DESTORY, EX_VAL_TRUE);
            if (options.DefaultCamera == Abstractions.CameraDevice.Front)
                appControl.ExtraData.Add(EX_KEY_SELFIE_MODE, EX_VAL_TRUE);
            else
                appControl.ExtraData.Add(EX_KEY_SELFIE_MODE, EX_VAL_FALSE);
            switch (options.PhotoSize)
            {
                case PhotoSize.Small:
                    appControl.ExtraData.Add(EX_KEY_VIDEO_RESOLUTION, EX_VAL_SMALL_RESOLUTION);
                    break;
                case PhotoSize.Full:
                case PhotoSize.Large:
                case PhotoSize.Medium:
                case PhotoSize.MaxWidthHeight:
                case PhotoSize.Custom:
                default:
                    appControl.ExtraData.Add(EX_KEY_VIDEO_RESOLUTION, EX_VAL_DEFAULT_RESOLUTION);
                    break;
            }
            // Because there is a question about whether DesiredSize is initialized, the option works when it is above a minimum value. (500K)
            if (options.DesiredSize > MIN_VIDEO_DESIRE_SIZE)
                appControl.ExtraData.Add(EX_KEY_VIDEO_SIZE_LIMIT, options.DesiredSize.ToString());
        }

        /// <summary>
        /// Setting Camera Options for Pick Photo
        /// </summary>
        /// <param name="options">PickMediaOptions from Media.Plugin</param>
        private void SetOptions(PickMediaOptions options, ref AppControl appControl)
        {
            if (appControl == null)
                throw new ObjectDisposedException("AppControl");
            /// In Tizen, no options are available.
            Log.Info(LOG_TAG, "There is no option to supported");
        }

        public async Task<List<MediaFile>> PickPhotosAsync(PickMediaOptions options = null, MultiPickerOptions pickerOptions = null, CancellationToken token = default(CancellationToken))
        {
            // TODO: complete Tizen implementation
            var result = await PickPhotoAsync(options, token);

            return new List<MediaFile> { result };
        }
    }
}
