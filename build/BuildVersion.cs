namespace BuildScript
{
    public class BuildVersion(int major, int minor, int patch, string quality)
    {
        public int Major { get; set; } = major;

        public int Minor { get; set; } = minor;

        public int Patch { get; set; } = patch;

        public string Quality { get; set; } = quality;

        public string Suffix { get; set; }

        public string VersionWithoutQuality()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public string Version()
        {
            return VersionWithoutQuality() + (Quality == null ? string.Empty : $"-{Quality}");
        }

        public string VersionWithSuffix()
        {
            return Version() + (Suffix == null ? string.Empty : $"-{Suffix}");
        }
    }
}
