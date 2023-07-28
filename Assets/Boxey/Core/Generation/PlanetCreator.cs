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
    [RequireComponent(typeof(CloudController))]
    public class PlanetCreator : MonoBehaviour {
        #region Private Varibles

        private enum GenerationMethod {
            Instantly,
            Enumerator
        }
        private void UpdateReal() => _realSize = (worldSizeInChunks * chunkSize);

        private List<Chunk> _terraformedChunks;
        private List<Vector3Int> _localPositions;
        private Dictionary<Vector3Int, Chunk> _chunks;
        private float[,,] _noiseMap;
        private float[,,] _biomeMap;
        private int _max = 2;
        private int _currentLOD = 0;
        private bool _isCreated;
        private Camera _cam;
        private MeshRotator _rotator;
        private Bounds _planetBounds;
        private Plane[] _viewBound;
        [HideInInspector] public Vector3Int chunkOffset;

        private Mesh[] _planetMeshes;
        private MeshFilter _planetMeshFilter;
        private GameObject _waterObj;
        private Material _planetMat;
        private Material _waterMat;

        #endregion

        [Title("Update Settings", titleAlignment: TitleAlignments.Centered)] 
        [SerializeField, SuffixLabel("sec")] private float updateTime = 5;
        
        [Title("World Settings", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, EnumToggleButtons, HideLabel] private GenerationMethod genMethod = GenerationMethod.Instantly;
        [SerializeField, OnValueChanged("UpdateReal")] private int worldSizeInChunks = 4;
        [SerializeField, OnValueChanged("UpdateReal")] private int chunkSize = 16;
        [SerializeField, Range(1, 75f)] private float planetRadius = 35;
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
        [OnValueChanged("UpdatePreview"), SerializeField, Required] private Gradient noiseMapColors;
        [OnValueChanged("UpdatePreview"), SerializeField, Required, PropertyRange(0, "@_max")] private int layer = 1;
        [Space]
        [ShowInInspector, ReadOnly] private int _realSize;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _noiseGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _chunkGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _totalTime;
        [ShowInInspector, ReadOnly, HideLabel, PreviewField(250, ObjectFieldAlignment.Center)] private Texture2D _noiseMapTexture;

        private void Awake() {
            _cam = Helpers.GetCamera;
            _rotator = GetComponent<MeshRotator>();
            _rotator.enabled = false;
        }

        [Button, ButtonGroup("Gen")]
        private void Create() {
            var startTime = Time.realtimeSinceStartup;
            _max = worldSizeInChunks * chunkSize - 1;
            _isCreated = false;
            if (Application.isPlaying) _rotator.enabled = false;
            //Set up for planet mesh Generation
            transform.rotation = Quaternion.Euler(Vector3.zero);
            chunkHolder.gameObject.SetActive(true);
            planetMeshObject.gameObject.SetActive(false);
            _planetMeshFilter = planetMeshObject.GetComponent<MeshFilter>();
            if (chunkHolder.childCount != 0) DestroyWorld();
            //Create Needed Data
            _planetMat = new Material(Shader.Find("Shader Graphs/Planet"));
            _waterMat = new Material(Shader.Find("Shader Graphs/Water"));
            GetComponent<CloudController>().SetCustomShaderData(transform.position, planetRadius);
            _terraformedChunks = new List<Chunk>();
            _localPositions = new List<Vector3Int>();
            _chunks = new Dictionary<Vector3Int, Chunk>();
            if (_planetMeshes == null) {
                _planetMeshes = new Mesh[planetLODS.Length];
                for (int i = 0; i < _planetMeshes.Length; i++) {
                    if (_planetMeshes[i] == null) {
                        _planetMeshes[i] = new Mesh {
                            name = "Planet Mesh - LOD: " + i + 1,
                            indexFormat = IndexFormat.UInt32
                        };
                    }
                }
            }else {
                foreach (var mesh in _planetMeshes) {
                    mesh.Clear();
                }
            }
            CreateNoiseMaps();
            layer = _max / 2;
            UpdatePreview();
            if (genMethod == GenerationMethod.Instantly) {
                CreateWorld();
                UpdatePlanetLook();
                CreatePlanetLODS();
                FixMeshRotation();
                onGenerate?.Invoke();
                _isCreated = true;
                _totalTime = Time.realtimeSinceStartup - startTime;
            }else {
                StartCoroutine(CreateWorldDynamic());
            }
        }
        private void CreateNoiseMaps() {
            var startTime = Time.realtimeSinceStartup;
            if (planetSettings.randomSeed) planetSettings.RandomSeed();
            var size = worldSizeInChunks * chunkSize;
            if (Application.isPlaying) _noiseMap = VoxelNoise.GetPlanetNoiseMapJob(size, planetRadius + 15, planetSettings);
            else _noiseMap = VoxelNoise.GetPlanetNoiseMap(size, planetRadius + 15, planetSettings);
            _noiseGenerationTime = Time.realtimeSinceStartup - startTime;
        }
        private IEnumerator CreateWorldDynamic() {
            var startTime = Time.realtimeSinceStartup;
            var position = transform.position;
            var size = worldSizeInChunks;
            var halfSize = (worldSizeInChunks * chunkSize) / 2;
            
            chunkOffset = new Vector3Int(-halfSize, -halfSize, -halfSize);
            _planetBounds = new Bounds(position, (chunkOffset.ToFloat3() * -1.5f));
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    for (int z = 0; z < size; z++) {
                        var chuckPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        var positionOffset = position + chunkOffset;
                        _chunks.Add(chuckPos, new Chunk(_noiseMap, chuckPos, positionOffset, chunkSize,
                            valueGate, createGate, doSmoothing, doFlatShading, planetLODS[0], _planetMat));
                        _chunks[chuckPos].ChunkObject.transform.SetParent(chunkHolder);
                        _chunks[chuckPos].ChunkObject.layer = 3;
                    }
                    float time = GetTimeBetweenChunkGeneration();
                    time = Mathf.Clamp(time - 1f, 0, 100f);
                    yield return new WaitForSeconds(time);
                }
            }
            chunkHolder.localPosition = Vector3.zero;
            _waterObj = Instantiate(waterSphere, transform);
            _waterObj.name = "Water Sphere";
            UpdatePlanetLook();
            onGenerate?.Invoke();
            CreatePlanetLODS();
            FixMeshRotation();
            if (Application.isPlaying) StartCoroutine(HandlePlanet());
            _isCreated = true;
            _chunkGenerationTime = Time.realtimeSinceStartup - startTime;
            _noiseMap = null;
            _totalTime = _chunkGenerationTime + _noiseGenerationTime;
        }
        private void CreateWorld() {
            var startTime = Time.realtimeSinceStartup;
            var size = worldSizeInChunks;
            var halfSize = (worldSizeInChunks * chunkSize) / 2;
            var position = transform.position;
            chunkOffset = new Vector3Int(-halfSize, -halfSize, -halfSize);
            _planetBounds = new Bounds(position, chunkOffset.ToFloat3() * -1.5f);
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    for (int z = 0; z < size; z++) {
                        var chuckPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        var positionOffset = position + chunkOffset;
                        _chunks.Add(chuckPos, new Chunk(_noiseMap, chuckPos, positionOffset, chunkSize,
                            valueGate, createGate, doSmoothing, doFlatShading, planetLODS[0], _planetMat));
                        _chunks[chuckPos].ChunkObject.transform.SetParent(chunkHolder);
                        _chunks[chuckPos].ChunkObject.layer = 3;
                    }
                }
            }
            chunkHolder.localPosition = Vector3.zero;
            _waterObj = Instantiate(waterSphere, transform);
            _waterObj.name = "Water Sphere";
            if (Application.isPlaying) StartCoroutine(HandlePlanet());
            _chunkGenerationTime = Time.realtimeSinceStartup - startTime;
            _totalTime = _chunkGenerationTime + _noiseGenerationTime;
        }
        
        
        private void UpdatePreview() {
            _noiseMapTexture = new Texture2D(_noiseMap.GetLength(0), _noiseMap.GetLength(1));
            _noiseMapTexture.SetPixels(VoxelNoise.GetLayerColors3DPlanet(_noiseMap, layer, noiseMapColors, out _, out _, true));
            _noiseMapTexture.Apply();
        }
        [Button("Update Look"), ButtonGroup("Function")]
        private void UpdatePlanetLook() {
            var radius = (worldSizeInChunks * chunkSize) / 2;
            chunkOffset = new Vector3Int(-radius, -radius, -radius);
            var center = transform.position;
            _planetMat = new Material(Shader.Find("Shader Graphs/Planet"));
            _waterMat = new Material(Shader.Find("Shader Graphs/Water"));

            _planetMat.SetVector("_center", new Vector4(center.x, center.y, center.z));
            //Planet Float Values
            _planetMat.SetFloat("_Grass_Warping", planetSettings.groundHighlightsStrength);
            _planetMat.SetFloat("_Sand_Warping", planetSettings.sandHighlightsStrength);
            _planetMat.SetFloat("_Sand_Height", planetSettings.sandHeight);
            _planetMat.SetFloat("_Steepness_Warping", planetSettings.rockHighlightsStrength);
            _planetMat.SetFloat("_Steepness_Threshold", planetSettings.rockThreshold);
            //Planet Colors
            _planetMat.SetColor("_Grass", planetSettings.ground);
            _planetMat.SetColor("_Dark_Grass", planetSettings.groundHighlight);
            _planetMat.SetColor("_Sand", planetSettings.sand);
            _planetMat.SetColor("_Dark_Sand", planetSettings.sandHighlights);
            _planetMat.SetColor("_Rock", planetSettings.rock);
            _planetMat.SetColor("_Dark_Rock", planetSettings.rockHighlights);
            //Water Float Values
            _waterMat.SetFloat("_Depth", planetSettings.deepFadeDistance);
            _waterMat.SetFloat("_Strength", planetSettings.depthStrength);
            _waterMat.SetFloat("_Amount", planetSettings.foamAmount);
            _waterMat.SetFloat("_Foam_Strength", planetSettings.foamStrength);
            _waterMat.SetFloat("_Cutoff", planetSettings.foamCutoff);
            _waterMat.SetFloat("_Speed", planetSettings.foamSpeed);
            //Water Colors
            _waterMat.SetColor("_Shallow", planetSettings.shallowColor);
            _waterMat.SetColor("_Deep", planetSettings.deepColor);
            _waterMat.SetColor("_Foam_Color", planetSettings.foamColor);
            foreach (var c in _chunks) {
                c.Value.UpdateMaterial(_planetMat);
            }
            var scale = planetSettings.sandHeight - Random.Range(1f, 2f);
            scale = scale.Clamp(0, 1500);
            _waterObj.transform.localScale = new Vector3(scale, scale, scale);
            _waterObj.GetComponent<MeshRenderer>().sharedMaterial = _waterMat;
            _waterObj.SetActive(scale != 0);
        }
        [Button("Destroy"), ButtonGroup("Function")]
        private void DestroyWorld() {
            _chunks.Clear();
            foreach (var mesh in _planetMeshes) {
                mesh.Clear();
            }
            if (_waterObj != null) {
                if (Application.isPlaying) Destroy(_waterObj);
                else DestroyImmediate(_waterObj);
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
            if (!_isCreated || !CameraCanSee()) return;
            var distance = Vector3.Distance(_cam.transform.position, transform.position);
            _waterMat.renderQueue = distance <= planetLODS[0].viewDistance ? 3001 : 2999;
            if (distance > planetLODS[0].viewDistance + 5f) {
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
                _currentLOD = currentLODIndex;
            }
            else {
                //Normal Chunk Update
                if (planetMeshObject.gameObject.activeSelf) planetMeshObject.gameObject.SetActive(false);
                foreach (var chunk in _chunks) {
                    chunk.Value.Update();
                }
            }
        }

        //Says if planet is shown within camera bounds
        private bool CameraCanSee() {
            _viewBound = GeometryUtility.CalculateFrustumPlanes(_cam);
            return GeometryUtility.TestPlanesAABB(_viewBound, _planetBounds);
        }
        //Calls every x seconds maybe better performance
        private IEnumerator HandlePlanet() {
            var visible = CameraCanSee();
            if (chunkHolder.gameObject.activeSelf != visible) chunkHolder.gameObject.SetActive(visible);
            if (planetMeshObject.gameObject.activeSelf != visible) planetMeshObject.gameObject.SetActive(visible);
            if (atmosphereObject.gameObject.activeSelf != visible) atmosphereObject.gameObject.SetActive(visible);
            if (_waterObj.gameObject.activeSelf != visible) _waterObj.gameObject.SetActive(visible);
            yield return new WaitForSeconds(updateTime);
        }
        private void UpdateMeshLOD(int target) {
            if (target == _currentLOD) return;
            _planetMeshFilter.sharedMesh = _planetMeshes[target];
            _currentLOD = target;
        }

        public LodData GetCurrentLOD() => planetLODS[_currentLOD];

        #region LOD Mesh Creation

        private void CreatePlanetLODS() {
            foreach (var mesh in _planetMeshes) {
                mesh.Clear();
            }
            var filters = _chunks.Select(chunk => chunk.Value.GetFilter()).Where(filter => filter != null).ToList();
            _planetMeshes[0].Clear();
            _planetMeshes[0].indexFormat = IndexFormat.UInt32;
            var combiners = new CombineInstance[filters.Count];
            for (int i = 0; i < filters.Count; i++) {
                combiners[i].subMeshIndex = 0;
                combiners[i].mesh = filters[i].sharedMesh;
                combiners[i].transform = filters[i].transform.localToWorldMatrix;
            }
            _planetMeshes[0].CombineMeshes(combiners);
            //Create All Planet LODS
            for (int i = 0; i < _planetMeshes.Length; i++)   {
                var simplifier = new MeshSimplifier(_planetMeshes[0]);
                simplifier.SimplifyMesh(planetLODS[i].quality);
                _planetMeshes[i] = simplifier.ToMesh();
                _planetMeshes[i].name = "Planet Mesh - LOD: " + (i + 1);
            }
            planetMeshObject.transform.position = Vector3.zero;
            planetMeshObject.GetComponent<MeshRenderer>().material = _planetMat;
            _planetMeshFilter.sharedMesh = _planetMeshes[_currentLOD];
        }
        private void RebuildLODMesh() {
            //combine meshes int one big mesh
            var filters = _chunks.Select(chunk => chunk.Value.GetFilter()).Where(filter => filter != null).ToList();
            _planetMeshes[0].Clear();
            _planetMeshes[1].Clear();
            _planetMeshes[0].indexFormat = IndexFormat.UInt32;
            _planetMeshes[1].indexFormat = IndexFormat.UInt32;
            var combiners = new CombineInstance[filters.Count];
            for (int i = 0; i < filters.Count; i++) {
                combiners[i].subMeshIndex = 0;
                combiners[i].mesh = filters[i].sharedMesh;
                combiners[i].transform = filters[i].transform.localToWorldMatrix;
            }
            //Update Meshes
            _planetMeshes[0].CombineMeshes(combiners);
            _planetMeshes[1].CombineMeshes(combiners);
            _planetMeshes[0].name = "Planet Mesh - LOD: " + 1;
            _planetMeshes[1].name = "Planet Mesh - LOD: " + 2;
            
            planetMeshObject.transform.position = Vector3.zero;
            planetMeshObject.GetComponent<MeshRenderer>().material = _planetMat;
            _planetMeshFilter.sharedMesh = _planetMeshes[0];
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
            foreach (var position in _localPositions) {
                var chunkID = ToWorldPos(position, centerChunk);
                if (_chunks.ContainsKey(chunkID)) map = _chunks[chunkID].GetChunkMap();
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
            foreach (var position in _localPositions) {
                var chunkID = ToWorldPos(position, centerChunk);
                if (!_chunks.TryGetValue(chunkID, out var chunk)) continue; 
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
        }
        private void AddChunks(Vector3Int centerChunk, int updateRange) {
            _localPositions.Clear();
            _terraformedChunks.Clear();
            var offset = Vector3Int.one;
            for (var x = -updateRange; x <= updateRange; x++) {
                for (var y = -updateRange; y <= updateRange; y++) {
                    for (var z = -updateRange; z <= updateRange; z++) {
                        var next = centerChunk + (new Vector3Int(x, y, z) * chunkSize);
                        var local = offset + new Vector3Int(x, y, z);
                        _localPositions.Add(local);
                        if (_chunks.ContainsKey(next)) _terraformedChunks.Add(_chunks[next]);
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
            Gizmos.DrawWireCube(_planetBounds.center, _planetBounds.size);
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
            GetComponent<CloudController>().SendUpdatedData(planetSettings);
        }
    }
}
