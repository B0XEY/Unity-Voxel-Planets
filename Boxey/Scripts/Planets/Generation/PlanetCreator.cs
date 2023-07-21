using System;
using System.Collections.Generic;
using Boxey.Scripts.Planets.Generation.Data_Classes;
using Boxey.Scripts.Planets.Static;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Boxey.Scripts.Planets.Generation {
    public class PlanetCreator : MonoBehaviour {
        private void UpdateReal() => _realSize = worldSizeInChunks * chunkSize;

        private List<Chunk> _changedChunks = new List<Chunk>();
        private List<Vector3Int> _localPositions = new List<Vector3Int>();
        private Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
        private float[,,] _noiseMap;
        private Color[] _colorMap;
        [HideInInspector] public Vector3Int chunkOffset;

        private GameObject _waterObj;
        private Material _planetMat;
        private Material _waterMat;

        [Title("World Settings", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, OnValueChanged("UpdateReal")] private int worldSizeInChunks = 4;
        [SerializeField, OnValueChanged("UpdateReal")] private int chunkSize = 16;
        [SerializeField, Range(0, 50), Tooltip("Gets added to the distance value shrink the planet but keeps map the same size")] private int additive = 25;
        [SerializeField] private int viewDistance = 150;
        [SerializeField] private int colliderDistance = 75;
        [Space] 
        [SerializeField] private float createGate = 5f;
        [SerializeField] private float valueGate = .15f;
        [SerializeField] private bool doSmoothing = true;
        [SerializeField] private bool doFlatShading = false;
        [Space]
        [OnValueChanged("UpdatePlanetLook")]
        [SerializeField, Tooltip("Color Settings. Control the Colors of the Planet"), InlineEditor] private PlanetSettings planetSettings;

        [Title("Events", titleAlignment: TitleAlignments.Centered)] 
        public UnityEvent onGenerate;

        [Title("Other", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, Required] private Transform chunkHolder;
        [SerializeField, Required] private GameObject waterSphere;
        [Space]
        [ShowInInspector, ReadOnly] private int _realSize;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _noiseGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _chunkGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _totalTime;
        

        [Button, ButtonGroup("Gen")]
        private void Create() {
            var startTime = Time.realtimeSinceStartup;
            chunkHolder.gameObject.SetActive(true);
            if (chunkHolder.childCount != 0) DestroyWorld();
            _planetMat = new Material(Shader.Find("Shader Graphs/Planet"));
            _waterMat = new Material(Shader.Find("Shader Graphs/Water"));
            CreateNoiseMap();
            CreateWorld();
            UpdatePlanetLook();
            onGenerate?.Invoke();
            _totalTime = Time.realtimeSinceStartup - startTime;
        }
        private void CreateNoiseMap() {
            var startTime = Time.realtimeSinceStartup;
            if (planetSettings.randomSeed) planetSettings.RandomSeed();
            var size = (worldSizeInChunks * chunkSize) / 2;
            if (Application.isPlaying) _noiseMap = VoxelNoise.GetPlanetNoiseMapJob(size, additive, planetSettings);
            else _noiseMap = VoxelNoise.GetPlanetNoiseMap(size, additive, planetSettings);
            _noiseGenerationTime = Time.realtimeSinceStartup - startTime;
        }
        private void CreateWorld() {
            var startTime = Time.realtimeSinceStartup;
            var radius = (worldSizeInChunks * chunkSize) / 2;
            chunkOffset = new Vector3Int(-radius, -radius, -radius);
            var position = transform.position;
            for (int x = 0; x < worldSizeInChunks; x++) {
                for (int y = 0; y < worldSizeInChunks; y++) {
                    for (int z = 0; z < worldSizeInChunks; z++) {
                        var chuckPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        var positionOffset = position + chunkOffset;
                        _chunks.Add(chuckPos, new Chunk(_noiseMap, chuckPos, positionOffset, chunkSize,
                            valueGate, createGate, doSmoothing, doFlatShading, viewDistance, colliderDistance,
                            _planetMat));
                        _chunks[chuckPos].ChunkObject.transform.SetParent(chunkHolder);
                        _chunks[chuckPos].ChunkObject.layer = 3;
                    }
                }
            }
            chunkHolder.localPosition = Vector3.zero;
            _waterObj = Instantiate(waterSphere, transform);
            _waterObj.name = "Water Sphere";
            _chunkGenerationTime = Time.realtimeSinceStartup - startTime;
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
            _waterMat.renderQueue = 3005;
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
            foreach (var chunk in _chunks) {
                chunk.Value.Update();
            }
        }
        
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
            VoxelNoise.Terraform(ref finalMap, newMapSize, addTerrain, radius, speed, terraformPoint);
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
        private void AddChunks(Vector3Int centerChunk, int updateRange) {
            _localPositions.Clear();
            _changedChunks.Clear();
            var offset = Vector3Int.one;
            for (var x = -updateRange; x <= updateRange; x++) {
                for (var y = -updateRange; y <= updateRange; y++) {
                    for (var z = -updateRange; z <= updateRange; z++) {
                        var next = centerChunk + (new Vector3Int(x, y, z) * chunkSize);
                        var local = offset + new Vector3Int(x, y, z);
                        _localPositions.Add(local);
                        if (_chunks.ContainsKey(next)) _changedChunks.Add(_chunks[next]);
                    }
                }
            }
        }
        private Vector3Int ToWorldPos(Vector3Int local, Vector3Int center) {
            var realLocal = local - Vector3Int.one;
            var worldPosition = center + (realLocal * chunkSize);
            return worldPosition;
        }
    }
}
