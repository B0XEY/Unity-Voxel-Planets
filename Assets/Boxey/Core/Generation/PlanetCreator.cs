using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boxey.Core.Components;
using Boxey.Core.Static;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityMeshSimplifier;
using Random = UnityEngine.Random;

namespace Boxey.Core {
    public enum NoiseType {
        Normal,
        Ridged,
        Billow
    }
    [Serializable]
    public class NoiseSettings {
        [Title("Settings")]
        public NoiseType type = NoiseType.Normal;
        [LabelText("Use Mask"), Tooltip("Says it this layer will se the previous layer as a mask")]public bool useLastLayerAsMask;
        public float scale = .35f;
        [Range(0.01f, 2f)] public float layerPower = 1;
        [Space(5f)]
        [HideIf("@type == NoiseType.Normal")] [Range(0.01f, 5f)] public float gain = .5f;
        [HideIf("@type == NoiseType.Normal")] [Range(0.01f, 15f)] public float strength = 1;
        [Space(5f)]
        [Range(1, 8)] public int octaves = 3;
        [Range(0.01f, 7f)] public float lacunarity = 3.66f;
        [Range(0.01f, 15f)] public float amplitude = 10f;
        [Range(0.01f, 5f)] public float frequency = .804f;
        [Range(0.01f, 3f)] public float persistence = .15f;
        [Space(5f)]
        public Vector3 offset;
    }
    [Serializable]
    public struct LodData {
        public float viewDistance;
        public bool canCollideWith;
        [Range(0,1f)]public float quality;
        [Space(10f)]
        [Range(1,100)] public int cloudLayers;
    }
    [RequireComponent(typeof(SkyManager))]
    public class PlanetCreator : MonoBehaviour {
        #region Private Varibles

        private enum GenerationMethod {
            Instantly,
            Enumerator
        }
        private void UpdateReal() => _realSize = (worldSizeInChunks * chunkSize);

        private List<Chunk> m_terraformedChunks;
        private List<Vector3Int> m_localPositions;
        private Dictionary<Vector3Int, Chunk> m_chunks;
        private float[,,] m_noiseMap;
        private float[,,] m_biomeMap;
        private int m_max = 2;
        private int m_currentLOD;
        private bool m_isCreated;
        private Camera m_cam;
        private MeshRotator m_rotator;
        private Bounds m_planetBounds;
        private Plane[] m_viewBound;
        [HideInInspector] public Vector3Int chunkOffset;

        private Mesh[] m_planetMeshes;
        private MeshFilter m_planetMeshFilter;
        private GameObject m_waterObj;
        private Material m_planetMat;
        private Material m_waterMat;

        #endregion

        [Title("Update Settings", titleAlignment: TitleAlignments.Centered)] 
        [SerializeField, SuffixLabel("sec")] private float updateTime = 5;
        
        [Title("World Settings", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, EnumToggleButtons, HideLabel] private GenerationMethod genMethod = GenerationMethod.Instantly;
        [SerializeField, OnValueChanged("UpdateReal")] private int worldSizeInChunks = 4;
        [SerializeField, OnValueChanged("UpdateReal")] private int chunkSize = 16;
        [SerializeField, Range(1, 150f)] private float planetRadius = 35;
        [SerializeField] private LodData[] planetLODS;
        [Space] 
        [SerializeField] private float createGate = 5f;
        [SerializeField] private float valueGate = .15f;
        [SerializeField] private bool doSmoothing = true;
        [SerializeField] private bool doFlatShading;
        [Space]
        [OnValueChanged("UpdatePlanetLook"), Required]
        [SerializeField, Tooltip("Color Settings. Control the Colors of the Planet"), InlineEditor] private PlanetSettings planetSettings;

        [Title("Events", titleAlignment: TitleAlignments.Centered)] 
        public UnityEvent onGenerate;

        [Title("Other", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, Required] private Transform chunkHolder;
        [SerializeField, Required] private Transform planetMeshObject;
        [SerializeField, Required] private Transform atmosphereObject;
        [SerializeField, Required] private GameObject waterSphere;
        [Space] 
        [SerializeField] private bool showChunkBounds = false;
        [SerializeField, ShowIf("@showChunkBounds")] private bool outline = true;
        [SerializeField, ShowIf("@showChunkBounds")] private Color normalColor = new(255f/255f, 235f/255f, 4f/255f, 25f/255f);
        [SerializeField, ShowIf("@showChunkBounds")] private Color terraformingColor = new(255f/255f, 4/255f, 0f/255f, 75f/255f);
        [Space]
        [OnValueChanged("UpdatePreview"), SerializeField, Required] private Gradient noiseMapColors;
        [OnValueChanged("UpdatePreview"), SerializeField, Required, PropertyRange(0, "@m_max")] private int layer = 1;
        [Space]
        [ShowInInspector, ReadOnly, SuffixLabel("units")] private int _realSize;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _noiseGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _chunkGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _totalTime;
        [ShowInInspector, ReadOnly, HideLabel, PreviewField(250, ObjectFieldAlignment.Center)] private Texture2D _noiseMapTexture;

        private void Awake() {
            m_cam = Helpers.GetCamera;
            m_rotator = GetComponent<MeshRotator>();
            m_rotator.enabled = false;
            for (int i = 0; i < planetLODS.Length; i++)
            {
                planetLODS[i].viewDistance += planetRadius;
            }
        }

        [Button, ButtonGroup("Gen")]
        private void Create() {
            var startTime = Time.realtimeSinceStartup;
            m_max = worldSizeInChunks * chunkSize - 1;
            m_isCreated = false;
            if (Application.isPlaying) m_rotator.enabled = false;
            //Set up for planet mesh Generation
            transform.rotation = Quaternion.Euler(Vector3.zero);
            chunkHolder.gameObject.SetActive(true);
            planetMeshObject.gameObject.SetActive(false);
            m_planetMeshFilter = planetMeshObject.GetComponent<MeshFilter>();
            if (chunkHolder.childCount != 0) DestroyWorld();
            //Create Needed Data
            m_planetMat = new Material(Shader.Find("Shader Graphs/Planet"));
            m_waterMat = new Material(Shader.Find("Shader Graphs/Water"));
            GetComponent<SkyManager>().SendUpdatedData(planetSettings);
            GetComponent<SkyManager>().SetCustomShaderData(transform.position, planetRadius);
            m_terraformedChunks = new List<Chunk>();
            m_localPositions = new List<Vector3Int>();
            m_chunks = new Dictionary<Vector3Int, Chunk>();
            if (m_planetMeshes == null) {
                m_planetMeshes = new Mesh[planetLODS.Length];
                for (int i = 0; i < m_planetMeshes.Length; i++) {
                    if (m_planetMeshes[i] == null) {
                        m_planetMeshes[i] = new Mesh {
                            name = "Planet Mesh - LOD: " + i + 1,
                            indexFormat = IndexFormat.UInt32
                        };
                    }
                }
            }else {
                foreach (var mesh in m_planetMeshes) {
                    mesh.Clear();
                }
            }
            CreateNoiseMaps();
            layer = m_max / 2;
            UpdatePreview();
            if (genMethod == GenerationMethod.Instantly) {
                CreateWorld();
                UpdatePlanetLook();
                if (Application.isPlaying) {
                    CreatePlanetLODS();
                    FixMeshRotation();
                    StartCoroutine(HandlePlanet());
                }
                onGenerate?.Invoke();
                m_isCreated = true;
                _totalTime = Time.realtimeSinceStartup - startTime;
            }else {
                StartCoroutine(CreateWorldDynamic());
            }
        }
        private void CreateNoiseMaps() {
            var startTime = Time.realtimeSinceStartup;
            if (planetSettings.randomSeed) planetSettings.RandomSeed();
            var size = worldSizeInChunks * chunkSize;
            if (Application.isPlaying) m_noiseMap = VoxelNoise.GetPlanetNoiseMapJob(size, planetRadius + 15, planetSettings);
            else m_noiseMap = VoxelNoise.GetPlanetNoiseMap(size, planetRadius + 15, planetSettings);
            _noiseGenerationTime = Time.realtimeSinceStartup - startTime;
        }
        private IEnumerator CreateWorldDynamic() {
            var startTime = Time.realtimeSinceStartup;
            var position = transform.position;
            var size = worldSizeInChunks;
            var halfSize = (worldSizeInChunks * chunkSize) / 2;
            var lod = planetLODS[0];
            lod.viewDistance -= planetRadius / 1.75f;
            chunkOffset = new Vector3Int(-halfSize, -halfSize, -halfSize);
            m_planetBounds = new Bounds(position, (chunkOffset.ToFloat3() * -2f));
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    for (int z = 0; z < size; z++) {
                        var chuckPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        var positionOffset = position + chunkOffset;
                        m_chunks.Add(chuckPos, new Chunk(m_noiseMap, chuckPos, positionOffset, chunkSize,
                            valueGate, createGate, doSmoothing, doFlatShading, lod, m_planetMat));
                        m_chunks[chuckPos].ChunkObject.transform.SetParent(chunkHolder);
                        m_chunks[chuckPos].ChunkObject.layer = 3;
                    }
                    float time = GetTimeBetweenChunkGeneration();
                    time = Mathf.Clamp(time - 1f, 0, 100f);
                    yield return new WaitForSeconds(time);
                }
            }
            chunkHolder.localPosition = Vector3.zero;
            m_waterObj = Instantiate(waterSphere, transform);
            m_waterObj.name = "Water Sphere";
            UpdatePlanetLook();
            onGenerate?.Invoke();
            if (Application.isPlaying) {
                CreatePlanetLODS();
                FixMeshRotation();
                StartCoroutine(HandlePlanet());
            }
            m_isCreated = true;
            _chunkGenerationTime = Time.realtimeSinceStartup - startTime;
            _totalTime = _chunkGenerationTime + _noiseGenerationTime;
        }
        private void CreateWorld() {
            var startTime = Time.realtimeSinceStartup;
            var size = worldSizeInChunks;
            var halfSize = (worldSizeInChunks * chunkSize) / 2;
            var position = transform.position;
            chunkOffset = new Vector3Int(-halfSize, -halfSize, -halfSize);
            m_planetBounds = new Bounds(position, chunkOffset.ToFloat3() * -2f);
            var lod = planetLODS[0];
            lod.viewDistance -= planetRadius / 1.75f;
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    for (int z = 0; z < size; z++) {
                        var chuckPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        var positionOffset = position + chunkOffset;
                        m_chunks.Add(chuckPos, new Chunk(m_noiseMap, chuckPos, positionOffset, chunkSize,
                            valueGate, createGate, doSmoothing, doFlatShading, lod, m_planetMat));
                        m_chunks[chuckPos].ChunkObject.transform.SetParent(chunkHolder);
                        m_chunks[chuckPos].ChunkObject.layer = 3;
                    }
                }
            }
            chunkHolder.localPosition = Vector3.zero;
            m_waterObj = Instantiate(waterSphere, transform);
            m_waterObj.name = "Water Sphere";
            _chunkGenerationTime = Time.realtimeSinceStartup - startTime;
            _totalTime = _chunkGenerationTime + _noiseGenerationTime;
        }
        
        
        private void UpdatePreview() {
            _noiseMapTexture = new Texture2D(m_noiseMap.GetLength(0), m_noiseMap.GetLength(1));
            _noiseMapTexture.SetPixels(VoxelNoise.GetLayerColors3DPlanet(m_noiseMap, layer, noiseMapColors, out _, out _, true));
            _noiseMapTexture.Apply();
        }
        [Button("Update Look"), ButtonGroup("Function")]
        private void UpdatePlanetLook() {
            var radius = (worldSizeInChunks * chunkSize) / 2;
            chunkOffset = new Vector3Int(-radius, -radius, -radius);
            var center = transform.position;
            m_planetMat = new Material(Shader.Find("Shader Graphs/Planet"));
            m_waterMat = new Material(Shader.Find("Shader Graphs/Water"));
            GetComponent<SkyManager>().SendUpdatedData(planetSettings);
            GetComponent<SkyManager>().SetCustomShaderData(transform.position, planetRadius);

            m_planetMat.SetVector("_center", new Vector4(center.x, center.y, center.z));
            //Planet Float Values
            m_planetMat.SetFloat("_Grass_Warping", planetSettings.groundHighlightsStrength);
            m_planetMat.SetFloat("_Sand_Warping", planetSettings.sandHighlightsStrength);
            m_planetMat.SetFloat("_Sand_Height", planetSettings.sandHeight);
            m_planetMat.SetFloat("_Steepness_Warping", planetSettings.rockHighlightsStrength);
            m_planetMat.SetFloat("_Steepness_Threshold", planetSettings.rockThreshold);
            //Planet Colors
            m_planetMat.SetColor("_Grass", planetSettings.ground);
            m_planetMat.SetColor("_Dark_Grass", planetSettings.groundHighlight);
            m_planetMat.SetColor("_Sand", planetSettings.sand);
            m_planetMat.SetColor("_Dark_Sand", planetSettings.sandHighlights);
            m_planetMat.SetColor("_Rock", planetSettings.rock);
            m_planetMat.SetColor("_Dark_Rock", planetSettings.rockHighlights);
            //Water Float Values
            m_waterMat.SetFloat("_Depth", planetSettings.deepFadeDistance);
            m_waterMat.SetFloat("_Strength", planetSettings.depthStrength);
            m_waterMat.SetFloat("_Amount", planetSettings.foamAmount);
            m_waterMat.SetFloat("_Foam_Strength", planetSettings.foamStrength);
            m_waterMat.SetFloat("_Cutoff", planetSettings.foamCutoff);
            m_waterMat.SetFloat("_Speed", planetSettings.foamSpeed);
            //Water Colors
            m_waterMat.SetColor("_Shallow", planetSettings.shallowColor);
            m_waterMat.SetColor("_Deep", planetSettings.deepColor);
            m_waterMat.SetColor("_Foam_Color", planetSettings.foamColor);
            foreach (var c in m_chunks) {
                c.Value.UpdateMaterial(m_planetMat);
            }
            var scale = planetSettings.sandHeight - Random.Range(1f, 2f);
            scale = scale.Clamp(0, 1500);
            m_waterObj.transform.localScale = new Vector3(scale, scale, scale);
            m_waterObj.GetComponent<MeshRenderer>().sharedMaterial = m_waterMat;
            m_waterObj.SetActive(scale != 0);
        }
        [Button("Destroy"), ButtonGroup("Function")]
        private void DestroyWorld() {
            m_chunks?.Clear();
            foreach (var mesh in m_planetMeshes) {
                mesh.Clear();
            }
            if (m_waterObj != null) {
                if (Application.isPlaying) Destroy(m_waterObj);
                else DestroyImmediate(m_waterObj);
            }
            var children = new List<GameObject>();
            foreach (Transform child in chunkHolder) children.Add(child.gameObject);
            if (Application.isPlaying) children.ForEach(Destroy);
            else children.ForEach(DestroyImmediate);
        }

        //Rendering
        private void Update() {
            if (Input.GetKeyUp(KeyCode.Space)) Create();
        }
        private void FixedUpdate() {
            if (!m_isCreated || !CameraCanSee()) return;
            var distance = Vector3.Distance(m_cam.transform.position, transform.position);
            m_waterMat.renderQueue = distance <= planetLODS[0].viewDistance ? 3001 : 2999;
            if (distance > planetLODS[0].viewDistance) {
                if (chunkHolder.gameObject.activeSelf) chunkHolder.gameObject.SetActive(false);
                if (!planetMeshObject.gameObject.activeSelf) planetMeshObject.gameObject.SetActive(true);
                //Get Current LOD Index
                var currentLODIndex = planetLODS.Length - 1;
                for (var i = 0; i < planetLODS.Length; i++) {
                    if (distance <= planetLODS[i].viewDistance) {
                        currentLODIndex = i;
                        break;
                    }
                }
                //Update the big mesh
                UpdateMeshLOD(currentLODIndex);
                m_currentLOD = currentLODIndex;
            }
            else {
                //Normal Chunk Update
                if (!chunkHolder.gameObject.activeSelf) chunkHolder.gameObject.SetActive(true);
                if (planetMeshObject.gameObject.activeSelf) planetMeshObject.gameObject.SetActive(false);
                foreach (var chunk in m_chunks) {
                    chunk.Value.Update();
                }
            }
        }

        //Says if planet is shown within camera bounds
        private bool CameraCanSee() {
            m_viewBound = GeometryUtility.CalculateFrustumPlanes(m_cam);
            return GeometryUtility.TestPlanesAABB(m_viewBound, m_planetBounds);
        }
        //Calls every x seconds maybe better performance
        private IEnumerator HandlePlanet() {
            while (true) {
                var visible = CameraCanSee();
                if (chunkHolder.gameObject.activeSelf != visible) chunkHolder.gameObject.SetActive(visible);
                if (planetMeshObject.gameObject.activeSelf != visible) planetMeshObject.gameObject.SetActive(visible);
                if (atmosphereObject.gameObject.activeSelf != visible) atmosphereObject.gameObject.SetActive(visible);
                if (m_waterObj.gameObject.activeSelf != visible) m_waterObj.gameObject.SetActive(visible);
                yield return Helpers.GetWaitTime(updateTime);
            }
        }
        private void UpdateMeshLOD(int target) {
            if (target == m_currentLOD) return;
            m_planetMeshFilter.sharedMesh = m_planetMeshes[target];
            m_currentLOD = target;
        }

        public LodData GetCurrentLOD() => planetLODS[m_currentLOD];

        #region LOD Mesh Creation

        private void CreatePlanetLODS() {
            foreach (var mesh in m_planetMeshes) {
                mesh.Clear();
            }
            var filters = m_chunks.Select(chunk => chunk.Value.GetFilter()).Where(filter => filter != null).ToList();
            m_planetMeshes[0].Clear();
            m_planetMeshes[0].indexFormat = IndexFormat.UInt32;
            var combiners = new CombineInstance[filters.Count];
            for (int i = 0; i < filters.Count; i++) {
                combiners[i].subMeshIndex = 0;
                combiners[i].mesh = filters[i].sharedMesh;
                combiners[i].transform = filters[i].transform.localToWorldMatrix;
            }
            m_planetMeshes[0].CombineMeshes(combiners);
            //Create All Planet LODS
            for (int i = 0; i < m_planetMeshes.Length; i++)   {
                var simplifier = new MeshSimplifier(m_planetMeshes[0]);
                simplifier.SimplifyMesh(planetLODS[i].quality);
                m_planetMeshes[i] = simplifier.ToMesh();
                m_planetMeshes[i].name = "Planet Mesh - LOD: " + (i + 1);
            }
            planetMeshObject.transform.position = Vector3.zero;
            planetMeshObject.GetComponent<MeshRenderer>().material = m_planetMat;
            m_planetMeshFilter.sharedMesh = m_planetMeshes[m_currentLOD];
        }
        private void RebuildLODMesh() {
            //combine meshes int one big mesh
            var filters = m_chunks.Select(chunk => chunk.Value.GetFilter()).Where(filter => filter != null).ToList();
            m_planetMeshes[0].Clear();
            m_planetMeshes[1].Clear();
            m_planetMeshes[0].indexFormat = IndexFormat.UInt32;
            m_planetMeshes[1].indexFormat = IndexFormat.UInt32;
            var combiners = new CombineInstance[filters.Count];
            for (int i = 0; i < filters.Count; i++) {
                combiners[i].subMeshIndex = 0;
                combiners[i].mesh = filters[i].sharedMesh;
                combiners[i].transform = filters[i].transform.localToWorldMatrix;
            }
            //Update Meshes
            m_planetMeshes[0].CombineMeshes(combiners);
            m_planetMeshes[1].CombineMeshes(combiners);
            m_planetMeshes[0].name = "Planet Mesh - LOD: " + 1;
            m_planetMeshes[1].name = "Planet Mesh - LOD: " + 2;
            
            planetMeshObject.transform.position = Vector3.zero;
            planetMeshObject.GetComponent<MeshRenderer>().material = m_planetMat;
            m_planetMeshFilter.sharedMesh = m_planetMeshes[0];
        }
        // ReSharper disable Unity.InefficientPropertyAccess
        private void FixMeshRotation() {
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
            planetMeshObject.eulerAngles /= 99;
        }

        #endregion
        #region TERRAFORMING FUNCTIONS

        public void Terrafrom(Vector3Int centerChunk, float3 pointWorldPos, int updateRange, float radius, float speed, bool addTerrain) {
            AddChunks(centerChunk, updateRange);
            var terraformPoint = pointWorldPos - centerChunk.ToFloat3();
            terraformPoint += new float3(chunkSize);
            var newMapSize = chunkSize * (updateRange * 2 + 1);
            var finalMap = new float[newMapSize + 1, newMapSize + 1, newMapSize + 1];
            var map = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];
            foreach (var position in m_localPositions) {
                var chunkID = ToWorldPos(position, centerChunk);
                if (m_chunks.ContainsKey(chunkID))
                {
                    map = m_chunks[chunkID].GetChunkMap();
                    m_chunks[chunkID].TerraformingChunk = true;
                }
                var offset = position * chunkSize;
                for (var x = 0; x <= chunkSize; x++) {
                    for (var y = 0; y <= chunkSize; y++) {
                        for (var z = 0; z <= chunkSize; z++) {
                            finalMap[x + offset.x, y + offset.y, z + offset.z] = map[x, y, z];
                        }
                    }
                }
            }
            VoxelNoise.Terraform(ref finalMap, newMapSize, addTerrain, radius, speed / planetSettings.groundToughness, terraformPoint);
            foreach (var position in m_localPositions) {
                var chunkID = ToWorldPos(position, centerChunk);
                if (!m_chunks.TryGetValue(chunkID, out var chunk)) continue; 
                map = chunk.GetChunkMap();
                var offset = position * chunkSize;
                for (var x = 0; x <= chunkSize; x++) {
                    for (var y = 0; y <= chunkSize; y++) {
                        for (var z = 0; z <= chunkSize; z++) {
                            map[x, y, z] = finalMap[x +offset.x, y +offset.y, z +offset.z];
                        }
                    }
                }
                chunk.UpdateChunk(map);
            }
        }
        public void FinishTerraform() {
            RebuildLODMesh();
            FixMeshRotation();
            foreach (var chunk in m_chunks) {
                if (chunk.Value.TerraformingChunk) chunk.Value.TerraformingChunk = false;
            }
        }
        private void AddChunks(Vector3Int centerChunk, int updateRange) {
            m_localPositions.Clear();
            m_terraformedChunks.Clear();
            var offset = Vector3Int.one;
            for (var x = -updateRange; x <= updateRange; x++) {
                for (var y = -updateRange; y <= updateRange; y++) {
                    for (var z = -updateRange; z <= updateRange; z++) {
                        var next = centerChunk + (new Vector3Int(x, y, z) * chunkSize);
                        var local = offset + new Vector3Int(x, y, z);
                        m_localPositions.Add(local);
                        if (m_chunks.ContainsKey(next)) m_terraformedChunks.Add(m_chunks[next]);
                    }
                }
            }
        }
        private Vector3Int ToWorldPos(Vector3Int local, Vector3Int center) {
            var realLocal = local - Vector3Int.one;
            var worldPosition = center + (realLocal * chunkSize);
            return worldPosition;
        }

        #endregion

        private float GetTimeBetweenChunkGeneration() {
            float mapSize = worldSizeInChunks * chunkSize;
            float fps = 1f / Time.deltaTime;
            float timeBetweenChunkGeneration = (mapSize / fps) / Time.frameCount;
            return timeBetweenChunkGeneration;
        }
        private void OnDrawGizmos() {
            Gizmos.DrawWireCube(m_planetBounds.center, m_planetBounds.size);
            if (m_chunks == null || !showChunkBounds) return;
            Vector3 halfSize = new float3(chunkSize / 2f);
            foreach (var chunk in m_chunks) {
                if (!chunk.Value.ChunkMeshGenerated) continue;
                Gizmos.color = chunk.Value.TerraformingChunk ? terraformingColor : normalColor;
                var position = (chunk.Value.ChunkObject.transform.position + halfSize);
                if (outline) Gizmos.DrawWireCube(position, new float3(chunkSize));
                else Gizmos.DrawCube(position, new float3(chunkSize));
            }
        }
        private void OnValidate() {
            if (planetLODS.Length == 0) {
                planetLODS = new LodData[1];
                planetLODS[0] = new LodData {
                    viewDistance = 100,
                    canCollideWith = true,
                    quality = 1
                };
            }
            for (int i = 0; i < planetLODS.Length; i++) {
                if (i is 0 or 1) {
                    if (i == 0) planetLODS[i].canCollideWith = true;
                    planetLODS[i].quality = 1;
                }else if (planetLODS[i].quality == 0) {
                    planetLODS[i].quality = Mathf.Clamp(planetLODS[i - 1].quality - 0.1f, 0.001f, 1);
                }
                if (i > 0 && planetLODS[i].viewDistance < planetLODS[i - 1].viewDistance) {
                    planetLODS[i].viewDistance = planetLODS[i - 1].viewDistance + 50f;
                }
                if (i > 0 && planetLODS[i].cloudLayers > planetLODS[i - 1].cloudLayers) {
                    planetLODS[i].cloudLayers = Mathf.Clamp(planetLODS[i - 1].cloudLayers - 1, 1, 100);
                }
            }
            GetComponent<SkyManager>().SendUpdatedData(planetSettings);
        }
    }
}
