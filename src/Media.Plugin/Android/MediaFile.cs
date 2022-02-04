using System;
using System.Threading.Tasks;
using Android.Content;
using Plugin.Media.Abstractions;

namespace Plugin.Media
{
    /// <summary>
    /// 
    /// </summary>
    [Android.Runtime.Preserve(AllMembers = true)]
    public static class MediaFileExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task<MediaFile> GetMediaFileExtraAsync(this Intent self, Context context)
        {
            if (self == null)
                throw new ArgumentNullException("self");
            if (context == null)
                throw new ArgumentNullException("context");

            var action = self.GetStringExtra("action");
            if (action == null)
                throw new ArgumentException("Intent was not results from MediaPicker", "self");

            var uri = (Android.Net.Uri)self.GetParcelableExtra("MediaFile");
            var isPhoto = self.GetBooleanExtra("isPhoto", false);
            var path = (Android.Net.Uri)self.GetParcelableExtra("path");
            var saveToAlbum = false;
            try
            {
                saveToAlbum = (bool)self.GetParcelableExtra("album_save");
            }
            catch { }

            return MediaPickerActivity.GetMediaFileAsync(context, 0, action, isPhoto, ref path, uri, saveToAlbum)
                .ContinueWith(t => t.Result.ToTask()).Unwrap();
        }
    }

}