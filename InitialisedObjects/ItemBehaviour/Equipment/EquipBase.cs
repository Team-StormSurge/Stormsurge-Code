using RoR2;
using StormSurge.ItemBehaviour;
using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.InitialisedObjects.ItemBehaviour.Equipment
{
    [HarmonyLib.HarmonyPatch]
    public abstract class EquipBase : InitialisedBase
    {
        protected abstract string equipDefName { get; }
        protected abstract ItemLanguage lang { get; }
        protected ItemLanguage? tokens;

        private EquipmentDef _equipDef;
        public EquipmentDef equipDef
        {
            get
            {
                if (_equipDef == null) _equipDef = Assets.ContentPack.equipmentDefs.Find(equipDefName);
                return _equipDef;
            }
        }
        public override void InitFunction()
        {
            //if (!AddConfig()) return;
            AddConfig();
            AddEquipBehavior();
            tokens = lang;
        }
        public abstract void AddEquipBehavior();
    }
}
