using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace StormSurge.ScriptableObjects.TierDef
{
    [HarmonyPatch]
    public class TierDefProvider
    {
        private static GameObject _displayPrefab;
        private static GameObject _highlightPrefab;
        public static GameObject DisplayPrefab
        {
            get
            {
                if (_displayPrefab == null)
                {
                    _displayPrefab = LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Common/PickupDroplet.prefab");
                }
                return _displayPrefab;
            }
        }
        public static GameObject HighlightPrefab
        {
            get
            {
                if (_highlightPrefab == null)
                {
                    _highlightPrefab = LegacyResourcesAPI.Load<GameObject>("RoR2/Base/UI/HighlightLunarItem.prefab");
                }
                return _highlightPrefab;
            }
        }
        public static void Init(StormsurgeContentPack contentPack)
        {
            var tierDefs = contentPack.itemTierDefs;
            tierDefs[0].dropletDisplayPrefab = DisplayPrefab;
            tierDefs[0].highlightPrefab = HighlightPrefab;
            HunterTier = tierDefs[0];
        }

        public static ItemTierDef HunterTier;

        [HarmonyPostfix, HarmonyPatch(typeof(ColorCatalog), MethodType.StaticConstructor)]
        public static void AddNewColor()
        {
            var len = ColorCatalog.indexToColor32.Length;
            HunterTier.colorIndex = (ColorCatalog.ColorIndex) len;
            HunterTier.darkColorIndex = (ColorCatalog.ColorIndex) len + 1;

            var hunterLight = new Color32(223, 205, 255, 255);
            var hunterDark = new Color32(88, 50, 86, 255);
            
            ColorCatalog.indexToColor32 = ColorCatalog.indexToColor32.AddItem(hunterLight).AddItem(hunterDark).ToArray();
            ColorCatalog.indexToHexString = ColorCatalog.indexToHexString.AddItem(Util.RGBToHex(hunterLight)).AddItem(Util.RGBToHex(hunterDark)).ToArray();
        }
    }
}
