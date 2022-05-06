using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StormSurge
{
    [CreateAssetMenu(menuName = "Stormsurge/ContentPackProvider")]
    public class StormsurgeContentPack : SerializableContentPack
    {
        public ExpansionDef[] expansionDefs = { };
        public ItemTierDef[] itemTierDefs = { };
        public ItemRelationshipType[] itemRelationshipTypes = { };
        public ItemRelationshipProvider[] itemRelationshipProviders = { };

        public override ContentPack CreateContentPack()
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
