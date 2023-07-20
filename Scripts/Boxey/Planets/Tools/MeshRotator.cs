using UnityEngine;

namespace Boxey.Planets.Tools { 
    public class MeshRotator : MonoBehaviour {
        void FixedUpdate() {
            transform.Rotate(Vector3.up, .05f);
        }
    }
}
