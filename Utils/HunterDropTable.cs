using RoR2;
using StormSurge.ScriptableObjects.TierDef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormSurge.Utils
{
	[UnityEngine.CreateAssetMenu(menuName = "Stormsurge/Hunter Drop Table")]
	public class HunterDropTable : PickupDropTable
	{
		private WeightedSelection<PickupIndex> selector = new();
		private void Add(PickupIndex[] sourceDropList, float chance)
		{
			var pickupIndices = sourceDropList;
			if (chance <= 0f || !pickupIndices.Any())
			{
				return;
			}
			foreach (PickupIndex pickupIndex in pickupIndices)
			{
				selector.AddChoice(pickupIndex, chance);
			}
		}

		public override void Regenerate(Run run)
		{
			base.Regenerate(run);
			selector.Clear(); ;
			//Add(ItemBase.Items.Where(x => x.ItemDef.tier == BubbetsItemsPlugin.VoidLunarTier.tier).Select(x => x.PickupIndex), 1);
			GenerateHunterItems();
		}
		void GenerateHunterItems()
        {
			var catalog = ItemCatalog.allItemDefs
			.Where(x => x.tier == TierDefProvider.HunterTierDef.tier)
			.Select(x => PickupCatalog.FindPickupIndex(x.itemIndex)).ToArray();
			Add(catalog, 1);
		}
		public override int GetPickupCount()
		{
			return selector.Count;
		}

		public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
		{
			return GenerateDropFromWeightedSelection(rng, selector);
		}

		public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
		{
			return GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, selector);
		}
	}
}
