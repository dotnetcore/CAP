public class Util
{
	public Util(ICakeContext context, BuildParameters build)
	{
		Context = context;
		Build = build;
	}

	public ICakeContext Context { get; set; }
	public BuildParameters Build { get; set; }

	public void PrintInfo()
	{
		Context.Information($@"
Version:       {Build.FullVersion()}
Configuration: {Build.Configuration}
");
	}

	public static string CreateStamp()
	{
		var seconds = (long)(DateTime.UtcNow - new DateTime(2017, 1, 1)).TotalSeconds;
		return seconds.ToString();
	}
}
