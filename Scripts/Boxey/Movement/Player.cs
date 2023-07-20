using System;
using UnityEngine;

namespace Boxey.Movement {
    public class Player : MonoBehaviour {
        private Rigidbody _rb;

        private void Awake() {
            TryGetComponent<Rigidbody>(out _rb);
        }
    }
}
