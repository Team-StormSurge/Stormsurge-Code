using BepInEx.Configuration;

namespace StormSurge
{
    public static class Config
	{
		public static ConfigFile? configFile;
		/// <summary>
		/// Initialises our Config File using this file location.
		/// </summary>
		/// <param name="file">The location string for our config file.</param>
		public static void Init(ConfigFile file)
		{
			configFile = file;
		}
        
    }
}