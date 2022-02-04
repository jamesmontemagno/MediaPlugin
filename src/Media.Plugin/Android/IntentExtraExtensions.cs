using Android.Content;
using Android.Hardware;

namespace Plugin.Media
{
    static class IntentExtraExtensions
    {
        const string extraFrontPre25 = "android.intent.extras.CAMERA_FACING";
        const string extraFrontPost25 = "android.intent.extras.LENS_FACING_FRONT";
        const string extraBackPost25 = "android.intent.extras.LENS_FACING_BACK";
        const string extraUserFront = "android.intent.extra.USE_FRONT_CAMERA";

        public static void UseFrontCamera(this Intent intent)
        {
            // Android before API 25 (7.1)
            intent.PutExtra(extraFrontPre25, (int)CameraFacing.Front);

            // Android API 25 and up
            intent.PutExtra(extraFrontPost25, 1);

            var isIntentNeeded = intent.GetBooleanExtra(extraUserFront, false);
            if (isIntentNeeded)
                intent.PutExtra(extraUserFront, true);
        }

        public static void UseBackCamera(this Intent intent)
        {
            // Android before API 25 (7.1)
            intent.PutExtra(extraFrontPre25, (int)CameraFacing.Back);

            // Android API 25 and up
            intent.PutExtra(extraBackPost25, 1);

            var isIntentNeeded = intent.GetBooleanExtra(extraUserFront, false);
            if (isIntentNeeded)
                intent.PutExtra(extraUserFront, false);
        }
    }
}