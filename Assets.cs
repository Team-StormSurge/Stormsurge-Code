using System.Collections;
using System.Reflection;
using HarmonyLib;
using RoR2.ContentManagement;
using StormSurge.ScriptableObjects.TierDef;
using UnityEngine;
using static StormSurge.StormSurgePlugin;

namespace StormSurge
{
	public static class Assets
	{
		public static AssetBundle AssetBundle;
		public static ContentPack ContentPack;
		public static string assetBundleName = "stormsurgeassets";
		public static string contentPackName = "stormsurgecontent";
		public static void Init(Harmony harmony)
		{
			var assembly = Assembly.GetCallingAssembly();
			var location = assembly.Location;
			AssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(location), assetBundleName));
			
			// Load serializable contentpack and supply to content manager
			var scp = AssetBundle.LoadAsset<StormsurgeContentPack>(contentPackName);
			TierDefProvider.Init(scp);
			ContentPack = scp.CreateContentPack();
			ContentManager.collectContentPackProviders += dele => dele(new ContentPackProvider());
			InitialisedObjects.InitialisedBase.InitialiseAll(harmony);
		}
	}

	public class ContentPackProvider : IContentPackProvider
	{
		public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
		{
			args.ReportProgress(1f);
			yield break;
		}

		public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
		{
			ContentPack.Copy(Assets.ContentPack, args.output);
			args.ReportProgress(1f);
			yield break;
		}

		public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
		{
			args.ReportProgress(1f);
			yield break;
		}

		public string identifier => StormSurgePlugin.instance.Info.Metadata.GUID + "_fromUnity";
	}
}