using Plugin.Media.Abstractions;
using System;

namespace Plugin.Media
{
    /// <summary>
    /// Cross platform Media implemenations
    /// </summary>
    public class CrossMedia
    {
        static Lazy<IMedia> implementation = new Lazy<IMedia>(() => CreateMedia(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
		/// Gets if the plugin is supported on the current platform.
		/// </summary>
		public static bool IsSupported => implementation.Value == null ? false : true;

        /// <summary>
        /// Current plugin implementation to use
        /// </summary>
        public static IMedia Current
        {
            get
            {
                var ret = implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IMedia CreateMedia()
        {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
            return new MediaImplementation();
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        
    }
}
