using Sirenix.OdinInspector;
using UnityEngine;

namespace Boxey.Planets.Generation.Data_Classes {
    [CreateAssetMenu(menuName = "Data/Noise Settings", fileName = "New Noise Settings", order = -1)]
    public class NoiseSettings : ScriptableObject {
        [Title("Noise Settings", TitleAlignment = TitleAlignments.Centered)]
        public float scale = .65f;
        [Space(5f)]
        [Range(1, 8)] public int octaves = 3;
        [Range(0.01f, 7f)] public float lacunarity = 3.66f;
        [Range(0.01f, 15f)] public float amplitude = 15f;
        [Range(0.01f, 5f)] public float frequency = .804f;
        [Range(0.01f, 3f)] public float persistence = .15f;
        [Space(5f)]
        public Vector3 offset;
    }
}