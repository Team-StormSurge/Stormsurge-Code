using RoR2;
using StormSurge.ItemBehaviour;
using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Equipment
{
    /// <summary>
    /// The class that we use for any behaviours that associate with our modded equipment.
    /// </summary>
    [HarmonyLib.HarmonyPatch]
    public abstract class EquipBase : InitialisedBase
    {
        /// <summary>
        /// The name of the EquipDef with which we associate this equipment behaviour.
        /// </summary>
        protected abstract string equipDefName { get; }

        private EquipmentDef _equipDef;
        /// <summary>
        /// the EquipDef with which we associate this equipment behaviour.
        /// </summary>
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
        }

        /// <summary>
        /// Used to subscribe to any events for this equipment behaviour, etc.
        /// </summary>
        public abstract void AddEquipBehavior();
    }
}
