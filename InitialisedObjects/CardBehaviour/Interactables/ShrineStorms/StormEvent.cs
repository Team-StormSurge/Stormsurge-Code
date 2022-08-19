using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StormSurge.Interactables
{
    /// <summary>
    /// The StormEvent object used to determine values for stage-specific storm behaviours
    /// </summary>
    [CreateAssetMenu(menuName = "Stormsurge/Storm Event")]
    public class StormEvent : UnityEngine.ScriptableObject
    {
        [Header("Main Properties")]

        [Tooltip("The biome-specific token for the storm event.")]
        public string? startMessageToken;

        [Tooltip("The Particle Systems created.")]
        public GameObject? stormEffect;

        [Tooltip("The PP Volume added.")]
        public GameObject? postProcessingVolume;

        [Tooltip("Defines the storm's wet-ground material.")]
        [Obsolete]
        public Material? StormMaterial;

        [Space(height: 2)]
        [Header("Fog Properties")]

        [Tooltip("Controls the colour of the blindness effect.")]
        public Color fogColour;

        [Tooltip("The distance at which blindness fog renders.")]
        public int fogDistance;
    }
}
