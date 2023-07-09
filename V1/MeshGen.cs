using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Planets.Static;
using Planets.Tools;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Planets.Generation.V1 {
    public class MeshGen : MonoBehaviour {
        private Dictionary<Vector3, int> _verticesDictionary = new Dictionary<Vector3, int>();
        private List<Vector3> _verticesList = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private float[,,] _3dMap;
        private Mesh _worldMesh;
        public void UpdateMapValues() {
            _3dMap = new float[1, 1, 1];
            _3dMap = noise.Get3DMap();
            _have3DMap = _3dMap != new float[1, 1, 1];
        }
        
        [Title("Values", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField] private float createGate = 5;
        [SerializeField] private float valueGate;
        [SerializeField] private bool doSmoothing;
        [SerializeField] private bool doFlatShading;
        
        [Title("References", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private NoiseGen noise;
        [SerializeField] private Material mat;
        
        [Title("Events", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private UnityEvent onGenerate;

        [Title("DEBUG", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private TMP_Text text;
        [ShowInInspector, ReadOnly] private bool _have3DMap;
        [Space(5f)]
        [ShowInInspector, ReadOnly, SuffixLabel("ms")] private float _buildTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _cubeGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _timeTaken;

        [Button]
        public void Generate() {
            if (_3dMap == new float[1, 1, 1]) return;
            var startTime = Time.realtimeSinceStartup;
            if (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
            ClearMeshData();
            var startTime2 = Time.realtimeSinceStartup;
            for (int x = 0; x < _3dMap!.GetLength(0) - 1; x++) {
                for (int y = 0; y < _3dMap!.GetLength(1) - 1; y++) {
                    for (int z = 0; z < _3dMap!.GetLength(2) - 1; z++) {
                        if (_3dMap[x, y, z] < -createGate || _3dMap[x, y, z] > createGate) continue;
                        CreateCube(new int3(x,y,z));
                    }
                }
            }
            _cubeGenerationTime = Time.realtimeSinceStartup - startTime2;
            BuildMesh();
            onGenerate?.Invoke();
            _timeTaken = Time.realtimeSinceStartup - startTime;
            _buildTime *= 1000;
            UpdateUI();
        }
        private int GetCubeConfig(float[] cube) {
            int configIndex = 0;
            for (int i = 0; i < 8; i++) {
                if (cube[i] < valueGate) configIndex |= 1 << i;
            }

            return configIndex;
        }
        private void CreateCube(int3 position) {
            float[] cube = new float[8];
            for (int i = 0; i < 8; i++) {
                cube[i] = SampleMap(position + VoxelTables.CornerTable[i]);
            }
            int configIndex = GetCubeConfig(cube);
            if (configIndex is 0 or 255) return;
            int edgeIndex = 0;
            for (int i = 0; i < 5; i++) { //Mesh triangles
                for (int j = 0; j < 3; j++) { // Mesh Points
                    var indice = VoxelTables.TriangleTable[configIndex, edgeIndex];
                    if (indice == -1) return;
                    var edge1 = VoxelTables.EdgeIndexes[indice, 0];
                    var edge2 = VoxelTables.EdgeIndexes[indice, 1];
                    float3 vert1 = position + VoxelTables.CornerTable[edge1];
                    float3 vert2 = position + VoxelTables.CornerTable[edge2];
                    float3 vertPosition;
                    if (doSmoothing) {
                        float vert1Sample = cube[edge1];
                        float vert2Sample = cube[edge2];
                        float difference = vert2Sample - vert1Sample;
                        if (difference == 0) difference = valueGate;
                        else difference = (valueGate - vert1Sample) / difference;
                        vertPosition = vert1 + ((vert2 - vert1) * difference);
                    }else {
                        vertPosition = (vert1 + vert2) * .5f;
                    }
                    if (doFlatShading) {
                        _verticesList.Add(vertPosition);
                        _triangles.Add(_verticesList.Count - 1);
                    }else {
                        _triangles.Add(VertForIndice(vertPosition));
                    }
                    edgeIndex++;
                }
            }
        }
       
        private float SampleMap(int3 point) {
            return _3dMap[point.x, point.y, point.z];
        }
        private int VertForIndice(Vector3 vert) {
            if (_verticesDictionary.TryGetValue(vert, out int index)) {
                return index;
            }
            int newIndex = _verticesDictionary.Count;
            _verticesDictionary.Add(vert, newIndex);
            return newIndex;
        }

        private void UpdateUI() {
            text.text = "Total Time - " + _timeTaken.ToString(CultureInfo.InvariantCulture);
        }
        
        #region Mesh Functions

        private void BuildMesh() {
            var startTime = Time.realtimeSinceStartup;

            var obj = new GameObject {
                transform = {
                    parent = transform,
                    localScale = scale
                },
                name = "Mesh Target"
            };
            var filter = obj.AddComponent<MeshFilter>();
            var mRenderer = obj.AddComponent<MeshRenderer>();
            var offset = obj.AddComponent<ObjectOffsetTool>();
            var viewer = obj.AddComponent<BoundaryViewer>();
            var info = obj.AddComponent<MeshDataViewer>();
            
            _worldMesh = new Mesh();
            _worldMesh.indexFormat = IndexFormat.UInt32;
            _worldMesh.name = "Built Mesh";
            if (doFlatShading) _worldMesh.vertices = _verticesList.Select(vert => new Vector3(vert.x, vert.y, vert.z)).ToArray();
            else _worldMesh.vertices = _verticesDictionary.Select(vert => new Vector3(vert.Key.x, vert.Key.y, vert.Key.z)).ToArray();
            _worldMesh.triangles = _triangles.ToArray();
            _worldMesh.normals = CalculateNormals();
            _worldMesh.Optimize();
            
            filter.sharedMesh = _worldMesh;
            mRenderer.sharedMaterial = mat;
            offset.UpdateOffset();
            viewer.GetBoundary();
            info.UpdateData();
            _buildTime = Time.realtimeSinceStartup - startTime;
        }
        private void ClearMeshData() {
            _verticesDictionary.Clear();
            _verticesList.Clear();
            _triangles.Clear();
            UpdateMapValues();
        }
        private Vector3[] CalculateNormals() {
            Vector3[] vertexNormals = new Vector3[doFlatShading ? _verticesList.Count : _verticesDictionary.Count];
            int triangleCont = _triangles.Count / 3;
            for (int i = 0; i < triangleCont; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = _triangles[normalTriangleIndex];
                int vertexIndexB = _triangles[normalTriangleIndex + 1];
                int vertexIndexC = _triangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }

            for (int i = 0; i < vertexNormals.Length; i++) {
                vertexNormals[i].Normalize();
            }

            return vertexNormals;
        }
        private Vector3 SurfaceNormal(int indexA, int indexB, int indexC) {
            Vector3 pointA = doFlatShading ? _verticesList[indexA] : _verticesDictionary.ElementAt(indexA).Key;
            Vector3 pointB = doFlatShading ? _verticesList[indexB] : _verticesDictionary.ElementAt(indexB).Key;
            Vector3 pointC = doFlatShading ? _verticesList[indexC] : _verticesDictionary.ElementAt(indexC).Key;

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
        
            return Vector3.Cross(sideAb, sideAc).normalized;
        }
        

        #endregion
    }
}
