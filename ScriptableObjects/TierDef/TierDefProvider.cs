using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using StormSurge;
using UnityEngine;

namespace StormSurge.ScriptableObjects.TierDef
{
	[HarmonyPatch]
	public class TierDefProvider
	{
		private static ItemTierDef _hunter;

		public static ItemTierDef HunterTierDef
		{
			get
			{
				if (!_hunter) _hunter = Assets.AssetBundle.LoadAsset<ItemTierDef>("HunterTier");
				return _hunter;
			}
		}

		public static void Init(StormsurgeContentPack contentPack)
		{
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ColorCatalog), MethodType.StaticConstructor)]
		public static void AddNewColor()
		{
			var len = ColorCatalog.indexToColor32.Length;
			HunterTierDef.colorIndex = (ColorCatalog.ColorIndex) len;
			HunterTierDef.darkColorIndex = (ColorCatalog.ColorIndex) len + 1;

			var hunterLight = new Color32(223, 205, 255, 255);
			var hunterDark = new Color32(88, 50, 86, 255);

			ColorCatalog.indexToColor32 =
				ColorCatalog.indexToColor32.AddItem(hunterLight).AddItem(hunterDark).ToArray();
			ColorCatalog.indexToHexString = ColorCatalog.indexToHexString.AddItem(Util.RGBToHex(hunterLight))
				.AddItem(Util.RGBToHex(hunterDark)).ToArray();
		}

		[HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColor))]
		public static bool PatchGetColor(ColorCatalog.ColorIndex colorIndex, ref Color32 __result)
		{
			var ind = (int) colorIndex;
			if (ind >= ColorCatalog.indexToColor32.Length) return true;
			__result = ColorCatalog.indexToColor32[ind];
			return false;
		}

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