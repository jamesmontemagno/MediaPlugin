using CoreGraphics;
using System;
using System.Drawing;
using UIKit;
using CoreImage;
using Foundation;

namespace Plugin.Media
{
    /// <summary>
    /// Static mathods for UIImage
    /// </summary>
    public static class UIImageExtensions
    {
        /// <summary>
        /// Resize image maintain aspect ratio
        /// </summary>
        /// <param name="imageSource"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static UIImage ResizeImageWithAspectRatio(this UIImage imageSource, float scale)
        {
            if (scale > 1.0f)
                return imageSource;


            using var c = CIContext.Create();
            var sourceImage = CIImage.FromCGImage(imageSource.CGImage);
            var orientation = imageSource.Orientation;
            imageSource?.Dispose();

            CILanczosScaleTransform transform = null;
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                transform = new CILanczosScaleTransform
                {
                    Scale = scale,
                    InputImage = sourceImage,
                    AspectRatio = 1.0f
                };
            }
            else
            {
#if NET6_0_OR_GREATER

                return imageSource;
#else
                transform = new CILanczosScaleTransform
                {
                    Scale = scale,
#pragma warning disable CS0618 // Type or member is obsolete
                    Image = sourceImage,
#pragma warning restore CS0618 // Type or member is obsolete
                    AspectRatio = 1.0f
                };
#endif
            }

            var output = transform.OutputImage;
            using var cgi = c.CreateCGImage(output, output.Extent);
            transform?.Dispose();
            output?.Dispose();
            sourceImage?.Dispose();

            return UIImage.FromImage(cgi, 1.0f, orientation);
        }

        /// <summary>
        /// Resize image to maximum size
        /// keeping the aspect ratio
        /// </summary>
        public static UIImage ResizeImageWithAspectRatio(this UIImage sourceImage, float maxWidth, float maxHeight)
        {
            var sourceSize = sourceImage.Size;
            var maxResizeFactor = Math.Max(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);
            if (maxResizeFactor > 1)
                return sourceImage;
            var width = maxResizeFactor * sourceSize.Width;
            var height = maxResizeFactor * sourceSize.Height;
            UIGraphics.BeginImageContext(new CGSize(width, height));
            sourceImage.Draw(new CGRect(0, 0, width, height));
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        /// <summary>
        /// Resize image, but ignore the aspect ratio
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static UIImage ResizeImage(this UIImage sourceImage, float width, float height)
        {
            UIGraphics.BeginImageContext(new CGSize(width, height));
            sourceImage.Draw(new CGRect(0, 0, width, height));
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        /// <summary>
        /// Crop image to specitic size and at specific coordinates
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="crop_x"></param>
        /// <param name="crop_y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static UIImage CropImage(this UIImage sourceImage, int crop_x, int crop_y, int width, int height)
        {
            var imgSize = sourceImage.Size;
            UIGraphics.BeginImageContext(new CGSize(width, height));
            var context = UIGraphics.GetCurrentContext();
            var clippedRect = new CGRect(0, 0, width, height);
            context.ClipToRect(clippedRect);
            var drawRect = new CGRect(-crop_x, -crop_y, imgSize.Width, imgSize.Height);
            sourceImage.Draw(drawRect);
            var modifiedImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return modifiedImage;
        }
    }
}