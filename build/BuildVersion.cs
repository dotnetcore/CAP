namespace BuildScript
{
    public class BuildVersion
    {
        public BuildVersion(int major, int minor, int patch, string quality)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Quality = quality;
        }

        public int Major { get; set; }

        public int Minor { get; set; }

        public int Patch { get; set; }

        public string Quality { get; set; }

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
