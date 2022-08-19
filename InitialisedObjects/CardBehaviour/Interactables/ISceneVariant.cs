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
        /// <summary>
        /// The directorPool categories into which this variant will populate- i.e. "Interactables" or "voidStuff"
        /// </summary>
        string[] cardCategories { get; }
        /// <summary>
        /// the list of Scene Variants this card uses
        /// </summary>
        public abstract SceneVariantList SceneVariants
        { get; }
        /// <summary>
        /// tries to find a scene variant for a given scene
        /// </summary>
        /// <param name="sceneName">the name of the scene for which we would like to find a variant</param>
        /// <param name="returnedCard">the card value to which we will try and assign our scene variant</param>
        /// <returns>whether we found a Scene Variant card for this scene name</returns>
        bool GetSceneVariant(string sceneName, out DirectorCard returnedCard)
        {
            //linq code; look into our SceneVariants list, and find a value that contains the name of the scene for which we are searching
            returnedCard = SceneVariants.content.Where(x =>
            {
                return x.KeyList.Contains(sceneName);
            }).FirstOrDefault().SpawnCard;

            //return whether we successfully found a value
            return (returnedCard != null);
        }
        /// <summary>
        /// attempts to add this SceneVariant to the current director
        /// </summary>
        /// <param name="director">the director that is collecting interactable cards</param>
        /// <param name="selection">the card selection that our Scene Variant may be added into</param>
        public virtual void AddVariantToDirector(SceneDirector director, DirectorCardCategorySelection selection)
        {
            DirectorCard? dCard = null;
            //only continue if we found a scene variant for this director's stage
            var gotValue = GetSceneVariant(SceneInfo.instance.sceneDef.nameToken, out dCard);
            if (gotValue)
            {
                //add our card only if it matches the current scene and category
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
        //the link for stage names => stage variant used in Scene Variant cards
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
