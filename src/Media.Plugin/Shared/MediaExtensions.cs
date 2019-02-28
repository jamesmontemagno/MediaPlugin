using System;
using System.Globalization;
using System.IO;

namespace Plugin.Media.Abstractions
{
    /// <summary>
    /// 
    /// </summary>
    public static class MediaExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        public static void VerifyOptions(this StoreMediaOptions self)
        {
            if (self == null)
                throw new ArgumentNullException("options");
            //if (!Enum.IsDefined (typeof(MediaFileStoreLocation), options.Location))
            //    throw new ArgumentException ("options.Location is not a member of MediaFileStoreLocation");
            //if (options.Location == MediaFileStoreLocation.Local)
            //{
            //if (String.IsNullOrWhiteSpace (options.Directory))
            //	throw new ArgumentNullException ("options", "For local storage, options.Directory must be set");
            if (Path.IsPathRooted(self.Directory))
                throw new ArgumentException("options.Directory must be a relative path", "options");
            //}
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static string GetFilePath(this StoreMediaOptions self, string rootPath)
        {
            var isPhoto = !(self is StoreVideoOptions);

            var name = (self != null) ? self.Name : null;
            if (String.IsNullOrWhiteSpace(name))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                if (isPhoto)
                    name = "IMG_" + timestamp + ".jpg";
                else
                    name = "VID_" + timestamp + ".mp4";
            }

            var ext = Path.GetExtension(name);
            if (ext == String.Empty)
                ext = ((isPhoto) ? ".jpg" : ".mp4");

            name = Path.GetFileNameWithoutExtension(name);

            var folder = Path.Combine(rootPath ?? String.Empty,
              (self != null && self.Directory != null) ? self.Directory : String.Empty);

            return Path.Combine(folder, name + ext);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="rootPath"></param>
        /// <param name="checkExists"></param>
        /// <returns></returns>
        public static string GetUniqueFilepath(this StoreMediaOptions self, string rootPath, Func<string, bool> checkExists)
        {
            var path = self.GetFilePath(rootPath);
            var folder = Path.GetDirectoryName(path);
            var ext = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var nname = name + ext;
            var i = 1;
            while (checkExists(Path.Combine(folder, nname)))
                nname = name + "_" + (i++) + ext;

            return Path.Combine(folder, nname);
        }
    }
}
