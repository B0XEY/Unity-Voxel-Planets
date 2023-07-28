using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Boxey.Core {
    [CreateAssetMenu(menuName = "Data/Planet Settings", fileName = "New Planet Settings", order = -5)]
    public class PlanetSettings : ScriptableObject {
        public void RandomSeed() => seed = Random.Range(-999999, 999999);
        private void RandomSeedToggle() => randomSeed = !randomSeed;
        //Terraforming
        [Title("Terraform Settings", TitleAlignment = TitleAlignments.Centered)]
        [TabGroup("Basic")] [Range(0.01f, 10f)] public float groundToughness = 1;
        //Noise
        [Title("Noise Settings", TitleAlignment = TitleAlignments.Centered)]
        [TabGroup("Basic")] public bool useNoiseMap;
        [TabGroup("Basic"), ShowIf("@useNoiseMap")]
        [InlineButton("RandomSeed", " R"), InlineButton("RandomSeedToggle", "T")] public int seed;
        [TabGroup("Basic"), ShowIf("@useNoiseMap")] [ReadOnly] public bool randomSeed = true;
        [Space] 
        [TabGroup("Basic"), ShowIf("@useNoiseMap")] public NoiseSettings[] noiseLayers = new NoiseSettings[1];

        //Colors
        [Title("Ground Settings", titleAlignment: TitleAlignments.Centered)]
        [TabGroup("Planet")] [Range(0f, 1.5f)] public float groundHighlightsStrength = .6f;
        [TabGroup("Planet")] public Color ground = new Color(22/255f, 185/255f, 84/255f, 1);
        [TabGroup("Planet")] public Color groundHighlight = new Color(22/255f, 171/255f, 28/255f, 1);
        
        [Title("Sand Settings", titleAlignment: TitleAlignments.Centered)]
        [TabGroup("Planet")] [Range(0f, 1.5f)] public float sandHighlightsStrength = .653f;
        [TabGroup("Planet")] public float sandHeight = 49.4f;
        [TabGroup("Planet")] public Color sand = new Color(255/255f, 244/255f, 136/255f, 1);
        [TabGroup("Planet")] public Color sandHighlights = new Color(209/255f, 181/255f, 102/255f, 1);

        [Title("Rock Settings", titleAlignment: TitleAlignments.Centered)] 
        [TabGroup("Planet")] [Range(0f, 1.5f)] public float rockHighlightsStrength = .172f;
        [TabGroup("Planet")] [Range(0f, 1f)] public float rockThreshold = .823f;
        [TabGroup("Planet")] public Color rock = new Color(106/255f, 80/255f, 66/255f , 1);
        [TabGroup("Planet")] public Color rockHighlights = new Color(26/255f, 21/255f, 17/255f, 1);
        //Water
        [Title("Water Settings", titleAlignment: TitleAlignments.Centered)]
        [TabGroup("Water")] public Color shallowColor = new Color(71/255f, 123/255f, 255/255f, 150/255f);
        [TabGroup("Water")] public Color deepColor = new Color(10/255f, 117/255f, 236/255f, 235/255f);
        [TabGroup("Water")] public float deepFadeDistance = -2.13f;
        [TabGroup("Water")] [Range(0f, 2f)] public float depthStrength = .247f;
        
        [Title("Water Foam Settings", titleAlignment: TitleAlignments.Centered)]
        [TabGroup("Water")] public Color foamColor = Color.white;
        [TabGroup("Water")] public float foamAmount = -.05f;
        [TabGroup("Water")] [Range(0f, 2f)] public float foamStrength = 1.107f;
        [TabGroup("Water")] [Range(0f, 2f)] public float foamCutoff = 1f;
        [TabGroup("Water")] [Range(0.01f, 1f)] public float foamSpeed = 0.05f;
        
        //Clouds
        [Title("Clouds Settings", titleAlignment: TitleAlignments.Centered)]
        [TabGroup("Sky")] public Color cloudColor = Color.white;
        [TabGroup("Sky")] public float cloudSize = .02f;
        [TabGroup("Sky")] public float cloudSoftness = .221f;
        [TabGroup("Sky")] public float cloudiness = .43f;
        [TabGroup("Sky")] public float cloudSpeed = 1;
    }
}