using Boxey.Planets.Static;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Boxey.Planets.Tools {
    [RequireComponent(typeof(MeshFilter))]
    public class ObjectOffsetTool : MonoBehaviour {
        [Button]
        public void UpdateOffset() {
            var filter = GetComponent<MeshFilter>();
            Vector3 newPosition = Helpers.MultiplyVector3(filter.sharedMesh.bounds.center, transform.localScale);
            newPosition *= -1f;
            transform.position = newPosition;
            Helpers.GetCamera.transform.position = new Vector3(0, 0, newPosition.z * 2);
        }

        public Vector3 GetPosition() => Helpers.DivideVector3(transform.position, transform.localScale);
    }
}
