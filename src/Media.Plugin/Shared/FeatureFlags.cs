namespace Plugin.Media
{
    static class FeatureFlags
    {
#if UWP
        internal const string UwpUseNewMediaImplementation = "UwpUseNewMediaImplementation";
#endif
    }
}
