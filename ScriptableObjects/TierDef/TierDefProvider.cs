using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StormSurge.ScriptableObjects.TierDef
{
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
        }
    }
}
