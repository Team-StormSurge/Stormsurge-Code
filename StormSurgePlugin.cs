using BepInEx;
using HarmonyLib;
using StormSurge.ScriptableObjects.TierDef;

namespace StormSurge
{
	[BepInDependency(R2API.R2API.PluginGUID)]
	[BepInPlugin("teamstormsurge.stormsurge", "Storm Surge", "1.0.0")]
	public class StormSurgePlugin : BaseUnityPlugin
	{
		public static StormSurgePlugin instance;

		public void Awake()
		{
			instance = this;
			Assets.Init();
			StormSurge.Config.Init(Config);

			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(TierDefProvider)).Patch();
		}
	}
}