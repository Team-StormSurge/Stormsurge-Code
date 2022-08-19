using System.Collections;
using System.IO;
using System.Reflection;
using HarmonyLib;
using RoR2.ContentManagement;
using StormSurge.ScriptableObjects.TierDef;
using UnityEngine;
using static StormSurge.StormSurgePlugin;

namespace StormSurge
{
	/// <summary>
	/// Static class responsible for loading our mod asset bundles and content packs.
	/// If the mod gets big enough, maybe we'll find a way to make this slightly more generic.
	/// </summary>
	public static class Assets
	{
		static Material[] materials;
		public static AssetBundle AssetBundle; //reference to our loaded asset bundle
		public static ContentPack ContentPack; //reference to our loaded content pack
		public static uint soundbankID; //reference to our loaded soundbank
		public static string assetBundleName = "stormsurgeassets"; //the name of our asset bundle
		public static string contentPackName = "stormsurgecontent"; //the name of our content pack

		public static string soundBankName = "StormsurgeSoundbank.bnk"; //the name of our sound bank

		public static string path => Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);

		[RoR2.SystemInitializer]
		public static void LoadSoundbank()
        {
			//load soundbank from file
			var akResult = AkSoundEngine.AddBasePath(path);
			akResult = AkSoundEngine.LoadBank(soundBankName, out soundbankID);

		}
		/// <summary>
		/// Initialises our asset bundle and content pack at patch-time, then loads all modded behaviours.
		/// </summary>
		/// <param name="harmony">The patcher that we use to initialise content</param>
		public static void Init(Harmony harmony)
		{
			//load asset bundle from file

			AssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, assetBundleName)); 
			//collects and loads all of our modded language files
			RoR2.Language.collectLanguageRootFolders += list => list.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Language");

			// Load serializable contentpack and supply to content manager
			var scp = AssetBundle.LoadAsset<StormsurgeContentPack>(contentPackName);
			TierDefProvider.Init(scp); //initialise our Tier Def Provider
			ContentPack = scp.CreateContentPack(); //generates a content pack from our loaded values.
			ContentManager.collectContentPackProviders += dele => dele(new ContentPackProvider()); //prepares content pack for loading ingame.
			InitialisedBase.InitialiseAll(harmony); //Initialises all modded behaviours after content loading.
			ApplyShaders(); //replaces all of our stubbed shaders with their ingame variants.
		}
		/// <summary>
		/// Loads all materials in our asset bundle, then replaces any stubbed variants with their ingame counterparts.
		/// </summary>
		public static void ApplyShaders()
		{
			materials = AssetBundle.LoadAllAssets<Material>();
			var stubString = "StubbedShader";
			foreach (Material material in materials)
			{
				if (material.shader.name.Contains(stubString))
				{
					var unStubbedAddress = material.shader.name.Substring(stubString.Length).ToLower();
					material.shader = Resources.Load<Shader>("shaders" + unStubbedAddress);
					//log error if we have failed to convert any shaders- this is bad juju!
					if (material.shader.name.ToLowerInvariant().Contains("internalerror"))
						UnityEngine.Debug.LogError($"STORMSURGE : Cannot convert shader : shaders{unStubbedAddress} => {material.shader.name}");
				}
			}
		}
	}

	/// <summary>
	/// Our custom ContentPackProvider, used to load our content pack at patch-time.
	/// </summary>
	public class ContentPackProvider : IContentPackProvider //TODO change this to not be stupid??
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