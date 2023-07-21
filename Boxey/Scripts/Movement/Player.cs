using UnityEngine;

namespace Boxey.Scripts.Movement {
    public class Player : MonoBehaviour {
        private Rigidbody _rb;

        private void Awake() {
            TryGetComponent<Rigidbody>(out _rb);
        }
    }
}
