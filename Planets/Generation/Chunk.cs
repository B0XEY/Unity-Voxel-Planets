using System.Collections.Generic;
using System.Linq;
using Boxey.Planets.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Generation {
    public class Chunk {
        public readonly GameObject ChunkObject;
        
        private readonly Dictionary<Vector3, int> _verticesDictionary;
        private readonly List<Vector3> _verticesList;
        private readonly List<int> _triangles;
        private float[,,] _3dMap;

        private readonly int _size;
        private readonly float _valueGate;
        private readonly float _createGate;
        private readonly bool _doSmoothing;
        private readonly bool _doFlatShading;
        private readonly int _viewDistance;
        private readonly int _collideDistance;
        
        private readonly Material _mat;
        private readonly Transform _object;
        
        private readonly Mesh _chunkMesh;
        private readonly MeshFilter _chunkFilter;
        private readonly MeshRenderer _chunkRenderer;
        private readonly MeshCollider _chunkCollider;

        public Chunk(float[,,] map, Vector3Int position, int chunkSize, float valueGate, float createGate, bool doSmoothing, bool flatShading, int viewDistance, int colliderDistance, Material mat) {
            _3dMap = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];
            _size = chunkSize;
            _valueGate = valueGate;
            _createGate = createGate;
            _doSmoothing = doSmoothing;
            _doFlatShading = flatShading;
            _viewDistance = viewDistance;
            _collideDistance = colliderDistance;
            _mat = mat;
            for (int x = 0; x <= _size; x++) {
                for (int y = 0; y <= _size; y++) {
                    for (int z = 0; z <= _size; z++) {
                        _3dMap[x, y, z] = map[(x + position.x), (y + position.y), (z + position.z)];
                    }
                }
            }
            if (_doFlatShading) _verticesList = new List<Vector3>();
            else _verticesDictionary = new Dictionary<Vector3, int>();
            _triangles = new List<int>();
            _object = Helpers.GetCamera.transform;
            
            ChunkObject = new GameObject {
                transform = {
                    position = position,
                    rotation = Quaternion.identity,
                    localScale = Vector3.one
                },
                name = $"{position.x},{position.y},{position.z}"
            };
            _chunkMesh = new Mesh{
                name = "Built Mesh"
            };
            _chunkFilter = ChunkObject.AddComponent<MeshFilter>();
            _chunkRenderer = ChunkObject.AddComponent<MeshRenderer>();
            _chunkCollider = ChunkObject.AddComponent<MeshCollider>();
            Generate();
            SetCollision(false);
            if (Application.isPlaying) SetVisible(false);
        }

        private void Generate() {
            ClearMeshData();
            for (int x = 0; x < _size; x++) {
                for (int y = 0; y < _size; y++) {
                    for (int z = 0; z < _size; z++) {
                        if (_3dMap[x, y, z] < -_createGate || _3dMap[x, y, z] > _createGate) continue;
                        CreateCube(new int3(x, y, z));
                    }
                }
            }
            BuildMesh();
        }
        private int GetCubeConfig(float[] cube) {
            int configIndex = 0;
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

        #region Chunk Commands
        public float[,,] GetChunkMap() => _3dMap;
        public void UpdateChunk(float[,,] newMap) {
            _3dMap = newMap;
            Generate();
        }
        
        public void Update() {
            if (_doFlatShading ? _verticesList.Count == 0 : _verticesDictionary.Count == 0) {
                SetVisible(false);
                SetCollision(false);
                return;
            }
            var distance = Vector3.Distance(ChunkObject.transform.position, _object.position);
            SetVisible(distance <= _viewDistance);
            SetCollision(distance <= _collideDistance);
        }
        private void SetVisible(bool visible) {
            ChunkObject.SetActive(visible);
        }
        private void SetCollision(bool enabled) {
            _chunkCollider.enabled = enabled;
        }
        #endregion

        #region Mesh Functions
        private void BuildMesh() {
            _chunkMesh.Clear();
            if (_doFlatShading ? _verticesList.Count == 0 : _verticesDictionary.Count == 0) {
                SetVisible(false);
                SetCollision(false);
                return;
            }
            if (_doFlatShading) _chunkMesh.vertices = _verticesList.Select(vert => new Vector3(vert.x, vert.y, vert.z)).ToArray();
            else _chunkMesh.vertices = _verticesDictionary.Select(vert => new Vector3(vert.Key.x, vert.Key.y, vert.Key.z)).ToArray();
            _chunkMesh.triangles = _triangles.ToArray();
            _chunkMesh.normals = CalculateNormals();
            _chunkMesh.RecalculateBounds();
            _chunkMesh.Optimize();

            _chunkCollider.sharedMesh = _chunkMesh;
            _chunkFilter.sharedMesh = _chunkMesh;
            _chunkRenderer.sharedMaterial = _mat;
        }
        private void ClearMeshData() {
            if (_doFlatShading) _verticesList.Clear();
            else _verticesDictionary.Clear();
            _triangles.Clear();
        }
        private Vector3[] CalculateNormals() {
            Vector3[] vertexNormals = new Vector3[_doFlatShading ? _verticesList.Count : _verticesDictionary.Count];
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
