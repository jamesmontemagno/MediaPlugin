
#if !MACCATALYST
using System;
using CoreImage;
using Foundation;
using Photos;
using UIKit;

namespace Plugin.Media
{
    /// <summary>
    /// Accesst library
    /// </summary>
    public static class PhotoLibraryAccess
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static NSDictionary GetPhotoLibraryMetadata(NSUrl url)
        {
            NSDictionary meta = null;

            var image = PHAsset.FetchAssets(new NSUrl[] { url }, new PHFetchOptions()).firstObject as PHAsset;
            if (image == null)
                return null;

            try
            {
                var imageManager = PHImageManager.DefaultManager;
                var requestOptions = new PHImageRequestOptions
                {
                    Synchronous = true,
                    NetworkAccessAllowed = true,
                    DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
                };

                if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                {
                    imageManager.RequestImageDataAndOrientation(image, requestOptions, (data, dataUti, orientation, info) =>
                    {
                        try
                        {
                            var fullimage = CIImage.FromData(data);
                            if (fullimage?.Properties != null)
                            {
                                meta = new NSMutableDictionary
                                {
                                    [ImageIO.CGImageProperties.Orientation] = NSNumber.FromNInt((int)(fullimage.Properties.Orientation ?? CIImageOrientation.TopLeft)),
                                    [ImageIO.CGImageProperties.ExifDictionary] = fullimage.Properties.Exif?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.TIFFDictionary] = fullimage.Properties.Tiff?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.GPSDictionary] = fullimage.Properties.Gps?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.IPTCDictionary] = fullimage.Properties.Iptc?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.JFIFDictionary] = fullimage.Properties.Jfif?.Dictionary ?? new NSDictionary()
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                    });
                }
                else
                {
                    imageManager.RequestImageData(image, requestOptions, (data, dataUti, orientation, info) =>
                    {
                        try
                        {
                            var fullimage = CIImage.FromData(data);
                            if (fullimage?.Properties != null)
                            {
                                meta = new NSMutableDictionary
                                {
                                    [ImageIO.CGImageProperties.Orientation] = NSNumber.FromNInt((int)(fullimage.Properties.Orientation ?? CIImageOrientation.TopLeft)),
                                    [ImageIO.CGImageProperties.ExifDictionary] = fullimage.Properties.Exif?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.TIFFDictionary] = fullimage.Properties.Tiff?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.GPSDictionary] = fullimage.Properties.Gps?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.IPTCDictionary] = fullimage.Properties.Iptc?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.JFIFDictionary] = fullimage.Properties.Jfif?.Dictionary ?? new NSDictionary()
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                    });
                }

            }
            catch
            {

            }

            return meta;
        }
    }
}
#endif