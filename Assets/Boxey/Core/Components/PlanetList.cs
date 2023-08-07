using Boxey.Core.Editor;
using UnityEngine;
using UnityEngine.Events;

namespace Boxey.Core.Components {
    public class PlanetList : MonoBehaviour {
        [Header("Events")]
        [Line (1.5f, .5f,.5f,.5f)]
        [SerializeField] private UnityEvent onGetPlanets;
        
        [Header("Planets")] 
        [Line (1.5f, .5f,.5f,.5f)]
        [SerializeField] private PlanetCreator[] planets;
        public void GetAllPlanets() {
            planets = FindObjectsOfType<PlanetCreator>();
            onGetPlanets?.Invoke();
        }

        public PlanetCreator[] GetPlanets() => planets;
    }
}
