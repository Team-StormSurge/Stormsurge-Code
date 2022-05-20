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
        private static ItemTierDef HunterTierDef
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
            
            ColorCatalog.indexToColor32 = ColorCatalog.indexToColor32.AddItem(hunterLight).AddItem(hunterDark).ToArray();
            ColorCatalog.indexToHexString = ColorCatalog.indexToHexString.AddItem(Util.RGBToHex(hunterLight)).AddItem(Util.RGBToHex(hunterDark)).ToArray();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColor))]
        public static bool ReplaceGetColor(ColorCatalog.ColorIndex colorIndex, ref Color32 __result)
        {
            if (colorIndex < ColorCatalog.ColorIndex.None || (int) colorIndex > ColorCatalog.indexToColor32.Length)
            {
                colorIndex = ColorCatalog.ColorIndex.Error;
            }

            __result = ColorCatalog.indexToColor32[(int) colorIndex];
            return false;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColorHexString))]
        public static bool ReplaceGetHexString(ColorCatalog.ColorIndex colorIndex, ref string __result)
        {
            if (colorIndex < ColorCatalog.ColorIndex.None || (int) colorIndex > ColorCatalog.indexToHexString.Length)
            {
                colorIndex = ColorCatalog.ColorIndex.Error;
            }

            __result = ColorCatalog.indexToHexString[(int) colorIndex];
            return false;
        }
    }
}
