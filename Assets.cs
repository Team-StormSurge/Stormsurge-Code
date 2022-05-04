using System.Collections;
using System.Reflection;
using RoR2.ContentManagement;
using UnityEngine;

namespace StormSurge
{
	public static class Assets
	{
		public static AssetBundle AssetBundle;
		public static ContentPack ContentPack;
		public static string assetBundleName = "StormsurgeAssets";
		public static string contentPackName = "StormsurgeContent";
		public static void Init()
		{
			var assembly = Assembly.GetCallingAssembly();
			var location = assembly.Location;
			AssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(location, assetBundleName));
			
			// Load serializable contentpack and supply to content manager
			var scp = AssetBundle.LoadAsset<SerializableContentPack>(contentPackName);
			ContentPack = scp.CreateContentPack();
			ContentManager.collectContentPackProviders += dele => dele(new ContentPackProvider());
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