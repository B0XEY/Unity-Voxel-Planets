using Sirenix.OdinInspector;
using UnityEngine;

namespace Boxey.Scripts.Planets.Tools {
    [RequireComponent(typeof(MeshFilter))]
    public class MeshDataViewer : MonoBehaviour {
        private string _numOfVertex;
        private string _numOfTriangles;
        [Title("@_numOfVertex", titleAlignment: TitleAlignments.Centered, horizontalLine: false, Bold = true)]
        [Title("@_numOfTriangles", titleAlignment: TitleAlignments.Centered, horizontalLine: true, Bold = true)]
        
        [Button]
        public void UpdateData() {
            var verts = GetComponent<MeshFilter>().sharedMesh.vertices.Length;
            _numOfVertex = verts.ToString() + " Vertices";
            _numOfTriangles = (verts / 3).ToString()  + " Triangles";
        }
    }
}
