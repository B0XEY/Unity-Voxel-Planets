using UnityEngine;

namespace Boxey.Scripts.Planets.Tools { 
    public class MeshRotator : MonoBehaviour {
        private void Update() {
            transform.Rotate(Vector3.up, .005f);
        }
    }
}
