using System.IO;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{
    [Reference("System.Xml.XmlDocument, System.Xml, Version=4.0.0.0, Culture=neutral, publicKeyToken=b77a5c561934e089")]
    public partial class BuildScript
    {
        public BuildVersion FetchBuildVersion(ITaskContext context)
        {
            var content = File.ReadAllText(RootDirectory.CombineWith("build/version.props"));

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);

            var versionMajor = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionMajor").InnerText;
            var versionMinor = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionMinor").InnerText;
            var versionPatch = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionPatch").InnerText;
            var versionQuality = doc.DocumentElement.SelectSingleNode("/Project/PropertyGroup/VersionQuality").InnerText;
            versionQuality = string.IsNullOrWhiteSpace(versionQuality) ? null : versionQuality;

            var suffix = versionQuality;

            bool isCi = false;
            bool isTagged = false;
            if (!context.BuildServers().IsLocalBuild)
            {
                isCi = true;
                bool isTagAppveyor = context.BuildServers().AppVeyor().IsTag;
                if (context.BuildServers().RunningOn == BuildServerType.AppVeyor && isTagAppveyor ||
                    context.BuildServers().RunningOn == BuildServerType.TravisCI && string.IsNullOrWhiteSpace(context.BuildServers().Travis().TagName))
                {
                    isTagged = true;
                }
            }

            if (!isTagged)
            {
                suffix += (isCi ? "preview-" : "dv-") + CreateStamp();
            }

            suffix = string.IsNullOrWhiteSpace(suffix) ? null : suffix;

            var version = new BuildVersion(int.Parse(versionMajor), int.Parse(versionMinor), int.Parse(versionPatch), versionQuality);
            version.Suffix = suffix;

            return version;
        }
    }
}
