using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using StormSurge;
using UnityEngine;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.ScriptableObjects.TierDef
{
	/// <summary>
	/// Implements our custom Item Tiers (currently Hunter only)
	/// </summary>
	[HarmonyPatch]
	public class TierDefProvider
	{
		public static InstRef<ItemTierDef> HunterTierDef = new(() => Assets.AssetBundle.LoadAsset<ItemTierDef>("HunterTier"));

		/// <summary>
		/// initialises our TierDef provider and runs all pertinent code at patch-time.
		/// </summary>
		/// <param name="contentPack"></param>
		public static void Init(StormsurgeContentPack contentPack)
		{
			AddNewColor();
		}

		/// <summary>
		/// Adds custom colours into RoR's custom colour catalogue.
		/// </summary>
		public static void AddNewColor()
		{
			var len = ColorCatalog.indexToColor32.Length;
			((ItemTierDef) HunterTierDef).colorIndex	= (ColorCatalog.ColorIndex) len;
			((ItemTierDef)HunterTierDef).darkColorIndex = (ColorCatalog.ColorIndex) len + 1;

			var hunterLight = new Color32(223, 205, 255, 255);
			var hunterDark = new Color32(88, 50, 86, 255);

			ColorCatalog.indexToColor32 =
				ColorCatalog.indexToColor32.AddItem(hunterLight).AddItem(hunterDark).ToArray();
			ColorCatalog.indexToHexString = ColorCatalog.indexToHexString.AddItem(Util.RGBToHex(hunterLight))
				.AddItem(Util.RGBToHex(hunterDark)).ToArray();
		}

		//Harmony pre-fix for ColorCatalog.GetColor to work with our custom colours.
		[HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColor))]
		public static bool PatchGetColor(ColorCatalog.ColorIndex colorIndex, ref Color32 __result)
		{
			var ind = (int) colorIndex;
			if (ind >= ColorCatalog.indexToColor32.Length) return true;
			__result = ColorCatalog.indexToColor32[ind];
			return false;
		}


		//Harmony pre-fix for ColorCatalog.GetColorHexString to work with our custom colours.
		[HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColorHexString))]
		public static bool GetColorHexString(ColorCatalog.ColorIndex colorIndex, ref string __result)
		{
			var ind = (int) colorIndex;
			if (ind >= ColorCatalog.indexToHexString.Length) return true;
			__result = ColorCatalog.indexToHexString[ind];
			return false;
		}
	}
}