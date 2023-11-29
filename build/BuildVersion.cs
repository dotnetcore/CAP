namespace BuildScript
{
    public class BuildVersion
    {
        public int Major { get; init; }

        public int Minor { get; init; }

        public int Patch { get; init; } 

        public string Quality { get; init; }

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
