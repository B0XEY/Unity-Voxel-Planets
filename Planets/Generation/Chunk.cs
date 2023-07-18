using System.Collections.Generic;
using System.Linq;
using Boxey.Planets.Static;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boxey.Planets.Generation {
    public class Chunk {
        private Dictionary<Vector3, int> _verticesDictionary;
        private List<Vector3> _verticesList;
        private List<int> _triangles;
        private float[,,] _3dMap;
        public readonly GameObject ChunkObject;

        private readonly int _size;
        private readonly float _valueGate;
        private readonly float _createGate;
        private readonly bool _doSmoothing;
        private readonly bool _doFlatShading;
        private readonly int _viewDistance;
        private readonly int _collideDistance;
        private readonly Material _mat;
        private readonly Transform _object;
        
        private MeshCollider _chunkCollider;

        public Chunk(float[,,] map, Vector3Int offset, Vector3Int position, int chunkSize, float valueGate, float createGate, bool doSmoothing, bool flatShading, int viewDistance, int colliderDistance, Material mat) {
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
        public void RegenerateChunk(float radius, float speed, bool remove, float3 center) {
            Terraform(radius, speed, remove, center);
            if (_doFlatShading) _verticesList = new List<Vector3>();
            else _verticesDictionary = new Dictionary<Vector3, int>();
            _triangles = new List<int>();
            Generate();
        }
        public void Update() {
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

        #region Terraforming
        private void Terraform(float radius, float speed, bool remove, float3 center) {
            var numbers = ChunkObject.name.Split(",");
            var position = new float3(float.Parse(numbers[0]), float.Parse(numbers[1]), float.Parse(numbers[2]));
            VoxelNoise.TerraformChunk(ref _3dMap, _size, remove, radius, speed, center, position);
        }
        #endregion
        
        #region Mesh Functions

        public Mesh GetMesh() => ChunkObject.GetComponent<MeshFilter>().sharedMesh;
        public MeshFilter GetMeshFilter() => ChunkObject.GetComponent<MeshFilter>();

        // ReSharper disable Unity.PerformanceAnalysis
        private void BuildMesh() {
            var filter = new MeshFilter();
            if (ChunkObject.TryGetComponent<MeshFilter>(out var f)) filter = f;
            else filter = ChunkObject.AddComponent<MeshFilter>();
            var mRenderer = new MeshRenderer();
            if (ChunkObject.TryGetComponent<MeshRenderer>(out var m)) mRenderer = m;
            else mRenderer = ChunkObject.AddComponent<MeshRenderer>();
            
            if (ChunkObject.TryGetComponent<MeshCollider>(out var mc)) _chunkCollider = mc;
            else _chunkCollider = ChunkObject.AddComponent<MeshCollider>();

            var worldMesh = new Mesh {
                name = "Built Mesh"
            };
            if (_doFlatShading) worldMesh.vertices = _verticesList.Select(vert => new Vector3(vert.x, vert.y, vert.z)).ToArray();
            else worldMesh.vertices = _verticesDictionary.Select(vert => new Vector3(vert.Key.x, vert.Key.y, vert.Key.z)).ToArray();
            worldMesh.triangles = _triangles.ToArray();
            worldMesh.normals = CalculateNormals();
            worldMesh.Optimize();

            _chunkCollider.sharedMesh = worldMesh;
            
            filter.sharedMesh = worldMesh;
            mRenderer.sharedMaterial = _mat;
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
