using BepInEx.Configuration;

namespace StormSurge
{
	public static class Config
	{
		private static ConfigFile configFile;
		public static void Init(ConfigFile file)
		{
			configFile = file;
		}
	}
}