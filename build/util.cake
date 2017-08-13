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
		return DateTime.Now.ToString("yyMMddHM");
	}
}
