using System.Runtime.InteropServices;

namespace SkyWalking.Utils
{
    internal static class PlatformInformation
    {
        private const string OSX = "Mac OS X";
        private const string LINUX = "Linux";
        private const string WINDOWS = "Windows";

        public static string GetOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WINDOWS;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return LINUX;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSX;
            }

            return "Unknown";
        }
    }
}
