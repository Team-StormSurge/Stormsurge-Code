using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StormSurge.Interactables
{
    public interface ISceneVariant
    {
        string[] cardCategories { get; }
        public abstract SceneVariantList SceneVariants
        { get; }
        bool GetSceneVariant(string sceneName, out DirectorCard returnedCard)
        {
            returnedCard = SceneVariants.content.Where(x =>
            {
                return x.KeyList.Contains(sceneName);
            }).FirstOrDefault().SpawnCard;
            return (returnedCard != null);
        }
        public virtual void AddVariantToDirector(SceneDirector director, DirectorCardCategorySelection selection)
        {
            DirectorCard? dCard = null;
            var gotValue = GetSceneVariant(SceneInfo.instance.sceneDef.nameToken, out dCard);
            if (gotValue)
            {
                foreach(string category in cardCategories)
                {
                    int ind = Array.FindIndex(selection.categories, (item) => item.name.Equals(category, StringComparison.OrdinalIgnoreCase));
                    if (ind >= 0)
                        selection.AddCard(ind, dCard);
                    return;
                    //UnityEngine.Debug.LogWarning($"STORMSURGE : added shrine {dCard.spawnCard.name} to stage {SceneInfo.instance.sceneDef.nameToken}");
                }
            }

        }
    }

    [CreateAssetMenu(menuName = "Stormsurge/Scene Variant List")]
    public class SceneVariantList : ScriptableObject
    {
        [Serializable]
        public struct VariantSet
        {
            public string[] KeyList;
            public DirectorCard SpawnCard;
        }
        [SerializeField]
        public List<VariantSet> content;
    }
}
