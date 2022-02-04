using System;

namespace Plugin.Media.Abstractions
{
    /// <summary>
    /// Media Options
    /// </summary>
    public class StoreMediaOptions
    {
        /// <summary>
        /// 
        /// </summary>
        protected StoreMediaOptions()
        {
        }

        /// <summary>
        /// Directory name
        /// </summary>
        public string Directory
        {
            get;
            set;
        }

        /// <summary>
        /// File name
        /// </summary>
        public string Name
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Camera device
    /// </summary>
    public enum CameraDevice
    {
        /// <summary>
        /// Back of device
        /// </summary>
        Rear,
        /// <summary>
        /// Front facing of device
        /// </summary>
        Front
    }

    /// <summary>
    /// Specifies the media picker's modal presentation style.
    /// Only applies to iOS.
    /// </summary>
    public enum MediaPickerModalPresentationStyle
    {
        /// <summary>
        /// This is the equivalent of presenting the media picker with UIKit.UIModalPresentationStyle.FullScreen style.
        /// Will remove the views of the underlying view controller when presenting the media picker.
        /// Only applies to iOS.
        /// </summary>
        FullScreen,

        /// <summary>
        /// This is the equivalent of presenting the media picker with UIKit.UIModalPresentationStyle.OverFullScreen style.
        /// Will keep the views of the underlying view controller when presenting the media picker.
        /// Only applies to iOS.
        /// </summary>
        OverFullScreen
    }

    /// <summary>
    /// 
    /// </summary>
    public class PickMediaOptions
    {
        /// <summary>
        /// Gets or sets the the max width or height of the image.
        /// The image will aspect resize to the MaxWidthHeight as the max size of the image height or width. 
        /// This value is only used if PhotoSize is PhotoSize.MaxWidthHeight 
        /// </summary>
        /// <value>The max width or height of the image.</value>
        public int? MaxWidthHeight { get; set; }

        /// <summary>
        /// Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;

        int customPhotoSize = 100;
        /// <summary>
        /// The custom photo size to use, 100 full size (same as Full),
        /// and 1 being smallest size at 1% of original
        /// Default is 100
        /// </summary>
        public int CustomPhotoSize
        {
            get { return customPhotoSize; }
            set
            {
                if (value > 100)
                    customPhotoSize = 100;
                else if (value < 1)
                    customPhotoSize = 1;
                else
                    customPhotoSize = value;
            }
        }

        int quality = 100;
        /// <summary>
        /// The compression quality to use, 0 is the maximum compression (worse quality),
        /// and 100 minimum compression (best quality)
        /// Default is 100
        /// </summary>
        public int CompressionQuality
        {
            get { return quality; }
            set
            {
                if (value > 100)
                    quality = 100;
                else if (value < 0)
                    quality = 0;
                else
                    quality = value;
            }
        }

        bool rotateImage = true;
        /// <summary>
        /// Should the library rotate image according to received exif orientation.
        /// Set to true by default.
        /// </summary>
        public bool RotateImage
        {
            get { return rotateImage; }
            set { rotateImage = value; }
        }

        bool saveMetaData = true;
        /// <summary>
        /// Saves metadate/exif data from the original file.
        /// </summary>
        public bool SaveMetaData
        {
            get { return saveMetaData; }
            set { saveMetaData = value; }
        }

        /// <summary>
        /// Specifies the media picker's modal presentation style.
        /// Only applies to iOS.
        /// Defaults to FullScreen, which is the equivalent of using UIKit.UIModalPresentationStyle.FullScreen.
        /// </summary>
        public MediaPickerModalPresentationStyle ModalPresentationStyle { get; set; } = MediaPickerModalPresentationStyle.FullScreen;
    }

    public class StorePickerMediaOptions : StoreMediaOptions
    {
        /// <summary>
        /// Enable multi picker
        /// </summary>
        public bool MultiPicker { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StoreCameraMediaOptions
        : StoreMediaOptions
    {
        /// <summary>
        /// Allow cropping on photos and trimming on videos
        /// If null will use default
        /// Photo: UWP cropping can only be disabled on full size
        /// Video: UWP trimming when disabled won't allow time limit to be set
        /// </summary>
        public bool? AllowCropping { get; set; } = null;

        /// <summary>
        /// Default camera
        /// Should work on iOS and Windows, but not guaranteed on Android as not every camera implements it
        /// </summary>
        public CameraDevice DefaultCamera
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the the max width or height of the image.
        /// The image will aspect resize to the MaxWidthHeight as the max size of the image height or width. 
        /// This value is only used if PhotoSize is PhotoSize.MaxWidthHeight 
        /// </summary>
        /// <value>The max width or height of the image.</value>
        public int? MaxWidthHeight { get; set; }

        /// <summary>
        /// Get or set for an OverlayViewProvider
        /// </summary>
        public Func<Object> OverlayViewProvider
        {
            get;
            set;
        }

        /// <summary>
        // Get or set if the image should be stored public
        /// </summary>
        public bool SaveToAlbum
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;


        int customPhotoSize = 100;
        /// <summary>
        /// The custom photo size to use, 100 full size (same as Full),
        /// and 1 being smallest size at 1% of original
        /// Default is 100
        /// </summary>
        public int CustomPhotoSize
        {
            get { return customPhotoSize; }
            set
            {
                if (value > 100)
                    customPhotoSize = 100;
                else if (value < 1)
                    customPhotoSize = 1;
                else
                    customPhotoSize = value;
            }
        }


        int quality = 100;
        /// <summary>
        /// The compression quality to use, 0 is the maximum compression (worse quality),
        /// and 100 minimum compression (best quality)
        /// Default is 100
        /// </summary>
        public int CompressionQuality
        {
            get { return quality; }
            set
            {
                if (value > 100)
                    quality = 100;
                else if (value < 0)
                    quality = 0;
                else
                    quality = value;
            }
        }

        /// <summary>
        /// Store provided location
        /// </summary>
        public Location Location { get; set; }

        bool rotateImage = true;
        /// <summary>
        /// Should the library rotate image according to received exif orientation.
        /// Set to true by default.
        /// </summary>
        public bool RotateImage
        {
            get { return rotateImage; }
            set { rotateImage = value; }
        }

        bool saveMetaData = true;
        /// <summary>
        /// Saves metadate/exif data from the original file.
        /// </summary>
        public bool SaveMetaData
        {
            get { return saveMetaData; }
            set { saveMetaData = value; }
        }

        /// <summary>
        /// Specifies the media picker's modal presentation style.
        /// Only applies to iOS.
        /// Defaults to FullScreen, which is the equivalent of using UIKit.UIModalPresentationStyle.FullScreen.
        /// </summary>
        public MediaPickerModalPresentationStyle ModalPresentationStyle { get; set; } = MediaPickerModalPresentationStyle.FullScreen;
    }

    /// <summary>
    /// Photo size enum.
    /// </summary>
    public enum PhotoSize
    {
        /// <summary>
        /// 25% of original
        /// </summary>
        Small,
        /// <summary>
        /// 50% of the original
        /// </summary>
        Medium,
        /// <summary>
        /// 75% of the original
        /// </summary>
        Large,
        /// <summary>
        /// Untouched
        /// </summary>
        Full,
        /// <summary>
        /// Custom size between 1-100
        /// Must set the CustomPhotoSize value
        /// Only applies to iOS and Android
        /// Windows will auto configure back to small, medium, large, and full
        /// </summary>
        Custom,
        /// <summary>
        /// Use the Max Width or Height photo size.
        /// The property ManualSize must be set to a value. The MaxWidthHeight will be the max width or height of the image
        /// Currently this works on iOS and Android only.
        /// On Windows the PhotoSize will fall back to Full
        /// </summary>
        MaxWidthHeight
    }

    public enum MultiPickerBarStyle
    {
        Default = 0,
        Black = 1,
        BlackOpaque = 1,
        BlackTranslucent = 2
    }

    /// <summary>
    /// UI options for iOS multi image picker
    /// </summary>
    public class MultiPickerOptions
    {
        // TODO: This only affects iOS since Android uses native

        public int MaximumImagesCount { get; set; } = 10;

        public MultiPickerBarStyle BarStyle { get; set; } = MultiPickerBarStyle.Default;

        public string PathToOverlay { get; set; }
        public string AlbumSelectTitle { get; set; }
        public string PhotoSelectTitle { get; set; }
        public string BackButtonTitle { get; set; }
        public string DoneButtonTitle { get; set; }
        public string LoadingTitle { get; set; }
    }

    /// <summary>
    /// Video quality
    /// </summary>
    public enum VideoQuality
    {
        /// <summary>
        /// Low
        /// </summary>
        Low = 0,
        /// <summary>
        /// Medium
        /// </summary>
        Medium = 1,
        /// <summary>
        /// High
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// Store Video options
    /// </summary>
    public class StoreVideoOptions
      : StoreCameraMediaOptions
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public StoreVideoOptions()
        {
            Quality = VideoQuality.High;
            DesiredLength = TimeSpan.FromMinutes(10);
        }

        /// <summary>
        /// Desired Length
        /// </summary>
        public TimeSpan DesiredLength
        {
            get;
            set;
        }

        /// <summary>
        /// Desired Quality
        /// </summary>
        public VideoQuality Quality
        {
            get;
            set;
        }

        /// <summary>
        /// Desired Video Size
        /// Only available on Android - Set the desired file size in bytes.
        /// Eg. 1000000 = 1MB
        /// </summary>
        public long DesiredSize
        {
            get;
            set;
        }
    }
}
