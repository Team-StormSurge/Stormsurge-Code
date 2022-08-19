using RoR2;
using StormSurge.ScriptableObjects.TierDef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormSurge.Utils
{
	/// <summary>
	/// The custom Drop Table that we use for Huntier-tier item drops, with Storm events.
	/// </summary>
	[UnityEngine.CreateAssetMenu(menuName = "Stormsurge/Hunter Drop Table")] //adds this to Thunderkit's asset menu
	public class HunterDropTable : PickupDropTable
	{
		private WeightedSelection<PickupIndex> selector = new(); // the selector for our drop table
		private void Add(PickupIndex[] sourceDropList, float chance) //adds new pickups to our drop table
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

		public override void Regenerate(Run run) //regenerates our drop table's chances using the current run as a seed.
		{
			base.Regenerate(run);
			selector.Clear(); ;
			//Add(ItemBase.Items.Where(x => x.ItemDef.tier == BubbetsItemsPlugin.VoidLunarTier.tier).Select(x => x.PickupIndex), 1);
			GenerateHunterItems();
		}

		//gets all items that have the Hunter ItemTierDef
		//THIS IS A PLACEHOLDER, AS THIS DOES NOT ALLOW FOR ANY MORE SPECIFIC DROPS!! 
		void GenerateHunterItems() 
        {
			var catalog = ItemCatalog.allItemDefs
			.Where(x => x.tier == ((ItemTierDef) TierDefProvider.HunterTierDef).tier)
			.Select(x => PickupCatalog.FindPickupIndex(x.itemIndex)).ToArray();
			Add(catalog, 1);
		}
		public override int GetPickupCount() //returns the length of this drop table
		{
			return selector.Count;
		}

		public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng) //generates a new drop
		{
			return GenerateDropFromWeightedSelection(rng, selector);
		}

		public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng) //generates multiple new drops
		{
			return GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, selector);
		}
	}
}
