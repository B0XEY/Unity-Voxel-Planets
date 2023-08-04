using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boxey.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core {
    public class Chunk {
        public readonly GameObject ChunkObject;
        public bool TerraformingChunk;
        public bool ChunkMeshGenerated;

        private readonly Dictionary<Vector3, int> m_verticesDictionary;
        private readonly List<Vector3> m_verticesList;
        private readonly List<int> m_triangles;
        private float[,,] m_noiseMap;

        private readonly Camera m_camera;
        private readonly int m_size;
        private readonly float m_valueGate;
        private readonly float m_createGate;
        private readonly bool m_doSmoothing;
        private readonly bool m_doFlatShading;
        
        private Material m_mat;
        
        private readonly LodData m_lodData;
        private readonly Mesh m_chunkMesh;
        private readonly MeshFilter m_chunkFilter;
        private readonly MeshRenderer m_chunkRenderer;
        private readonly MeshCollider m_chunkCollider;

        public Chunk(float[,,] noiseMap, Vector3Int position, Vector3 offset, int chunkSize, float valueGate, float createGate, bool doSmoothing, bool flatShading, LodData lod, Material mat) {
            ChunkMeshGenerated = false;
            m_noiseMap = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];
            m_size = chunkSize;
            m_valueGate = valueGate;
            m_createGate = createGate;
            m_doSmoothing = doSmoothing;
            m_doFlatShading = flatShading;
            m_lodData = lod;
            m_mat = mat;
            for (int x = 0; x <= m_size; x++) {
                for (int y = 0; y <= m_size; y++) {
                    for (int z = 0; z <= m_size; z++) {
                        m_noiseMap[x, y, z] = noiseMap[(x + position.x), (y + position.y), (z + position.z)];
                    }
                }
            }
            if (m_doFlatShading) m_verticesList = new List<Vector3>();
            else m_verticesDictionary = new Dictionary<Vector3, int>();
            m_triangles = new List<int>();
            m_camera = Helpers.GetCamera;
            
            ChunkObject = new GameObject {
                transform = {
                    position = position + offset,
                    localScale = Vector3.one
                },
                name = $"{position.x},{position.y},{position.z}"
            };
            m_chunkMesh = new Mesh {
                name = "LOD - 0"
            };
            m_chunkFilter = ChunkObject.AddComponent<MeshFilter>();
            m_chunkRenderer = ChunkObject.AddComponent<MeshRenderer>();
            m_chunkCollider = ChunkObject.AddComponent<MeshCollider>();
            Generate();
        }

        private void Generate() {
            ClearMeshData();
            for (int x = 0; x < m_size; x++) {
                for (int y = 0; y < m_size; y++) {
                    for (int z = 0; z < m_size; z++) {
                        if (m_noiseMap[x, y, z] < -m_createGate || m_noiseMap[x, y, z] > m_createGate) continue;
                        CreateCube(new int3(x, y, z));
                    }
                }
            }
            BuildMesh();
        }
        private int GetCubeConfig(float[] cube) {
            var configIndex = 0;
            for (int i = 0; i < 8; i++) {
                if (cube[i] < m_valueGate) configIndex |= 1 << i;
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
                    if (m_doSmoothing) {
                        float vert1Sample = cube[edge1];
                        float vert2Sample = cube[edge2];
                        float difference = vert2Sample - vert1Sample;
                        if (difference == 0) difference = m_valueGate;
                        else difference = (m_valueGate - vert1Sample) / difference;
                        vertPosition = vert1 + ((vert2 - vert1) * difference);
                    }else {
                        vertPosition = (vert1 + vert2) * .5f;
                    }
                    if (m_doFlatShading) {
                        m_verticesList.Add(vertPosition);
                        m_triangles.Add(m_verticesList.Count - 1);
                    }else {
                        m_triangles.Add(VertForIndice(vertPosition));
                    }
                    edgeIndex++;
                }
            }
        }
       
        private float SampleMap(int3 point) {
            return m_noiseMap[point.x, point.y, point.z];
        }
        private int VertForIndice(Vector3 vert) {
            if (m_verticesDictionary.TryGetValue(vert, out int index)) {
                return index;
            }
            int newIndex = m_verticesDictionary.Count;
            m_verticesDictionary.Add(vert, newIndex);
            return newIndex;
        }

        #region Chunk Commands
        public void UpdateMaterial(Material newMat) {
            m_chunkRenderer.sharedMaterial = newMat;
            m_mat = newMat;
        }
        public float[,,] GetChunkMap() => m_noiseMap;
        public void UpdateChunk(float[,,] newMap) {
            m_noiseMap = newMap;
            Generate();
        }
        public void Update() {
            if (!ChunkMeshGenerated) return;
            var distance = Vector3.Distance(ChunkObject.transform.position, m_camera.transform.position);
            var toggle = distance <= m_lodData.viewDistance;
            ChunkObject.SetActive(toggle);
            m_chunkCollider.enabled = toggle;
        }
        #endregion

        #region Mesh Functions

        public MeshFilter GetFilter() {
            return ChunkMeshGenerated ? m_chunkFilter : null;
        }
        private void BuildMesh() {
            ChunkMeshGenerated = false;
            ChunkObject.SetActive(ChunkMeshGenerated);
            if (m_doFlatShading ? m_verticesList.Count == 0 : m_verticesDictionary.Count == 0) {
                return;
            }
            m_chunkMesh.Clear();
            if (m_doFlatShading) {
                m_chunkMesh.vertices = m_verticesList.Select(vert => new Vector3(vert.x, vert.y, vert.z)).ToArray();
            }else {
                m_chunkMesh.vertices = m_verticesDictionary.Select(vert => new Vector3(vert.Key.x, vert.Key.y, vert.Key.z)).ToArray();
            }
            m_chunkMesh.triangles = m_triangles.ToArray();
            m_chunkMesh.normals = CalculateNormals(m_doFlatShading ? m_verticesList : m_verticesDictionary);
            m_chunkCollider.sharedMesh = m_chunkMesh;
            m_chunkFilter.sharedMesh = m_chunkMesh;
            m_chunkRenderer.sharedMaterial = m_mat;
            ChunkMeshGenerated = true;
            ChunkObject.SetActive(ChunkMeshGenerated);
        }
        private void ClearMeshData() {
            if (m_doFlatShading) m_verticesList.Clear();
            else m_verticesDictionary.Clear();
            m_triangles.Clear();
        }
        private Vector3[] CalculateNormals(ICollection verts) {
            Vector3[] vertexNormals = new Vector3[verts.Count];
            int triangleCont = m_triangles.Count / 3;
            for (int i = 0; i < triangleCont; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = m_triangles[normalTriangleIndex];
                int vertexIndexB = m_triangles[normalTriangleIndex + 1];
                int vertexIndexC = m_triangles[normalTriangleIndex + 2];

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
            Vector3 pointA = m_doFlatShading ? m_verticesList[indexA] : m_verticesDictionary.ElementAt(indexA).Key;
            Vector3 pointB = m_doFlatShading ? m_verticesList[indexB] : m_verticesDictionary.ElementAt(indexB).Key;
            Vector3 pointC = m_doFlatShading ? m_verticesList[indexC] : m_verticesDictionary.ElementAt(indexC).Key;

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
        
            return Vector3.Cross(sideAb, sideAc).normalized;
        }

        #endregion
    }
}
