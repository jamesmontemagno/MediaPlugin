//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.IO;
using System.Threading.Tasks;

using Plugin.Media.Abstractions;

using CoreGraphics;
using AssetsLibrary;
using Foundation;
using UIKit;
using NSAction = global::System.Action;
using System.Globalization;
using ImageIO;
using MobileCoreServices;

namespace Plugin.Media
{
    internal class MediaPickerDelegate
        : UIImagePickerControllerDelegate
    {
        internal MediaPickerDelegate(UIViewController viewController, UIImagePickerControllerSourceType sourceType, StoreCameraMediaOptions options)
        {
            this.viewController = viewController;
            this.source = sourceType;
            this.options = options ?? new StoreCameraMediaOptions();

            if (viewController != null)
            {
                UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
                observer = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, DidRotate);
            }
        }

        public UIPopoverController Popover
        {
            get;
            set;
        }

        public UIView View
        {
            get { return viewController.View; }
        }

        public Task<MediaFile> Task
        {
            get { return tcs.Task; }
        }

        public override async void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            RemoveOrientationChangeObserverAndNotifications();

            MediaFile mediaFile;
            switch ((NSString)info[UIImagePickerController.MediaType])
            {
                case MediaImplementation.TypeImage:
                    mediaFile = await GetPictureMediaFile(info);
                    break;

                case MediaImplementation.TypeMovie:
                    mediaFile = await GetMovieMediaFile(info);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
                UIApplication.SharedApplication.StatusBarHidden = MediaImplementation.StatusBarHidden;
            }

            Dismiss(picker, () =>
            {


                tcs.TrySetResult(mediaFile);
            });
        }

        public override void Canceled(UIImagePickerController picker)
        {
            RemoveOrientationChangeObserverAndNotifications();

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
                UIApplication.SharedApplication.StatusBarHidden = MediaImplementation.StatusBarHidden;
            }

            Dismiss(picker, () =>
            {


                tcs.SetResult(null);
            });
        }

        public void DisplayPopover(bool hideFirst = false)
        {
            if (Popover == null)
                return;

            var swidth = UIScreen.MainScreen.Bounds.Width;
            var sheight = UIScreen.MainScreen.Bounds.Height;

            nfloat width = 400;
            nfloat height = 300;


            if (orientation == null)
            {
                if (IsValidInterfaceOrientation(UIDevice.CurrentDevice.Orientation))
                    orientation = UIDevice.CurrentDevice.Orientation;
                else
                    orientation = GetDeviceOrientation(this.viewController.InterfaceOrientation);
            }

            nfloat x, y;
            if (orientation == UIDeviceOrientation.LandscapeLeft || orientation == UIDeviceOrientation.LandscapeRight)
            {
                y = (swidth / 2) - (height / 2);
                x = (sheight / 2) - (width / 2);
            }
            else
            {
                x = (swidth / 2) - (width / 2);
                y = (sheight / 2) - (height / 2);
            }

            if (hideFirst && Popover.PopoverVisible)
                Popover.Dismiss(animated: false);

            Popover.PresentFromRect(new CGRect(x, y, width, height), View, 0, animated: true);
        }

        private UIDeviceOrientation? orientation;
        private NSObject observer;
        private readonly UIViewController viewController;
        private readonly UIImagePickerControllerSourceType source;
        private TaskCompletionSource<MediaFile> tcs = new TaskCompletionSource<MediaFile>();
        private readonly StoreCameraMediaOptions options;

        private bool IsCaptured
        {
            get { return source == UIImagePickerControllerSourceType.Camera; }
        }

        private void Dismiss(UIImagePickerController picker, NSAction onDismiss)
        {
            if (viewController == null)
            {
                onDismiss();
                tcs = new TaskCompletionSource<MediaFile>();
            }
            else
            {
                if (Popover != null)
                {
                    Popover.Dismiss(animated: true);
                    Popover.Dispose();
                    Popover = null;

                    onDismiss();
                }
                else
                {
                    picker.DismissViewController(true, onDismiss);
                    picker.Dispose();
                }
            }
        }

        private void RemoveOrientationChangeObserverAndNotifications()
        {
            if (viewController != null)
            {
                UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
                NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
                observer.Dispose();
            }
        }

        private void DidRotate(NSNotification notice)
        {
            UIDevice device = (UIDevice)notice.Object;
            if (!IsValidInterfaceOrientation(device.Orientation) || Popover == null)
                return;
            if (orientation.HasValue && IsSameOrientationKind(orientation.Value, device.Orientation))
                return;

            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
            {
                if (!GetShouldRotate6(device.Orientation))
                    return;
            }
            else if (!GetShouldRotate(device.Orientation))
                return;

            UIDeviceOrientation? co = orientation;
            orientation = device.Orientation;

            if (co == null)
                return;

            DisplayPopover(hideFirst: true);
        }

        private bool GetShouldRotate(UIDeviceOrientation orientation)
        {
            UIInterfaceOrientation iorientation = UIInterfaceOrientation.Portrait;
            switch (orientation)
            {
                case UIDeviceOrientation.LandscapeLeft:
                    iorientation = UIInterfaceOrientation.LandscapeLeft;
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    iorientation = UIInterfaceOrientation.LandscapeRight;
                    break;

                case UIDeviceOrientation.Portrait:
                    iorientation = UIInterfaceOrientation.Portrait;
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    iorientation = UIInterfaceOrientation.PortraitUpsideDown;
                    break;

                default: return false;
            }

            return viewController.ShouldAutorotateToInterfaceOrientation(iorientation);
        }

        private bool GetShouldRotate6(UIDeviceOrientation orientation)
        {
            if (!viewController.ShouldAutorotate())
                return false;

            UIInterfaceOrientationMask mask = UIInterfaceOrientationMask.Portrait;
            switch (orientation)
            {
                case UIDeviceOrientation.LandscapeLeft:
                    mask = UIInterfaceOrientationMask.LandscapeLeft;
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    mask = UIInterfaceOrientationMask.LandscapeRight;
                    break;

                case UIDeviceOrientation.Portrait:
                    mask = UIInterfaceOrientationMask.Portrait;
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    mask = UIInterfaceOrientationMask.PortraitUpsideDown;
                    break;

                default: return false;
            }

            return viewController.GetSupportedInterfaceOrientations().HasFlag(mask);
        }


        private async Task<MediaFile> GetPictureMediaFile(NSDictionary info)
        {
            var image = (UIImage)info[UIImagePickerController.EditedImage] ?? (UIImage)info[UIImagePickerController.OriginalImage];

            NSDictionary meta = null;
            if (source == UIImagePickerControllerSourceType.Camera)
            {
                meta = info[UIImagePickerController.MediaMetadata] as NSDictionary;
                if (meta != null && meta.ContainsKey(ImageIO.CGImageProperties.Orientation))
                {
                    var newMeta = new NSMutableDictionary();
                    newMeta.SetValuesForKeysWithDictionary(meta);
                    var newTiffDict = new NSMutableDictionary();
                    newTiffDict.SetValuesForKeysWithDictionary(meta[ImageIO.CGImageProperties.TIFFDictionary] as NSDictionary);
                    newTiffDict.SetValueForKey(meta[ImageIO.CGImageProperties.Orientation], ImageIO.CGImageProperties.TIFFOrientation);
                    newMeta[ImageIO.CGImageProperties.TIFFDictionary] = newTiffDict;
                    meta = newMeta;
                }
                var location = options.Location;
                if (meta != null && location.Latitude > 0.0)
                {
                    meta = SetGpsLocation(meta, location);
                }
            }
            else
            {
                meta = PhotoLibraryAccess.GetPhotoLibraryMetadata(info[UIImagePickerController.ReferenceUrl] as NSUrl);
            }

            string path = GetOutputPath(MediaImplementation.TypeImage,
                options.Directory ?? ((IsCaptured) ? String.Empty : "temp"),
                options.Name);

            var cgImage = image.CGImage;

            if (options.PhotoSize != PhotoSize.Full)
            {
                try
                {
                    var percent = 1.0f;
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
                        var max = Math.Max(image.CGImage.Width, image.CGImage.Height);
                        if (max > options.MaxWidthHeight)
                        {
                            percent = (float)options.MaxWidthHeight / (float)max;
                        }
                    }

                    //calculate new size
                    var width = (image.CGImage.Width * percent);
                    var height = (image.CGImage.Height * percent);

                    //begin resizing image
                    image = image.ResizeImageWithAspectRatio(width, height);
                    //update exif pixel dimiensions
                    meta[ImageIO.CGImageProperties.ExifDictionary].SetValueForKey(new NSString(width.ToString()), ImageIO.CGImageProperties.ExifPixelXDimension);
                    meta[ImageIO.CGImageProperties.ExifDictionary].SetValueForKey(new NSString(height.ToString()), ImageIO.CGImageProperties.ExifPixelYDimension);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to compress image: {ex}");
                }
            }

            //iOS quality is 0.0-1.0
            var quality = (options.CompressionQuality / 100f);
            if (meta == null)
            {
                image.AsJPEG(quality).Save(path, true);
            }
            else
            {
                var success = SaveImageWithMetadata(image, quality, meta, path);
                if (!success)
                {
                    image.AsJPEG(quality).Save(path, true);
                }
            }

            string aPath = null;
            if (source != UIImagePickerControllerSourceType.Camera)
            {

                //try to get the album path's url
                var url = (NSUrl)info[UIImagePickerController.ReferenceUrl];
                aPath = url?.AbsoluteString;
            }
            else
            {
                if (this.options.SaveToAlbum)
                {
                    try
                    {
                        var library = new ALAssetsLibrary();
                        var albumSave = await library.WriteImageToSavedPhotosAlbumAsync(cgImage, meta);
                        aPath = albumSave.AbsoluteString;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("unable to save to album:" + ex);
                    }
                }

            }

            return new MediaFile(path, () => File.OpenRead(path), albumPath: aPath);
        }

        private static NSDictionary SetGpsLocation(NSDictionary meta, Location location)
        {
            var newMeta = new NSMutableDictionary();
            newMeta.SetValuesForKeysWithDictionary(meta);
            var newGpsDict = new NSMutableDictionary();
            newGpsDict.SetValueForKey(new NSNumber(location.Latitude), ImageIO.CGImageProperties.GPSLatitude);
            newGpsDict.SetValueForKey(new NSString(location.Latitude > 0 ? "N" : "S"), ImageIO.CGImageProperties.GPSLatitudeRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Longitude), ImageIO.CGImageProperties.GPSLongitude);
            newGpsDict.SetValueForKey(new NSString(location.Longitude > 0 ? "E" : "W"), ImageIO.CGImageProperties.GPSLongitudeRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Altitude), ImageIO.CGImageProperties.GPSAltitude);
            newGpsDict.SetValueForKey(new NSNumber(0), ImageIO.CGImageProperties.GPSAltitudeRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Speed), ImageIO.CGImageProperties.GPSSpeed);
            newGpsDict.SetValueForKey(new NSString("K"), ImageIO.CGImageProperties.GPSSpeedRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Direction), ImageIO.CGImageProperties.GPSImgDirection);
            newGpsDict.SetValueForKey(new NSString("T"), ImageIO.CGImageProperties.GPSImgDirectionRef);
            newGpsDict.SetValueForKey(new NSString(location.Timestamp.ToString("hh:mm:ss")), ImageIO.CGImageProperties.GPSTimeStamp);
            newGpsDict.SetValueForKey(new NSString(location.Timestamp.ToString("yyyy:MM:dd")), ImageIO.CGImageProperties.GPSDateStamp);
            newMeta[ImageIO.CGImageProperties.GPSDictionary] = newGpsDict;
            return newMeta;
        }

        private bool SaveImageWithMetadata(UIImage image, float quality, NSDictionary meta, string path)
        {
            var imageData = image.AsJPEG(quality);
            var dataProvider = new CGDataProvider(imageData);
            var cgImageFromJpeg = CGImage.FromJPEG(dataProvider, null, false, CGColorRenderingIntent.Default);
            var imageWithExif = new NSMutableData();
            var destination = CGImageDestination.Create(imageWithExif, UTType.JPEG, 1);
            var cgImageMetadata = new CGMutableImageMetadata();
            var destinationOptions = new CGImageDestinationOptions();
            if (meta.ContainsKey(ImageIO.CGImageProperties.ExifDictionary))
            {
                destinationOptions.ExifDictionary =
                    new CGImagePropertiesExif(meta[ImageIO.CGImageProperties.ExifDictionary] as NSDictionary);
            }
            if (meta.ContainsKey(ImageIO.CGImageProperties.TIFFDictionary))
            {
                destinationOptions.TiffDictionary = new CGImagePropertiesTiff(meta[ImageIO.CGImageProperties.TIFFDictionary] as NSDictionary);
            }
            if (meta.ContainsKey(ImageIO.CGImageProperties.GPSDictionary))
            {
                destinationOptions.GpsDictionary =
                    new CGImagePropertiesGps(meta[ImageIO.CGImageProperties.GPSDictionary] as NSDictionary);
            }
            if (meta.ContainsKey(ImageIO.CGImageProperties.JFIFDictionary))
            {
                destinationOptions.JfifDictionary =
                    new CGImagePropertiesJfif(meta[ImageIO.CGImageProperties.JFIFDictionary] as NSDictionary);
            }
            if (meta.ContainsKey(ImageIO.CGImageProperties.IPTCDictionary))
            {
                destinationOptions.IptcDictionary =
                    new CGImagePropertiesIptc(meta[ImageIO.CGImageProperties.IPTCDictionary] as NSDictionary);
            }
            destination.AddImageAndMetadata(cgImageFromJpeg, cgImageMetadata, destinationOptions);
            var success = destination.Close();
            if (success)
            {
                imageWithExif.Save(path, true);
            }
            return success;
        }


        private async Task<MediaFile> GetMovieMediaFile(NSDictionary info)
        {
            NSUrl url = (NSUrl)info[UIImagePickerController.MediaURL];

            string path = GetOutputPath(MediaImplementation.TypeMovie,
                      options.Directory ?? ((IsCaptured) ? String.Empty : "temp"),
                      options.Name ?? Path.GetFileName(url.Path));

            File.Move(url.Path, path);

            string aPath = null;
            if (source != UIImagePickerControllerSourceType.Camera)
            {
                //try to get the album path's url
                var url2 = (NSUrl)info[UIImagePickerController.ReferenceUrl];
                aPath = url2?.AbsoluteString;
            }
            else
            {
                if (this.options.SaveToAlbum)
                {
                    try
                    {
                        var library = new ALAssetsLibrary();
                        var albumSave = await library.WriteVideoToSavedPhotosAlbumAsync(new NSUrl(path));
                        aPath = albumSave.AbsoluteString;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("unable to save to album:" + ex);
                    }
                }
            }

            return new MediaFile(path, () => File.OpenRead(path), albumPath: aPath);
        }

        private static string GetUniquePath(string type, string path, string name)
        {
            bool isPhoto = (type == MediaImplementation.TypeImage);
            string ext = Path.GetExtension(name);
            if (ext == String.Empty)
                ext = ((isPhoto) ? ".jpg" : ".mp4");

            name = Path.GetFileNameWithoutExtension(name);

            string nname = name + ext;
            int i = 1;
            while (File.Exists(Path.Combine(path, nname)))
                nname = name + "_" + (i++) + ext;

            return Path.Combine(path, nname);
        }

        private static string GetOutputPath(string type, string path, string name)
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path);
            Directory.CreateDirectory(path);

            if (String.IsNullOrWhiteSpace(name))
            {
                var timestamp = DateTime.Now.ToString("yyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                if (type == MediaImplementation.TypeImage)
                    name = "IMG_" + timestamp + ".jpg";
                else
                    name = "VID_" + timestamp + ".mp4";
            }

            return Path.Combine(path, GetUniquePath(type, path, name));
        }

        private static bool IsValidInterfaceOrientation(UIDeviceOrientation self)
        {
            return (self != UIDeviceOrientation.FaceUp && self != UIDeviceOrientation.FaceDown && self != UIDeviceOrientation.Unknown);
        }

        private static bool IsSameOrientationKind(UIDeviceOrientation o1, UIDeviceOrientation o2)
        {
            if (o1 == UIDeviceOrientation.FaceDown || o1 == UIDeviceOrientation.FaceUp)
                return (o2 == UIDeviceOrientation.FaceDown || o2 == UIDeviceOrientation.FaceUp);
            if (o1 == UIDeviceOrientation.LandscapeLeft || o1 == UIDeviceOrientation.LandscapeRight)
                return (o2 == UIDeviceOrientation.LandscapeLeft || o2 == UIDeviceOrientation.LandscapeRight);
            if (o1 == UIDeviceOrientation.Portrait || o1 == UIDeviceOrientation.PortraitUpsideDown)
                return (o2 == UIDeviceOrientation.Portrait || o2 == UIDeviceOrientation.PortraitUpsideDown);

            return false;
        }

        private static UIDeviceOrientation GetDeviceOrientation(UIInterfaceOrientation self)
        {
            switch (self)
            {
                case UIInterfaceOrientation.LandscapeLeft:
                    return UIDeviceOrientation.LandscapeLeft;
                case UIInterfaceOrientation.LandscapeRight:
                    return UIDeviceOrientation.LandscapeRight;
                case UIInterfaceOrientation.Portrait:
                    return UIDeviceOrientation.Portrait;
                case UIInterfaceOrientation.PortraitUpsideDown:
                    return UIDeviceOrientation.PortraitUpsideDown;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
