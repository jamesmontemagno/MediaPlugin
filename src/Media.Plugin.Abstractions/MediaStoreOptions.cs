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
    /// 
    /// </summary>
    public class PickMediaOptions
    {

        /// <summary>
        /// Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;

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
        Full
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
    }
}
