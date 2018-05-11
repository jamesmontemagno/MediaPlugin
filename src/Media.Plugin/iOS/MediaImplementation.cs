using Plugin.Media.Abstractions;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Linq;

using UIKit;
using Foundation;

using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Collections.Generic;

namespace Plugin.Media
{
    /// <summary>
    /// Implementation for Media
    /// </summary>
    public class MediaImplementation : IMedia
    {
        /// <summary>
        /// Color of the status bar
        /// </summary>
        public static UIStatusBarStyle StatusBarStyle { get; set; }

       

        ///<inheritdoc/>
        public Task<bool> Initialize() => Task.FromResult(true);

        /// <summary>
        /// Implementation
        /// </summary>
        public MediaImplementation()
        {
            StatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;
            IsCameraAvailable = UIImagePickerController.IsCameraDeviceAvailable(UIKit.UIImagePickerControllerCameraDevice.Front)
									   | UIImagePickerController.IsCameraDeviceAvailable(UIKit.UIImagePickerControllerCameraDevice.Rear);

            var availableCameraMedia = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.Camera) ?? new string[0];
            var avaialbleLibraryMedia = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary) ?? new string[0];

            foreach (string type in availableCameraMedia.Concat(avaialbleLibraryMedia))
            {
                if (type == TypeMovie)
                    IsTakeVideoSupported = IsPickVideoSupported = true;
                else if (type == TypeImage)
                    IsTakePhotoSupported = IsPickPhotoSupported = true;
            }
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

        
        /// <summary>
        /// Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        public async Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
        {
            if (!IsPickPhotoSupported)
                throw new NotSupportedException();


			//Does not need permission on iOS 11
			if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
			{
				CheckUsageDescription(photoDescription);

				await CheckPermissions(Permission.Photos);
			}

			var cameraOptions = new StoreCameraMediaOptions
			{
				PhotoSize = options?.PhotoSize ?? PhotoSize.Full,
				CompressionQuality = options?.CompressionQuality ?? 100,
				AllowCropping = false,
				CustomPhotoSize = options?.CustomPhotoSize ?? 100,
				MaxWidthHeight = options?.MaxWidthHeight,
				RotateImage = options?.RotateImage ?? true,
				SaveMetaData = options?.SaveMetaData ?? true,
				SaveToAlbum = false,
				ModalPresentationStyle = options?.ModalPresentationStyle ?? MediaPickerModalPresentationStyle.FullScreen,
            };

            return await GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeImage, cameraOptions);
        }


        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options)
        {
            if (!IsTakePhotoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            CheckUsageDescription(cameraDescription);
			if (options.SaveToAlbum)
				CheckUsageDescription(photoAddDescription);

            VerifyCameraOptions(options);

			var permissionsToCheck = new List<Permission> { Permission.Camera };
			if (options.SaveToAlbum)
				permissionsToCheck.Add(Permission.Photos);

			await CheckPermissions(permissionsToCheck.ToArray());

            return await GetMediaAsync(UIImagePickerControllerSourceType.Camera, TypeImage, options);
        }


        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        public async Task<MediaFile> PickVideoAsync()
        {
            if (!IsPickVideoSupported)
                throw new NotSupportedException();

            var backgroundTask = UIApplication.SharedApplication.BeginBackgroundTask(() => { });

            
			//Not needed on iOS 11 since it runs in different process
			if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
			{
				CheckUsageDescription(photoDescription);
				await CheckPermissions(Permission.Photos);
			}

			var media = await GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeMovie);

            UIApplication.SharedApplication.EndBackgroundTask(backgroundTask);

            return media;
        }
        

        /// <summary>
        /// Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            if (!IsTakeVideoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            CheckUsageDescription(cameraDescription, microphoneDescription);

			if (options.SaveToAlbum)
				CheckUsageDescription(photoAddDescription);

			VerifyCameraOptions(options);

			var permissionsToCheck = new List<Permission> { Permission.Camera, Permission.Microphone };
			if (options.SaveToAlbum)
				permissionsToCheck.Add(Permission.Photos);

			await CheckPermissions(permissionsToCheck.ToArray());

            return await GetMediaAsync(UIImagePickerControllerSourceType.Camera, TypeMovie, options);
        }

        private UIPopoverController popover;
        private UIImagePickerControllerDelegate pickerDelegate;
        /// <summary>
        /// image type
        /// </summary>
        public const string TypeImage = "public.image";
        /// <summary>
        /// movie type
        /// </summary>
        public const string TypeMovie = "public.movie";

        private void VerifyOptions(StoreMediaOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (options.Directory != null && Path.IsPathRooted(options.Directory))
                throw new ArgumentException("options.Directory must be a relative path", "options");
        }

        private void VerifyCameraOptions(StoreCameraMediaOptions options)
        {
            VerifyOptions(options);
            if (!Enum.IsDefined(typeof(CameraDevice), options.DefaultCamera))
                throw new ArgumentException("options.Camera is not a member of CameraDevice");
        }

        private static MediaPickerController SetupController(MediaPickerDelegate mpDelegate, UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null)
        {
            var picker = new MediaPickerController(mpDelegate);
            picker.MediaTypes = new[] { mediaType };
            picker.SourceType = sourceType;

            if (sourceType == UIImagePickerControllerSourceType.Camera)
            {
                picker.CameraDevice = GetUICameraDevice(options.DefaultCamera);
                picker.AllowsEditing = options?.AllowCropping ?? false;

                if (options.OverlayViewProvider != null)
                {
                    var overlay = options.OverlayViewProvider();
                    if (overlay is UIView)
                    {
                        picker.CameraOverlayView = overlay as UIView;
                    }
                }
                if (mediaType == TypeImage)
                {
                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo;
                }
                else if (mediaType == TypeMovie)
                {
                    StoreVideoOptions voptions = (StoreVideoOptions)options;

                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;
                    picker.VideoQuality = GetQuailty(voptions.Quality);
                    picker.VideoMaximumDuration = voptions.DesiredLength.TotalSeconds;
                }
            }

            return picker;
        }

        private Task<MediaFile> GetMediaAsync(UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null)
        {
			
			UIViewController viewController = null;
            UIWindow window = UIApplication.SharedApplication.KeyWindow;
            if (window == null)
                throw new InvalidOperationException("There's no current active window");

            if(window.WindowLevel == UIWindowLevel.Normal)
                viewController = window.RootViewController;

            if (viewController == null)
            {
                window = UIApplication.SharedApplication.Windows.OrderByDescending(w => w.WindowLevel).FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);
                if (window == null)
                    throw new InvalidOperationException("Could not find current view controller");
                else
                    viewController = window.RootViewController;
            }

            while (viewController.PresentedViewController != null)
                viewController = viewController.PresentedViewController;

            MediaPickerDelegate ndelegate = new MediaPickerDelegate(viewController, sourceType, options);
            var od = Interlocked.CompareExchange(ref pickerDelegate, ndelegate, null);
            if (od != null)
                throw new InvalidOperationException("Only one operation can be active at at time");

            var picker = SetupController(ndelegate, sourceType, mediaType, options);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && sourceType == UIImagePickerControllerSourceType.PhotoLibrary)
            {
                ndelegate.Popover = popover = new UIPopoverController(picker);
                ndelegate.Popover.Delegate = new MediaPickerPopoverDelegate(ndelegate, picker);
                ndelegate.DisplayPopover();
            }
            else
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                {
	                picker.ModalPresentationStyle = options?.ModalPresentationStyle == MediaPickerModalPresentationStyle.OverFullScreen
		                ? UIModalPresentationStyle.OverFullScreen
		                : UIModalPresentationStyle.FullScreen;
                }
                viewController.PresentViewController(picker, true, null);
            }

            return ndelegate.Task.ContinueWith(t =>
            {
				try
				{
					popover?.Dispose();
				}
				catch
				{

				}
                popover = null;
                

                Interlocked.Exchange(ref pickerDelegate, null);

				try
				{
					picker?.Dispose();
				}
				catch
				{

				}
                return t;
            }).Unwrap();
        }

        private static UIImagePickerControllerCameraDevice GetUICameraDevice(CameraDevice device)
        {
            switch (device)
            {
                case CameraDevice.Front:
                    return UIImagePickerControllerCameraDevice.Front;
                case CameraDevice.Rear:
                    return UIImagePickerControllerCameraDevice.Rear;
                default:
                    throw new NotSupportedException();
            }
        }

        private static UIImagePickerControllerQualityType GetQuailty(VideoQuality quality)
        {
            switch (quality)
            {
                case VideoQuality.Low:
                    return UIImagePickerControllerQualityType.Low;
                case VideoQuality.Medium:
                    return UIImagePickerControllerQualityType.Medium;
                default:
                    return UIImagePickerControllerQualityType.High;
            }
        }

        static async Task CheckPermissions(params Permission[] permissions)
        {
			//See which ones we need to request.
			var permissionsToRequest = new List<Permission>();
			foreach(var permission in permissions)
			{
				var permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(permission);

				if (permissionStatus != PermissionStatus.Granted)
					permissionsToRequest.Add(permission);
            }

			//Nothing to request, Awesome!
			if (permissionsToRequest.Count == 0)
				return;

			//let's request!
			var results = await CrossPermissions.Current.RequestPermissionsAsync(permissionsToRequest.ToArray());

			//check for anything not granted, if none, Awesome!
			var notGranted = results.Where(r => r.Value != PermissionStatus.Granted);
			if (notGranted.Count() == 0)
				return;

			//Gunna need those permissions :(
			throw new MediaPermissionException(notGranted.Select(r => r.Key).ToArray());
			
        }

		const string cameraDescription = "NSCameraUsageDescription";
		const string photoDescription = "NSPhotoLibraryUsageDescription";
		const string photoAddDescription = "NSPhotoLibraryAddUsageDescription";
		const string microphoneDescription = "NSMicrophoneUsageDescription";
		void CheckUsageDescription(params string[] descriptionNames)
        {
			foreach(var description in descriptionNames)
			{

				var info = NSBundle.MainBundle.InfoDictionary;

				if (!UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
					return;

				if (!info.ContainsKey(new NSString(description)))
					throw new UnauthorizedAccessException($"On iOS 10 and higher you must set {description} in your Info.plist file to enable Authorization Requests for access!");

			}
		}
    }
}
