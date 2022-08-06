using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormSurge.Interactables
{
    public interface ISceneVariant
    {
        string cardCategory { get; }
        public Dictionary<string[], DirectorCard> SceneVariants
        {
            get;
        }
        bool GetSceneVariant(string sceneName, out DirectorCard returnedCard)
        {
            returnedCard = SceneVariants.Where(x =>
            {
                return x.Key.Contains(sceneName);
            }).FirstOrDefault().Value;
            return (returnedCard != null);
        }
        public virtual void AddVariantToDirector(SceneDirector director, DirectorCardCategorySelection selection)
        {
            DirectorCard? dCard = null;
            var gotValue = GetSceneVariant(SceneInfo.instance.sceneDef.nameToken, out dCard);
            if (gotValue)
            {
                int ind = Array.FindIndex(selection.categories, (item) => item.name.Equals(cardCategory, StringComparison.OrdinalIgnoreCase));
                //UnityEngine.Debug.LogWarning($"STORMSURGE : added shrine {dCard.spawnCard.name} to stage {SceneInfo.instance.sceneDef.nameToken}");
                selection.AddCard(ind, dCard);
            }

        }
    }
}
