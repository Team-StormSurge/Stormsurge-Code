using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using StormSurge.ScriptableObjects.TierDef;
using SearchableAttribute = HG.Reflection.SearchableAttribute;
[assembly: SearchableAttribute.OptIn]

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace StormSurge
{
	//[BepInDependency(R2API.R2API.PluginGUID)]
	[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
	public class StormSurgePlugin : BaseUnityPlugin
	{
		public const string MOD_GUID = "teamstormsurge.stormsurge";
		public const string MOD_NAME = "StormSurge";
		public const string MOD_VERSION = "0.5.25";

		public static StormSurgePlugin? instance;
		public static bool INSTALLED_BETTERUI;
		public static Harmony? harmony;

		public void Awake()
		{
			//check if BetterUI is loaded
			if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI")) INSTALLED_BETTERUI = true;
			//set singleton instance of this plugin
			instance = this;

			//initialise mod config
			StormSurge.Config.Init(Config);

			//Create Harmony patcher to hook ingame features
			harmony = new Harmony(MOD_GUID);

			//initialise asset loading
			Assets.Init(harmony);

			//generates the patch processor for our TierDefProvider, then patches it in.
			new PatchClassProcessor(harmony, typeof(TierDefProvider)).Patch();

		}
	}
}