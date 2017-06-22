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
		public MediaFile(string path, Func<Stream> streamGetter, string albumPath = null)
        {
            this.streamGetter = streamGetter;
            this.path = path;
            this.albumPath = albumPath;
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
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

		bool isDisposed;
		Func<Stream> streamGetter;
        string path;
        string albumPath;

        void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;
			if(disposing)
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
