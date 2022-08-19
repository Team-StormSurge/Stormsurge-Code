using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StormSurge
{
    /// <summary>
    /// The custom Content Pack that we use, in order to implement custom Expansions, Item Tiers, Void Conversions, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Stormsurge/Content Pack Provider")]
    public class StormsurgeContentPack : SerializableContentPack
    {

        public ExpansionDef[] expansionDefs = { };
        public ItemTierDef[] itemTierDefs = { };
        public ItemRelationshipType[] itemRelationshipTypes = { };
        public ItemRelationshipProvider[] itemRelationshipProviders = { };

        public override ContentPack CreateContentPack() // adds any of our custom content into the base content pack
        {
            var content = base.CreateContentPack();
            content.expansionDefs.Add(expansionDefs);
            content.itemTierDefs.Add(itemTierDefs);
            content.itemRelationshipTypes.Add(itemRelationshipTypes);
            content.itemRelationshipProviders.Add(itemRelationshipProviders);
            return content;
        }
    }
}
