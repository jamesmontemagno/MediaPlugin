using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

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
#if NETSTANDARD
            return null;
#elif UWP || WINDOWS
            return Flags.Contains(FeatureFlags.UwpUseNewMediaImplementation)
                ? (IMedia)new NewMediaImplementation()
                : new MediaImplementation();
#else
            return new MediaImplementation();
#endif
        }

        public static bool FlagsSet;
        static IReadOnlyList<string> flags;
#if NETSTANDARD1_0
        public static IReadOnlyList<string> Flags => flags ?? (flags = new List<string>());
#else
        public static IReadOnlyList<string> Flags => flags ?? (flags = new List<string>().AsReadOnly());
#endif

        public static void SetFlags(params string[] flags)
        {
            if (FlagsSet)
            {
                // Don't try to set the flags again if they've already been set
                // (e.g., during a configuration change where OnCreate runs again)
                return;
            }

#if NETSTANDARD1_0
            CrossMedia.flags = flags.ToList();
#else
            CrossMedia.flags = flags.ToList().AsReadOnly();
#endif
            FlagsSet = true;
        }

        internal static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

    }
}
