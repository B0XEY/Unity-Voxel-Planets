﻿using Boxey.Core.Editor;
using UnityEngine;

namespace Boxey.Core {
    [CreateAssetMenu(menuName = "Data/Planet Settings", fileName = "New Planet Settings", order = -5)]
    public class PlanetSettings : ScriptableObject{
        [HideInInspector] public int seed;
        //Terraforming
        [Header("Terraform Settings")] 
        [Line (1.5f, .5f,.5f,.5f)]
        [Range(0.01f, 10f)]public float groundToughness = 1f;
        //Noise
        [Header("Noise Settings")]
        [Line (1.5f, .5f,.5f,.5f)]
        public bool useNoiseMap;
        [Space] 
        public NoiseSettings[] noiseLayers = new NoiseSettings[1];

        //Colors
        [Header("Ground Settings")]
        [Line (1.5f, .5f,.5f,.5f)]
        [Range(0f, 1.5f)] public float groundHighlightsStrength = .6f;
        public Color ground = new (22/255f, 185/255f, 84/255f, 1);
        public Color groundHighlight = new (22/255f, 171/255f, 28/255f, 1);
        
        [Header("Sand Settings")]
        [Line (1.5f, .5f,.5f,.5f)]
        [Range(0f, 1.5f)] public float sandHighlightsStrength = .653f;
        public float sandHeight = 49.4f;
        public Color sand = new (255/255f, 244/255f, 136/255f, 1);
        public Color sandHighlights = new (209/255f, 181/255f, 102/255f, 1);

        [Header("Rock Settings")] 
        [Line (1.5f, .5f,.5f,.5f)]
        [Range(0f, 1.5f)] public float rockHighlightsStrength = .172f;
        [Range(0f, 1f)] public float rockThreshold = .823f;
        public Color rock = new (106/255f, 80/255f, 66/255f , 1);
        public Color rockHighlights = new (26/255f, 21/255f, 17/255f, 1);
        //Water
        [Header("Water Settings")]
        [Line (1.5f, .5f,.5f,.5f)]
        public Color shallowColor = new (71/255f, 123/255f, 255/255f, 150/255f);
        public Color deepColor = new (10/255f, 117/255f, 236/255f, 235/255f);
        public float deepFadeDistance = -2.13f;
        [Range(0f, 2f)] public float depthStrength = .247f;
        
        [Header("Water Foam Settings")]
        [Line (1.5f, .5f,.5f,.5f)]
        public Color foamColor = Color.white;
        public float foamAmount = -.05f;
        [Range(0f, 2f)] public float foamStrength = 1.107f;
        [Range(0f, 2f)] public float foamCutoff = 1f;
        [Range(0.01f, 1f)] public float foamSpeed = 0.05f;
        
        //Clouds
        [Header("Clouds Settings")]
        [Line (1.5f, .5f,.5f,.5f)]
        public bool hasAtmosphere = true;
        [ColorUsage(true,true)] public Color cloudColor = Color.white;
        public float cloudSize = .02f;
        public float cloudSoftness = .221f;
        public float cloudiness = .43f;
        public float cloudSpeed = 1;
    }
}