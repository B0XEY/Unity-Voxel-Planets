using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boxey.Core;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Generation {
    public class Chunk {
        public GameObject GetChunkObj { get; }
        public bool MeshGenerated { get; private set; }

        public bool TerraformingChunk;

        private readonly Dictionary<Vector3, int> _verticesDictionary;
        private readonly List<Vector3> _verticesList;
        private readonly List<int> _triangles;
        private float[,,] _noiseMap;

        private readonly Camera _camera;
        private readonly int _size;
        private readonly float _valueGate;
        private readonly float _createGate;
        private readonly bool _doSmoothing;
        private readonly bool _doFlatShading;
        
        private Material _mat;
        
        private readonly LodData _lodData;
        private Mesh _chunkMesh;
        private readonly MeshFilter _chunkFilter;
        private readonly MeshRenderer _chunkRenderer;
        private readonly MeshCollider _chunkCollider;

        public Chunk(Vector3Int position, Vector3 offset,  int chunkSize, PlanetCreator.ChunkData data) {
            MeshGenerated = false;
            _size = chunkSize;
            _valueGate = data.value;
            _createGate = data.create;
            _doSmoothing = data.smooth;
            _doFlatShading = data.flat;
            _lodData = data.lod;
            _mat = data.mat;
            if (!Application.isPlaying) _noiseMap = VoxelNoise.GetPlanetNoiseMap(chunkSize, data.size, position.ToFloat3(), data.radius, data.settings);
            else _noiseMap = VoxelNoise.GetPlanetNoiseMapJob(chunkSize, data.size, position.ToFloat3(), data.radius, data.settings);
            if (_doFlatShading) _verticesList = new List<Vector3>();
            else _verticesDictionary = new Dictionary<Vector3, int>();
            _triangles = new List<int>();
            _camera = Helpers.GetCamera;
            
            GetChunkObj = new GameObject {
                transform = {
                    position = position + offset,
                    localScale = Vector3.one
                },
                name = $"{position.x},{position.y},{position.z}"
            };
            _chunkMesh = new Mesh {
                name = "LOD - 0"
            };
            _chunkFilter = GetChunkObj.AddComponent<MeshFilter>();
            _chunkRenderer = GetChunkObj.AddComponent<MeshRenderer>();
            _chunkCollider = GetChunkObj.AddComponent<MeshCollider>();
            Generate();
        }

        private void Generate() {
            ClearMeshData();
            for (int x = 0; x < _size; x++) {
                for (int y = 0; y < _size; y++) {
                    for (int z = 0; z < _size; z++) {
                        if (_noiseMap[x, y, z] < -_createGate || _noiseMap[x, y, z] > _createGate) continue;
                        CreateCube(new int3(x, y, z));
                    }
                }
            }
            BuildMesh();
        }
        private int GetCubeConfig(float[] cube) {
            var configIndex = 0;
            for (int i = 0; i < 8; i++) {
                if (cube[i] < _valueGate) configIndex |= 1 << i;
            }

            return configIndex;
        }
        private void CreateCube(int3 position) {
            float[] cube = new float[8];
            for (int i = 0; i < 8; i++) {
                cube[i] = SampleMap(position + VoxelTables.CornerTable[i]);
            }
            int configIndex = GetCubeConfig(cube);
            if (configIndex is 0 or 255) {
                return;
            }
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
                    if (_doSmoothing) {
                        float vert1Sample = cube[edge1];
                        float vert2Sample = cube[edge2];
                        float difference = vert2Sample - vert1Sample;
                        if (difference == 0) difference = _valueGate;
                        else difference = (_valueGate - vert1Sample) / difference;
                        vertPosition = vert1 + ((vert2 - vert1) * difference);
                    }else {
                        vertPosition = (vert1 + vert2) * .5f;
                    }
                    if (_doFlatShading) {
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
            return _noiseMap[point.x, point.y, point.z];
        }
        private int VertForIndice(Vector3 vert) {
            if (_verticesDictionary.TryGetValue(vert, out int index)) {
                return index;
            }
            int newIndex = _verticesDictionary.Count;
            _verticesDictionary.Add(vert, newIndex);
            return newIndex;
        }

        #region Chunk Commands
        public void UpdateMaterial(Material newMat) {
            _chunkRenderer.sharedMaterial = newMat;
            _mat = newMat;
        }
        public float[,,] GetChunkMap() => _noiseMap;
        public void Clear(){
            _noiseMap = null;
            _chunkMesh = null;
        }
        public void UpdateChunk(float[,,] newMap) {
            _noiseMap = newMap;
            Generate();
        }
        public void Update() {
            if (!MeshGenerated) return;
            var distance = Vector3.Distance(GetChunkObj.transform.position, _camera.transform.position);
            var toggle = distance <= _lodData.viewDistance;
            GetChunkObj.SetActive(toggle);
            _chunkCollider.enabled = toggle;
        }
        #endregion

        #region Mesh Functions

        public MeshFilter GetFilter() {
            return MeshGenerated ? _chunkFilter : null;
        }
        private void BuildMesh() {
            MeshGenerated = false;
            GetChunkObj.SetActive(MeshGenerated);
            if (_doFlatShading ? _verticesList.Count == 0 : _verticesDictionary.Count == 0) {
                return;
            }
            _chunkMesh.Clear();
            if (_doFlatShading) {
                _chunkMesh.vertices = _verticesList.Select(vert => new Vector3(vert.x, vert.y, vert.z)).ToArray();
            }else {
                _chunkMesh.vertices = _verticesDictionary.Select(vert => new Vector3(vert.Key.x, vert.Key.y, vert.Key.z)).ToArray();
            }
            _chunkMesh.triangles = _triangles.ToArray();
            _chunkMesh.normals = CalculateNormals(_doFlatShading ? _verticesList : _verticesDictionary);
            _chunkCollider.sharedMesh = _chunkMesh;
            _chunkFilter.sharedMesh = _chunkMesh;
            _chunkRenderer.sharedMaterial = _mat;
            MeshGenerated = true;
            GetChunkObj.SetActive(MeshGenerated);
        }
        private void ClearMeshData() {
            if (_doFlatShading) _verticesList.Clear();
            else _verticesDictionary.Clear();
            _triangles.Clear();
        }
        private Vector3[] CalculateNormals(ICollection verts) {
            Vector3[] vertexNormals = new Vector3[verts.Count];
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
            Vector3 pointA = _doFlatShading ? _verticesList[indexA] : _verticesDictionary.ElementAt(indexA).Key;
            Vector3 pointB = _doFlatShading ? _verticesList[indexB] : _verticesDictionary.ElementAt(indexB).Key;
            Vector3 pointC = _doFlatShading ? _verticesList[indexC] : _verticesDictionary.ElementAt(indexC).Key;

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
        
            return Vector3.Cross(sideAb, sideAc).normalized;
        }

        #endregion
    }
}
