using System;
using System.Collections.Generic;
using System.Linq;
using Boxey.Planets.Generation.Data_Classes;
using Boxey.Planets.Static;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using static System.Int32;
using Random = UnityEngine.Random;

namespace Boxey.Planets.Generation {
    public class WorldManager : MonoBehaviour {
        private void UpdateReal() => _realSize = worldSizeInChunks * chunkSize;
        private void RandomSeed() => seed = Random.Range(-999999, 999999);
        private void RandomSeedToggle() => _randomSeed = !_randomSeed;

        private List<Chunk> _changedChunks = new List<Chunk>();
        private List<Vector3Int> _localPositions = new List<Vector3Int>();
        private Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
        private float _maxLayer = 2;
        private float[,,] _noiseMap;
        private Color[] _colorMap;
        private Vector3Int _chunkOffset;

        [Title("World Settings", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, OnValueChanged("UpdateReal")] private int worldSizeInChunks = 4;
        [SerializeField, OnValueChanged("UpdateReal")] private int chunkSize = 16;
        [SerializeField, Range(0, 50), Tooltip("Gets added to the distance value shrink the planet but keeps map the same size")] private int additive = 25;
        [ShowInInspector, ReadOnly] private int _realSize;
        [Space] 
        [SerializeField] private bool doSmoothing = true;
        [SerializeField] private bool doFlatShading = false;
        [SerializeField] private float createGate = 5f;
        [SerializeField] private float valueGate = .15f;
        [Space] 
        [SerializeField] private int viewDistance = 150;
        [SerializeField] private int colliderDistance = 75;
        [Space] 
        [SerializeField, Required] private Material planetMaterial;
        [SerializeField, Required] private Transform chunkHolder;

        [Title("Noise Values", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, InlineButton("RandomSeed", " R"), InlineButton("RandomSeedToggle", "T")] private int seed;
        [SerializeField, Tooltip("Noise Settings. Control the values of the noise"), InlineEditor] private NoiseSettings noiseSettings;
        [SerializeField, Tooltip("Color Settings. Control the Colors of the Planet"), InlineEditor] private ColorSettings colorSettings;
        
        [Title("Terraforming", titleAlignment: TitleAlignments.Centered)] 
        [SerializeField] private bool doTerraform = true;
        [ShowIf("@doTerraform")]
        [SerializeField] private Transform point;
        [ShowIf("@doTerraform")]
        [SerializeField, Range(1,5)] private int updateRange = 1;
        [ShowIf("@doTerraform")]
        [SerializeField, PropertyRange(.25f, "@chunkSize - 2.5f")] private float brushRadius = 2.5f;
        [ShowIf("@doTerraform")]
        [SerializeField,Range(0.01f, 15f)] private float brushSpeed = 5;


        [Title("Display", titleAlignment: TitleAlignments.Centered)]
        [OnValueChanged("UpdateVisuals")]
        [Tooltip("White = 0, Black = 1"), SerializeField] private Gradient displayColors;
        [OnValueChanged("UpdateVisuals")] [SerializeField, PropertyRange(0, "@_maxLayer")] private int layerToView;
        [Space(7.5f)] 
        [ShowInInspector, ReadOnly] private bool _randomSeed = true;
        [Space(5f)] 
        [ShowInInspector, ReadOnly, SuffixLabel("ms")] private float _visualTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _noiseGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _chunkGenerationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _totalTime;
        [ShowInInspector, ReadOnly, HideLabel, PreviewField(250, ObjectFieldAlignment.Center)] private Texture2D _output;

        [Title("Events", titleAlignment: TitleAlignments.Centered)] 
        public UnityEvent onGenerate;

        [Button, ButtonGroup("Gen")]
        private void Create() {
            var startTime = Time.realtimeSinceStartup;
            chunkHolder.gameObject.SetActive(true);
            if (chunkHolder.childCount != 0) DestroyWorld();
            CreateNoiseMap();
#if UNITY_EDITOR
            UpdateVisuals();
#endif
            CreateWorld();
            UpdateShader();
            onGenerate?.Invoke();
            _totalTime = Time.realtimeSinceStartup - startTime;
            _visualTime *= 1000;
        }
        private void CreateNoiseMap() {
            var startTime = Time.realtimeSinceStartup;
            if (_randomSeed) RandomSeed();
            var size = (worldSizeInChunks * chunkSize) / 2;
            if (Application.isPlaying) _noiseMap = VoxelNoise.GetPlanetNoiseMapJob(size, additive, seed, noiseSettings);
            else _noiseMap = VoxelNoise.GetPlanetNoiseMap(size, additive, seed, noiseSettings);
            _maxLayer = _noiseMap.GetLength(2) - 1;
            layerToView = Mathf.RoundToInt(_maxLayer / 2);
            _noiseGenerationTime = Time.realtimeSinceStartup - startTime;
        }
        private void UpdateVisuals() {
            if (_noiseMap == null) return;
            var startTime = Time.realtimeSinceStartup;
            _output = new Texture2D(_noiseMap!.GetLength(0), _noiseMap.GetLength(1));
            _colorMap = VoxelNoise.GetLayerColors3D(_noiseMap, layerToView, displayColors, out _, out _, true);
            _output.filterMode = FilterMode.Point;
            _output.SetPixels(_colorMap);
            _output.Apply();
            _visualTime = Time.realtimeSinceStartup - startTime;
        }
        private void CreateWorld() {
            var startTime = Time.realtimeSinceStartup;
            var radius = (worldSizeInChunks * chunkSize) / 2;
            _chunkOffset = new Vector3Int(-radius, -radius, -radius);
            for (int x = 0; x < worldSizeInChunks; x++) {
                for (int y = 0; y < worldSizeInChunks; y++) {
                    for (int z = 0; z < worldSizeInChunks; z++) {
                        var chuckPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        _chunks.Add(chuckPos, new Chunk(_noiseMap, chuckPos, chunkSize,
                            valueGate, createGate, doSmoothing, doFlatShading, viewDistance, colliderDistance,
                            planetMaterial));
                        _chunks[chuckPos].ChunkObject.transform.SetParent(chunkHolder);
                        _chunks[chuckPos].ChunkObject.layer = 3;
                    }
                }
            }
            chunkHolder.position += _chunkOffset;
            _chunkGenerationTime = Time.realtimeSinceStartup - startTime;
        }

        [Button("Shader"), ButtonGroup("Function")]
        private void UpdateShader() {
            var radius = (worldSizeInChunks * chunkSize) / 2;
            Vector3 offset = new Vector3Int(-radius, -radius, -radius);
            var center = Vector3.zero;
            if (chunkHolder.childCount > 0) {
                center = Vector3.one * radius;
                center += offset;
            }

            planetMaterial.SetVector("_center", new Vector4(center.x, center.y, center.z));
            //Float Values
            planetMaterial.SetFloat("_Grass_Warping", colorSettings.grassHighlightsStrength);
            planetMaterial.SetFloat("_Sand_Warping", colorSettings.sandHighlightsStrength);
            planetMaterial.SetFloat("_Sand_Height", colorSettings.sandHeight);
            planetMaterial.SetFloat("_Steepness_Warping", colorSettings.rockHighlightsStrength);
            planetMaterial.SetFloat("_Steepness_Threshold", colorSettings.rockThreshold);
            //Colors
            planetMaterial.SetColor("_Grass", colorSettings.grass);
            planetMaterial.SetColor("_Dark_Grass", colorSettings.grassHighlights);
            planetMaterial.SetColor("_Sand", colorSettings.sand);
            planetMaterial.SetColor("_Dark_Sand", colorSettings.sandHighlights);
            planetMaterial.SetColor("_Rock", colorSettings.rock);
            planetMaterial.SetColor("_Dark_Rock", colorSettings.rockHighlights);
        }
        [Button("Destroy"), ButtonGroup("Function")]
        private void DestroyWorld() {
            _chunks.Clear();
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
            var r = Helpers.GetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out var hit)) {
                point.position = hit.point;
                if (!doTerraform && hit.transform.gameObject.layer != 3) return;
                var numbers = hit.transform.name.Split(",");
                var currentChunk = new Vector3Int(Parse(numbers[0]), Parse(numbers[1]), Parse(numbers[2]));
                float3 center = (point.localPosition + -_chunkOffset);
                if (Input.GetKey(KeyCode.Mouse0)) {
                    AddChunks(currentChunk);
                    Terrafrom(currentChunk, center, true);
                }else if (Input.GetKey(KeyCode.Mouse1)) {
                    AddChunks(currentChunk);
                    Terrafrom(currentChunk, center, false);
                }
            }
        }
        
        private void Terrafrom(Vector3Int centerChunk, float3 worldPos, bool addTerrain) {
            var terraformPoint = worldPos - centerChunk.ToFloat3();
            terraformPoint += new float3(chunkSize, chunkSize, chunkSize);
            var newMapSize = chunkSize * (updateRange * 2 + 1); // 16x3
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
            VoxelNoise.Terraform(ref finalMap, newMapSize, addTerrain, brushRadius, brushSpeed, terraformPoint);
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
        private void AddChunks(Vector3Int centerChunk) {
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
