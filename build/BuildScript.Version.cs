using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using FlubuCore.Context;
using FlubuCore.Scripting.Attributes;

namespace BuildScript
{
    [Include("./build/BuildVersion.cs")]
    [Reference("System.Xml.XmlDocument, System.Xml, Version=4.0.0.0, Culture=neutral, publicKeyToken=b77a5c561934e089")]
    public partial class BuildScript
    {
        public BuildVersion FetchBuildVersion(ITaskContext context)
        {
            var content = System.IO.File.ReadAllText("./build/version.props");

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
            if (!context.BuildSystems().IsLocalBuild)
            {
                isCi = true;
                //// todo use flubu build system when available.
                var appveyortag = context.GetEnvironmentVariable("APPVEYOR_REPO_TAG");
                bool isTagAppveyor = !string.IsNullOrEmpty(appveyortag) && appveyortag.Equals("true", StringComparison.OrdinalIgnoreCase);
             
                if ((context.BuildSystems().RunningOn == BuildSystemType.AppVeyor && isTagAppveyor) ||
                    (context.BuildSystems().RunningOn == BuildSystemType.TravisCI && string.IsNullOrWhiteSpace(context.BuildSystems().Travis().TagName)))
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
