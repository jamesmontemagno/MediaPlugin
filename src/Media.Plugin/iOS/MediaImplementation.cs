using Plugin.Media.Abstractions;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Linq;

using UIKit;
using Foundation;

using System.Collections.Generic;

#if NET6_0_OR_GREATER
using Permissions = Microsoft.Maui.ApplicationModel.Permissions;
using PermissionStatus = Microsoft.Maui.ApplicationModel.PermissionStatus;
#else
using Permissions = Xamarin.Essentials.Permissions;
using PermissionStatus = Xamarin.Essentials.PermissionStatus;
#endif

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

            foreach (var type in availableCameraMedia.Concat(avaialbleLibraryMedia))
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
        public async Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null, CancellationToken token = default(CancellationToken))
        {
            if (!IsPickPhotoSupported)
                throw new NotSupportedException();


            //Does not need permission on iOS 11
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                CheckUsageDescription(photoDescription);

                await CheckPermissions(nameof(Permissions.Photos));
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

            return await GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeImage, cameraOptions, token);
        }

        public async Task<List<MediaFile>> PickPhotosAsync(PickMediaOptions options = null, MultiPickerOptions pickerOptions = null, CancellationToken token = default(CancellationToken))
        {
            if (!IsPickPhotoSupported)
                throw new NotSupportedException();

            //Does not need permission on iOS 11
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                CheckUsageDescription(photoDescription);

                await CheckPermissions(nameof(Permissions.Photos));
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

            return await GetMediasAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeImage, cameraOptions, pickerOptions, token);
        }

        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options, CancellationToken token = default(CancellationToken))
        {
            if (!IsTakePhotoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            CheckUsageDescription(cameraDescription);
            if (options.SaveToAlbum)
                CheckUsageDescription(photoAddDescription);

            VerifyCameraOptions(options);

            var permissionsToCheck = new List<string> { nameof(Permissions.Camera) };
            if (options.SaveToAlbum)
                permissionsToCheck.Add(nameof(Permissions.Photos));

            await CheckPermissions(permissionsToCheck.ToArray());

            return await GetMediaAsync(UIImagePickerControllerSourceType.Camera, TypeImage, options, token);
        }


        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        public async Task<MediaFile> PickVideoAsync(CancellationToken token = default(CancellationToken))
        {
            if (!IsPickVideoSupported)
                throw new NotSupportedException();

            var backgroundTask = UIApplication.SharedApplication.BeginBackgroundTask(() => { });


            //Not needed on iOS 11 since it runs in different process
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                CheckUsageDescription(photoDescription);
                await CheckPermissions(nameof(Permissions.Photos));
            }

            var media = await GetMediaAsync(UIImagePickerControllerSourceType.PhotoLibrary, TypeMovie, token: token);

            UIApplication.SharedApplication.EndBackgroundTask(backgroundTask);

            return media;
        }


        /// <summary>
        /// Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options, CancellationToken token = default(CancellationToken))
        {
            if (!IsTakeVideoSupported)
                throw new NotSupportedException();
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            CheckUsageDescription(cameraDescription, microphoneDescription);

            if (options.SaveToAlbum)
                CheckUsageDescription(photoAddDescription);

            VerifyCameraOptions(options);

            var permissionsToCheck = new List<string> { nameof(Permissions.Camera), nameof(Permissions.Microphone) };
            if (options.SaveToAlbum)
                permissionsToCheck.Add(nameof(Permissions.Photos));

            await CheckPermissions(permissionsToCheck.ToArray());

            return await GetMediaAsync(UIImagePickerControllerSourceType.Camera, TypeMovie, options, token);
        }

        UIPopoverController popover;
        UIImagePickerControllerDelegate pickerDelegate;
        /// <summary>
        /// image type
        /// </summary>
        public const string TypeImage = "public.image";
        /// <summary>
        /// movie type
        /// </summary>
        public const string TypeMovie = "public.movie";

        void VerifyOptions(StoreMediaOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (options.Directory != null && Path.IsPathRooted(options.Directory))
                throw new ArgumentException("options.Directory must be a relative path", "options");
        }

        void VerifyCameraOptions(StoreCameraMediaOptions options)
        {
            VerifyOptions(options);
            if (!Enum.IsDefined(typeof(CameraDevice), options.DefaultCamera))
                throw new ArgumentException("options.Camera is not a member of CameraDevice");
        }

        static MediaPickerController SetupController(MediaPickerDelegate mpDelegate, UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null)
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
                    var voptions = (StoreVideoOptions)options;

                    picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;
                    picker.VideoQuality = GetQuailty(voptions.Quality);
                    picker.VideoMaximumDuration = voptions.DesiredLength.TotalSeconds;
                }
            }

            return picker;
        }

        Task<MediaFile> GetMediaAsync(UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null, CancellationToken token = default(CancellationToken))
        {

            var viewController = GetHostViewController();

            if (token.IsCancellationRequested)
                return Task.FromResult((MediaFile)null);

            var ndelegate = new MediaPickerDelegate(viewController, sourceType, options, token);
            var od = Interlocked.CompareExchange(ref pickerDelegate, ndelegate, null);
            if (od != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            var picker = SetupController(ndelegate, sourceType, mediaType, options);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && sourceType == UIImagePickerControllerSourceType.PhotoLibrary)
            {
                ndelegate.Popover = popover = new UIPopoverController(picker);
                ndelegate.Popover.Delegate = new MediaPickerPopoverDelegate(ndelegate, picker);
                ndelegate.DisplayPopover();

                token.Register(() =>
                {
                    if (popover == null)
                        return;
                    NSRunLoop.Main.BeginInvokeOnMainThread(() =>
                    {
                        ndelegate.Popover.Dismiss(true);
                        ndelegate.CancelTask();
                    });
                });
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

                token.Register(() =>
                {
                    if (picker == null)
                        return;

                    NSRunLoop.Main.BeginInvokeOnMainThread(() =>
                    {
                        picker.DismissModalViewController(true);
                        ndelegate.CancelTask();
                    });
                });
            }

            return ndelegate.Task.ContinueWith(t =>
            {
                Dismiss(popover, picker);

                return t.Result == null ? null : t.Result.FirstOrDefault();
            });
        }

        Task<List<MediaFile>> GetMediasAsync(UIImagePickerControllerSourceType sourceType, string mediaType, StoreCameraMediaOptions options = null, MultiPickerOptions pickerOptions = null, CancellationToken token = default(CancellationToken))
        {

#if MACCATALYST
            return Task.FromResult(new List<MediaFile>());
#else
            var viewController = GetHostViewController();
            
            if (options == null)
                options = new StoreCameraMediaOptions();

            var ndelegate = new MediaPickerDelegate(viewController, sourceType, options, token);
            var od = Interlocked.CompareExchange(ref pickerDelegate, ndelegate, null);
            if (od != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            var picker = ELCImagePickerViewController.Create(options, pickerOptions);

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
                    picker.ModalPresentationStyle = UIModalPresentationStyle.OverCurrentContext;
                }
                viewController.PresentViewController(picker, true, null);
            }

            // TODO: Make this use the existing Delegate?
            return picker.Completion.ContinueWith(t =>
            {
                Dismiss(popover, picker);
                picker.BeginInvokeOnMainThread(() =>
                {
                    picker.DismissViewController(true, null);
                });

                if (t.IsCanceled || t.Exception != null)
                {
                    return Task.FromResult(new List<MediaFile>());
                }

                var files = t.Result;
                Parallel.ForEach(files, mediaFile =>
                {
                    ResizeAndCompressImage(options, mediaFile, Path.GetExtension(mediaFile.Path).Replace(".", string.Empty));
                });

                return t;
            }).Unwrap();

#endif
        }

        static void ResizeAndCompressImage(StoreCameraMediaOptions options, MediaFile mediaFile, string pathExtension)
        {
            var image = UIImage.FromFile(mediaFile.Path);
            var percent = 1.0f;
            if (options.PhotoSize != PhotoSize.Full)
            {
                try
                {
                    switch (options.PhotoSize)
                    {
                        case PhotoSize.Large:
                            percent = .75f;
                            break;
                        case PhotoSize.Medium:
                            percent = .5f;
                            break;
                        case PhotoSize.Small:
                            percent = .25f;
                            break;
                        case PhotoSize.Custom:
                            percent = (float)options.CustomPhotoSize / 100f;
                            break;
                    }

                    if (options.PhotoSize == PhotoSize.MaxWidthHeight && options.MaxWidthHeight.HasValue)
                    {
                        var max = Math.Max(image.Size.Width, image.Size.Height);
                        if (max > options.MaxWidthHeight.Value)
                        {
                            percent = (float)options.MaxWidthHeight.Value / (float)max;
                        }
                    }

                    if (percent < 1.0f)
                    {
                        //begin resizing image
                        image = image.ResizeImageWithAspectRatio(percent);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to compress image: {ex}");
                }
            }
            NSDictionary meta = null;
            if (options.SaveMetaData)
            {
                try
                {
#if !MACCATALYST
                    meta = PhotoLibraryAccess.GetPhotoLibraryMetadata(new NSUrl(mediaFile.AlbumPath));
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to get metadata: {ex}");
                }
            }
            //iOS quality is 0.0-1.0
            var quality = (options.CompressionQuality / 100f);
            var savedImage = false;
            if (meta != null)
                savedImage = MediaPickerDelegate.SaveImageWithMetadata(image, quality, meta, mediaFile.Path, pathExtension);

            if (!savedImage)
            {
                if (pathExtension == "png")
                    image.AsPNG().Save(mediaFile.Path, true);
                else
                    image.AsJPEG(quality).Save(mediaFile.Path, true);
            }

            image?.Dispose();
            image = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        void Dismiss(UIPopoverController popover, UIViewController picker)
        {
            if (popover != null)
            {
                popover.Dispose();
                popover = null;
            }

            try
            {
                picker?.Dispose();
            }
            catch
            {

            }
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Default);
            Interlocked.Exchange(ref pickerDelegate, null);
        }

        static UIViewController GetHostViewController()
        {
            UIViewController viewController = null;
            var window = UIApplication.SharedApplication.KeyWindow;
            if (window == null)
                throw new InvalidOperationException("There's no current active window");

            if (window.WindowLevel == UIWindowLevel.Normal)
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

            return viewController;
        }

        static UIImagePickerControllerCameraDevice GetUICameraDevice(CameraDevice device)
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

        static UIImagePickerControllerQualityType GetQuailty(VideoQuality quality)
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

        static async Task CheckPermissions(params string[] permissions)
        {
            //See which ones we need to request.
            var permissionsToRequest = new List<string>();
            foreach (var permission in permissions)
            {
                var permissionStatus = PermissionStatus.Unknown;
                switch (permission)
                {
                    case nameof(Permissions.Camera):
                        permissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                        break;
                    case nameof(Permissions.Photos):
                        permissionStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
                        break;
                    case nameof(Permissions.Microphone):
                        permissionStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                        break;
                }

                if (permissionStatus != PermissionStatus.Granted)
                    permissionsToRequest.Add(permission);
            }

            //Nothing to request, Awesome!
            if (permissionsToRequest.Count == 0)
                return;

            var results = new Dictionary<string, PermissionStatus>();
            foreach (var permission in permissions)
            {
                switch (permission)
                {
                    case nameof(Permissions.Camera):
                        results.Add(permission, await Permissions.RequestAsync<Permissions.Camera>());
                        break;
                    case nameof(Permissions.Photos):
                        results.Add(permission, await Permissions.RequestAsync<Permissions.Photos>());
                        break;
                    case nameof(Permissions.Microphone):
                        results.Add(permission, await Permissions.RequestAsync<Permissions.Microphone>());
                        break;
                }
            }

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
            foreach (var description in descriptionNames)
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
