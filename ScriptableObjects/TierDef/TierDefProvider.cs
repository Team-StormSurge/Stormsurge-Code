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
    }
}
