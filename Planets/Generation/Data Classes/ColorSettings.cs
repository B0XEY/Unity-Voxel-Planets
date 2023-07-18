using Sirenix.OdinInspector;
using UnityEngine;

namespace Boxey.Planets.Generation.Data_Classes {
    [CreateAssetMenu(menuName = "Data/Color Settings", fileName = "New Color Settings", order = 0)]
    public class ColorSettings : ScriptableObject {
        [Title("Grass Values", titleAlignment: TitleAlignments.Centered)]
        [Range(0f, 1.5f)] public float grassHighlightsStrength = .6f;
        public Color grass = new Color(22/255f, 185/255f, 84/255f, 1);
        public Color grassHighlights = new Color(22/255f, 171/255f, 28/255f, 1);
        [Title("Sand Values", titleAlignment: TitleAlignments.Centered)]
        [Range(0f, 1.5f)] public float sandHighlightsStrength = .82f;
        public float sandHeight = 52.7f;
        public Color sand = new Color(255/255f, 244/255f, 136/255f, 1);
        public Color sandHighlights = new Color(209/255f, 181/255f, 102/255f, 1);

        [Title("Rock Values", titleAlignment: TitleAlignments.Centered)] 
        [Range(0f, 1.5f)] public float rockHighlightsStrength = .31f;
        [Range(0f, 1f)] public float rockThreshold = .95f;
        public Color rock = new Color(106/255f, 80/255f, 66/255f , 1);
        public Color rockHighlights = new Color(26/255f, 21/255f, 17/255f, 1);
    }
}