using Boxey.Planets.Core.Static;
using UnityEngine;

namespace Boxey.Planets.Core.Components { 
    public class MeshRotator : MonoBehaviour {
        private readonly Vector2 _range = new Vector2(.01f, 0.001f);
        private float _up;
        private float _left;
        private float _forward;

        private void Awake() {
            _up = _range.Random();
            _left = _range.Random();
            _forward = _range.Random();
        }

        private void Update() {
            transform.Rotate(Vector3.up, _up);
            transform.Rotate(Vector3.left, _left);
            transform.Rotate(Vector3.forward, _forward);
        }
    }
}
