using System;
using System.IO;


namespace Plugin.Media.Abstractions
{
    /// <summary>
    /// Media file representations
    /// </summary>
    public sealed class MediaFile : IDisposable
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path"></param>
        /// <param name="streamGetter"></param>
        /// <param name="albumPath"></param>
        public MediaFile(string path, Func<Stream> streamGetter, Func<Stream> streamGetterForExternalStorage = null, string albumPath = null, string originalFilename = null)
        {
            this.streamGetter = streamGetter;
            this.streamGetterForExternalStorage = streamGetterForExternalStorage;
            this.path = path;
            this.albumPath = albumPath;
            this.originalFilename = originalFilename;
        }

        /// <summary>
        /// The original filename
        /// </summary>
        public string OriginalFilename
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException(null);

                return originalFilename;
            }
        }

        /// <summary>
        /// Path to file
        /// </summary>
        public string Path
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException(null);

                return path;
            }
        }

        /// <summary>
        /// Path to file
        /// </summary>
        public string AlbumPath
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException(null);

                return albumPath;
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException(null);

                albumPath = value;
            }
        }

        /// <summary>
        /// Get stream if available
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            if (isDisposed)
                throw new ObjectDisposedException(null);

            return streamGetter();
        }

        /// <summary>
        /// Get stream with image orientation rotated if available. If not, then just GetStream()
        /// </summary>
        /// <returns></returns>
        public Stream GetStreamWithImageRotatedForExternalStorage()
        {
            if (isDisposed)
                throw new ObjectDisposedException(null);

            if (streamGetterForExternalStorage != null)
                return streamGetterForExternalStorage();
            else
                return GetStream();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool isDisposed;
        Func<Stream> streamGetter;
        Func<Stream> streamGetterForExternalStorage;
        string originalFilename;
        string path;
        string albumPath;

        void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;
            if (disposing)
                streamGetter = null;
        }
        /// <summary>
        /// 
        /// </summary>
        ~MediaFile()
        {
            Dispose(false);
        }
    }
}
