using Boxey.Planets.Static;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Boxey.Planets.Tools {
    [RequireComponent(typeof(MeshFilter))]
    public class BoundaryViewer : MonoBehaviour {
        private Bounds _meshBounds;
        [Title("Type", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private bool outline = true;
        [SerializeField] private Color gizmosColor = new Color(255f/255f, 235f/255f, 4f/255f, 25f/255f);
        
        [Button]
        public void GetBoundary() {
            var filter = GetComponent<MeshFilter>();
            _meshBounds = filter.sharedMesh.bounds;
        }

        private void OnDrawGizmos() {
            var offset = Helpers.MultiplyVector3(_meshBounds.center, transform.localScale);
            var size = Helpers.MultiplyVector3(_meshBounds.size, transform.localScale);
            Gizmos.color = gizmosColor;
            if (outline) Gizmos.DrawWireCube(transform.position + offset, size);
            else Gizmos.DrawCube(transform.position + offset, size);
        }
    }
}
