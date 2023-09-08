using Boxey.Planets.Core.Editor;
using UnityEngine;
using UnityEngine.Events;

namespace Boxey.Core.Components {
    public class PlanetList : MonoBehaviour {
        [SerializeField] private UnityEvent onGetPlanets;
        
        [Header("Planets")] 
        [Line]
        [SerializeField] private PlanetCreator[] planets;
        [Button("Get Planets")]
        public void GetAllPlanets() {
            planets = FindObjectsOfType<PlanetCreator>();
            onGetPlanets?.Invoke();
        }

        public PlanetCreator[] GetPlanets() => planets;
    }
}
