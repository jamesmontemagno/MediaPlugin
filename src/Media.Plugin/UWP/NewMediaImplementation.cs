using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;

using Plugin.Media.Abstractions;
using System.Diagnostics;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Plugin.Media
{
    /// <summary>
    /// Implementation for Media
    /// </summary>
    public class NewMediaImplementation : IMedia
    {
        static readonly IEnumerable<string> supportedVideoFileTypes = new List<string> { ".mp4", ".wmv", ".avi" };
        static readonly IEnumerable<string> supportedImageFileTypes = new List<string> { ".jpeg", ".jpg", ".png", ".gif", ".bmp" };
        /// <summary>
        /// Implementation
        /// </summary>
        public NewMediaImplementation()
        {
            watcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
            watcher.Added += OnDeviceAdded;
            watcher.Updated += OnDeviceUpdated;
            watcher.Removed += OnDeviceRemoved;
            watcher.Start();
        }

        bool initialized = false;
        /// <summary>
        /// Initialize camera
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Initialize()
        {
            if (initialized)
                return true;

            try
            {
                var info = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask().ConfigureAwait(false);
                lock (devices)
                {
                    foreach (var device in info)
                    {
                        if (device.IsEnabled)
                            devices.Add(device.Id);
                    }

                    isCameraAvailable = (devices.Count > 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to detect cameras: " + ex);
            }

            initialized = true;
            return true;
        }

        /// <inheritdoc/>
        public bool IsCameraAvailable
        {
            get
            {
                if (!initialized)
                    throw new InvalidOperationException("You must call Initialize() before calling any properties.");

                return isCameraAvailable;
            }
        }
        /// <inheritdoc/>
        public bool IsTakePhotoSupported => true;

        /// <inheritdoc/>
        public bool IsPickPhotoSupported => true;

        /// <inheritdoc/>
        public bool IsTakeVideoSupported => true;

        /// <inheritdoc/>
        public bool IsPickVideoSupported => true;

        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options, CancellationToken token = default(CancellationToken))
        {
            if (!initialized)
                await Initialize();

            if (!IsCameraAvailable)
                throw new NotSupportedException();

            options.VerifyOptions();

            var capture = new CameraCaptureUI();
            //capture.PhotoSettings.CroppedAspectRatio = new Size(16, 9);
            capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            capture.PhotoSettings.MaxResolution = GetMaxResolution(options.PhotoSize, options.CustomPhotoSize, options.MaxWidthHeight ?? 0);
            if (options.AllowCropping ?? false)
            {
                capture.PhotoSettings.AllowCropping = true;
                capture.PhotoSettings.CroppedAspectRatio = new Size(16, 9);
            }
            else
                capture.PhotoSettings.AllowCropping = false;

            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (result == null) return null;

            if (options.SaveToAlbum) await SaveToAlbum(result, KnownFolders.SavedPictures, options.Directory, options.Name);

            if (IsValidFileName(options.Name))
            {
                var name = EnsureCorrectExtension(options.Name, result.FileType);
                await result.RenameAsync(name, NameCollisionOption.GenerateUniqueName);
            }

            await ResizeAsync(result, options);

            return MediaFileFromFile(result);
        }

        /// <summary>
        /// Picks a photo from the default gallery
        /// </summary>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file or null if canceled</returns>
        public async Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null, CancellationToken token = default(CancellationToken))
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;

            foreach (var filter in supportedImageFileTypes)
                picker.FileTypeFilter.Add(filter);

            var result = await picker.PickSingleFileAsync();
            if (result == null)
                return null;

            var copy = await CopyToLocalAndResizeAsync(result, options);
            if (copy is null)
                return MediaFileFromFile(result);
            else
                return MediaFileFromFile(copy);
        }

        public async Task<List<MediaFile>> PickPhotosAsync(PickMediaOptions options = null, MultiPickerOptions pickerOptions = null, CancellationToken token = default(CancellationToken))
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;

            foreach (var filter in supportedImageFileTypes)
                picker.FileTypeFilter.Add(filter);

            var result = await picker.PickMultipleFilesAsync();
            if (result == null)
                return null;

            var ret = new List<MediaFile>();
            foreach (var file in result)
            {
                var copy = await CopyToLocalAndResizeAsync(file, options);
                if (copy is null)
                    ret.Add(MediaFileFromFile(file));
                else
                    ret.Add(MediaFileFromFile(copy));
            }

            return ret;
        }

        MediaFile MediaFileFromFile(StorageFile file)
        {
            var aPath = file.Path;
            var path = file.Path;

            return new MediaFile(path, () => file.OpenStreamForReadAsync().Result, albumPath: aPath);
        }

        async Task SaveToAlbum(StorageFile file, StorageFolder folder, string directory = null, string name = null)
        {
            try
            {
                if (IsValidPathName(directory))
                    folder = await folder.CreateFolderAsync(directory, CreationCollisionOption.GenerateUniqueName);
                var destinationFile = await folder.CreateFileAsync(IsValidFileName(name) ? EnsureCorrectExtension(name, file.FileType) : file.Name, CreationCollisionOption.GenerateUniqueName);
                using var sourceStream = await file.OpenReadAsync();
                using var sourceInputStream = sourceStream.GetInputStreamAt(0);
                using var destinationStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite);
                using var destinationOutputStream = destinationStream.GetOutputStreamAt(0);
                await RandomAccessStream.CopyAndCloseAsync(sourceInputStream, destinationStream);
            }
            catch (UnauthorizedAccessException)
            {
#if DEBUG
                Debug.WriteLine("UnauthorizedAccessException: You have to give the permission to acces Pictures Library!");
#endif
            }
        }

        string EnsureCorrectExtension(string name, string extension)
        {
            if (!string.Equals(Path.GetExtension(name), extension, StringComparison.InvariantCultureIgnoreCase))
                return Path.GetFileNameWithoutExtension(name) + extension;
            return name;
        }

        bool IsValidFileName(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;

        bool IsValidPathName(string name) => !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidPathChars()) == -1;

        CameraCaptureUIMaxPhotoResolution GetMaxResolution(PhotoSize photoSize, int customPhotoSize, int maxWidthHeight)
        {
            if (photoSize == PhotoSize.Custom)
            {
                if (customPhotoSize <= 25)
                    photoSize = PhotoSize.Small;
                else if (customPhotoSize <= 50)
                    photoSize = PhotoSize.Medium;
                else if (customPhotoSize <= 75)
                    photoSize = PhotoSize.Large;
                else
                    photoSize = PhotoSize.Large;
            }
            switch (photoSize)
            {
                case PhotoSize.MaxWidthHeight:
                    return GetMaxResolutionFromMaxWidthHeight(maxWidthHeight);
                case PhotoSize.Full:
                    return CameraCaptureUIMaxPhotoResolution.HighestAvailable;
                case PhotoSize.Large:
                    return CameraCaptureUIMaxPhotoResolution.Large3M;
                case PhotoSize.Medium:
                    return CameraCaptureUIMaxPhotoResolution.MediumXga;
                case PhotoSize.Small:
                    return CameraCaptureUIMaxPhotoResolution.SmallVga;

            }

            return CameraCaptureUIMaxPhotoResolution.HighestAvailable;
        }

        async Task<StorageFile> CopyToLocalAndResizeAsync(StorageFile file, PickMediaOptions options)
        {
            try
            {
                var fileNameNoEx = Path.GetFileNameWithoutExtension(file.Path);
                var copy = await file.CopyAsync(ApplicationData.Current.TemporaryFolder,
                    fileNameNoEx + file.FileType, NameCollisionOption.GenerateUniqueName);

                await ResizeAsync(copy, options);

                return copy;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine("unable to save to app directory:" + ex);
                throw;
#else
                return null;
#endif
            }
        }

        /// <summary>
        /// Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options, CancellationToken token = default(CancellationToken))
        {
            if (!initialized)
                await Initialize();

            if (!IsCameraAvailable)
                throw new NotSupportedException();

            options.VerifyOptions();

            var capture = new CameraCaptureUI();
            capture.VideoSettings.MaxResolution = GetResolutionFromQuality(options.Quality);
            capture.VideoSettings.AllowTrimming = options?.AllowCropping ?? true;

            if (capture.VideoSettings.AllowTrimming)
                capture.VideoSettings.MaxDurationInSeconds = (float)options.DesiredLength.TotalSeconds;

            capture.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

            var result = await capture.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (result == null)
                return null;

            string aPath = null;
            if (options.SaveToAlbum) await SaveToAlbum(result, KnownFolders.VideosLibrary, options.Directory, options.Name);

            return new MediaFile(result.Path, () => result.OpenStreamForReadAsync().Result, albumPath: aPath);
        }

        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <param name="token">Cancellation token (currently ignored)</param>
        /// <returns>Media file of video or null if canceled</returns>
        public async Task<MediaFile> PickVideoAsync(CancellationToken token = default(CancellationToken))
        {
            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.VideosLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };

            foreach (var filter in supportedVideoFileTypes)
                picker.FileTypeFilter.Add(filter);

            var result = await picker.PickSingleFileAsync();
            if (result == null)
                return null;

            var aPath = result.Path;
            var path = result.Path;
            StorageFile copy = null;
            //copy local
            try
            {
                var fileNameNoEx = Path.GetFileNameWithoutExtension(aPath);
                copy = await result.CopyAsync(ApplicationData.Current.LocalFolder,
                    fileNameNoEx + result.FileType, NameCollisionOption.GenerateUniqueName);

                path = copy.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("unable to save to app directory:" + ex);
            }

            return new MediaFile(path, () =>
            {
                if (copy != null)
                    return copy.OpenStreamForReadAsync().Result;

                return result.OpenStreamForReadAsync().Result;
            }, albumPath: aPath);
        }

        readonly HashSet<string> devices = new HashSet<string>();
        readonly DeviceWatcher watcher;
        bool isCameraAvailable;


        static CameraCaptureUIMaxVideoResolution GetResolutionFromQuality(VideoQuality quality)
        {
            switch (quality)
            {
                case VideoQuality.High:
                    return CameraCaptureUIMaxVideoResolution.HighestAvailable;
                case VideoQuality.Medium:
                    return CameraCaptureUIMaxVideoResolution.StandardDefinition;
                case VideoQuality.Low:
                    return CameraCaptureUIMaxVideoResolution.LowDefinition;
                default:
                    return CameraCaptureUIMaxVideoResolution.HighestAvailable;
            }
        }

        static CameraCaptureUIMaxPhotoResolution GetMaxResolutionFromMaxWidthHeight(int maxWidthHeight)
        {
            if (maxWidthHeight > 2560)
                return CameraCaptureUIMaxPhotoResolution.HighestAvailable;
            if (maxWidthHeight > 1920)
                return CameraCaptureUIMaxPhotoResolution.VeryLarge5M;
            else if (maxWidthHeight > 1024)
                return CameraCaptureUIMaxPhotoResolution.Large3M;
            else if (maxWidthHeight > 320)
                return CameraCaptureUIMaxPhotoResolution.MediumXga;

            return CameraCaptureUIMaxPhotoResolution.SmallVga;
        }

        /// <summary>
        ///  Rotate an image if required and saves it back to disk.
        /// </summary>
        /// <param name="filePath">The file image path</param>
        /// <param name="mediaOptions">The options.</param>
        /// <param name="exif">original metadata</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        Task<bool> ResizeAsync(StorageFile file, PickMediaOptions mediaOptions)
        {
            return ResizeAsync(
                file,
                mediaOptions != null
                    ? new StoreCameraMediaOptions
                    {
                        PhotoSize = mediaOptions.PhotoSize,
                        CompressionQuality = mediaOptions.CompressionQuality,
                        CustomPhotoSize = mediaOptions.CustomPhotoSize,
                        MaxWidthHeight = mediaOptions.MaxWidthHeight,
                        RotateImage = mediaOptions.RotateImage,
                        SaveMetaData = mediaOptions.SaveMetaData
                    }
                    : new StoreCameraMediaOptions());
        }

        /// <summary>
        /// Resize Image Async
        /// </summary>
        /// <param name="filePath">The file image path</param>
        /// <param name="photoSize">Photo size to go to.</param>
        /// <param name="quality">Image quality (1-100)</param>
        /// <param name="customPhotoSize">Custom size in percent</param>
        /// <param name="exif">original metadata</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        Task<bool> ResizeAsync(StorageFile file, StoreCameraMediaOptions mediaOptions)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));

            try
            {
                var photoSize = mediaOptions.PhotoSize;
                if (photoSize == PhotoSize.Full)
                    return Task.FromResult(false);

                var customPhotoSize = mediaOptions.CustomPhotoSize;
                var quality = mediaOptions.CompressionQuality;
                return Task.Run(async () =>
                {
                    try
                    {
                        var percent = 1.0f;
                        switch (photoSize)
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
                                percent = customPhotoSize / 100f;
                                break;
                        }

                        BitmapDecoder decoder;
                        using (var stream = await file.OpenAsync(FileAccessMode.Read))
                            decoder = await BitmapDecoder.CreateAsync(stream);

                        using var bitmap = await decoder.GetSoftwareBitmapAsync();

                        if (mediaOptions.PhotoSize == PhotoSize.MaxWidthHeight && mediaOptions.MaxWidthHeight.HasValue)
                        {
                            var max = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);
                            if (max > mediaOptions.MaxWidthHeight)
                            {
                                percent = (float)mediaOptions.MaxWidthHeight / max;
                            }
                        }

                        var finalWidth = Convert.ToUInt32(bitmap.PixelWidth * percent);
                        var finalHeight = Convert.ToUInt32(bitmap.PixelHeight * percent);

                        using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            var propertySet = new BitmapPropertySet();
                            var qualityValue = new BitmapTypedValue(mediaOptions.CompressionQuality / 100.0, PropertyType.Single);
                            propertySet.Add("ImageQuality", qualityValue);

                            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream, propertySet);
                            encoder.SetSoftwareBitmap(bitmap);
                            encoder.BitmapTransform.ScaledWidth = finalWidth;
                            encoder.BitmapTransform.ScaledHeight = finalHeight;
                            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                            encoder.IsThumbnailGenerated = true;

                            var tryAgain = false;
                            try
                            {
                                await encoder.FlushAsync();
                            }
                            catch (Exception err)
                            {
                                const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                                switch (err.HResult)
                                {
                                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                                        // If the encoder does not support writing a thumbnail, then try again
                                        // but disable thumbnail generation.
                                        encoder.IsThumbnailGenerated = false;
                                        tryAgain = true;
                                        break;
                                    default:
                                        throw;
                                }
                            }

                            if (tryAgain)
                            {
                                await encoder.FlushAsync();
                            }
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        throw ex;
#else
                        return false;
#endif
                    }
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                return Task.FromResult(false);
#endif
            }
        }

        void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (!update.Properties.TryGetValue("System.Devices.InterfaceEnabled", out var value))
                return;

            lock (devices)
            {
                if ((bool)value)
                    devices.Add(update.Id);
                else
                    devices.Remove(update.Id);

                isCameraAvailable = devices.Count > 0;
            }
        }

        void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            lock (devices)
            {
                devices.Remove(update.Id);
                if (devices.Count == 0)
                    isCameraAvailable = false;
            }
        }

        void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            if (!device.IsEnabled)
                return;

            lock (devices)
            {
                devices.Add(device.Id);
                isCameraAvailable = true;
            }
        }
    }
}