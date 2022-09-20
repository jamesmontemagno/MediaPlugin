using System;

namespace Plugin.Media.Abstractions
{
    /// <summary>
    /// 
    /// </summary>
#if !NETSTANDARD
    [Serializable]
#endif
    public class MediaFileNotFoundException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public MediaFileNotFoundException(string path)
          : base("Unable to locate media file at " + path)
        {
            Path = path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="innerException"></param>
        public MediaFileNotFoundException(string path, Exception innerException)
          : base("Unable to locate media file at " + path, innerException)
        {
            Path = path;
        }
        /// <summary>
        /// Path
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        public MediaFileNotFoundException()
        {
        }
#if !NETSTANDARD1_0
        protected MediaFileNotFoundException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
        }
#endif
    }
}
