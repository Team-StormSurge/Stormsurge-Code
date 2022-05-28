using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormSurge.InitialisedObjects.CardBehaviour.Interactables
{
    public class StormShrineBehaviour : CardBase
    {
        static InteractableSpawnCard? _shrineCard;
        public static InteractableSpawnCard ShrineCard
        {
            get
            {
                _shrineCard ??= Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm");
                return _shrineCard;
            }
        }
        protected override string configName => "Shrine of Storms";

        protected override void AddCard()
        {
            SceneDirector.onGenerateInteractableCardSelection += SceneDirector_onGenerateInteractableCardSelection;
        }

        private void SceneDirector_onGenerateInteractableCardSelection(SceneDirector dir, DirectorCardCategorySelection selection)
        {
            DirectorCard dCard = new()
            {
                spawnCard = ShrineCard,
                selectionWeight = 1,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Far
            };
            int ind = Array.FindIndex(selection.categories,(item) => item.name.Equals("Shrines", StringComparison.OrdinalIgnoreCase));
            selection.AddCard(ind, dCard);
        }
    }
}
