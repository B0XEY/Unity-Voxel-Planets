using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Boxey.Core.Components {
    public class PlanetList : MonoBehaviour {
        [SerializeField] private UnityEvent onGetPlanets;
        
        [Title("Planets", titleAlignment: TitleAlignments.Centered)] 
        [SerializeField] private PlanetCreator[] planets;
        [Button("Get Planets", ButtonSizes.Large)]
        public void GetAllPlanets() {
            planets = FindObjectsOfType<PlanetCreator>();
            onGetPlanets?.Invoke();
        }

        public PlanetCreator[] GetPlanets() => planets;
    }
}
