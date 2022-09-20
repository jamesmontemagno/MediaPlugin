namespace Plugin.Media
{
    static class FeatureFlags
    {
#if UWP || WINDOWS
        internal const string UwpUseNewMediaImplementation = "UwpUseNewMediaImplementation";
#endif
    }
}
