using System;
using Planets.Static;
using Planets.Tools;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Planets.Generation.V1 {
    public class NoiseGen : MonoBehaviour {
        private enum NoiseMode {
            [InspectorName("2D")]
            TwoDimensional,
            [InspectorName("3D")]
            ThreeDimensional,
            [InspectorName("Planet")]
            Planet
        }

        private Texture2D _materialTexture;
        
        private float _maxLayer;
        private float[,] _mapFallOff;
        private float[,] _map2D;
        private float[,,] _map3D;
        private Color[] _colorMap;
        
        private void UpdateReal() => seed = _realSize = (size + additive) * 2;
        private void RandomSeed() => seed = Random.Range(-999999, 999999);
        private void RandomSeedToggle() => _randomSeed = !_randomSeed;

        [Title("Noise Values", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, Tooltip("Defines The Generation Mode"), EnumToggleButtons] private NoiseMode mode = NoiseMode.TwoDimensional;
        [SerializeField, InlineButton("RandomSeed", " R"), InlineButton("RandomSeedToggle", "T")] private int seed;
        [SerializeField, Tooltip("Noise Scale")] private float scale = 1f;
        [SerializeField, Tooltip("Noise Octaves"), InlineEditor] private NoiseSettings noiseSettings;
        [SerializeField, Tooltip("Map Size"), OnValueChanged("UpdateReal")] private int size;
        
        [ShowIf("@this.mode == NoiseMode.Planet")]
        [SerializeField, Tooltip("This Gets Added to The Radius Value. Making The Planet the Same Size Just in a Larger Map"), OnValueChanged("UpdateReal"), Range(0, 25)]
        private int additive = 5;
        [ShowIf("@this.mode == NoiseMode.Planet")]
        [ShowInInspector, Tooltip("Planets real Map size"), ReadOnly] private int _realSize;

        [ShowIf("@this.mode == NoiseMode.TwoDimensional")]
        [SerializeField, Tooltip("2D Noise Offset")] private Vector2 offset2D;
        
        [ShowIf("@this.mode != NoiseMode.TwoDimensional")]
        [SerializeField, Tooltip("3D Noise Offset")] private Vector3 offset3D;

        
        [Title("Material Settings", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private Material planetMaterial;

        [Title("Display", titleAlignment: TitleAlignments.Centered)]
        [OnValueChanged("UpdateVisuals")]
        [Tooltip("White = 0, Black = 1"), SerializeField] private Gradient displayColors;
        [OnValueChanged("UpdateVisuals")]
        [ShowIf("@this.mode != NoiseMode.TwoDimensional")]
        [SerializeField, PropertyRange(1, "@_maxLayer")] private int layerToView;
        [Space(15f)]
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text sizeText;
        [Space(15f)]
        [ShowInInspector, ReadOnly] private float _maxValue;
        [ShowInInspector, ReadOnly] private float _minValue;
        [ShowInInspector, ReadOnly] private bool _randomSeed;
        [Space(7.5f)]
        [ShowInInspector, ReadOnly, SuffixLabel("ms")] private float _visualTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _generationTime;
        [ShowInInspector, ReadOnly, SuffixLabel("sec")] private float _totalTime;
        [ShowInInspector, ReadOnly, HideLabel, PreviewField(250, ObjectFieldAlignment.Center)] private Texture2D _output;

        [Title("Events", titleAlignment: TitleAlignments.Centered)]
        [SerializeField] private UnityEvent onGenerate;

        [Button, ButtonGroup("Gen")]
        public void Generate() {
            var startTime = Time.realtimeSinceStartup;
            UpdateReal();
            GetNoise();
            UpdateVisuals();
            onGenerate?.Invoke();
            _totalTime = Time.realtimeSinceStartup - startTime;
            _visualTime *= 1000;
            UpdateUI();
        }
        [Button, ButtonGroup("Functions")]
        private void GetNoise() {
            var startTime = Time.realtimeSinceStartup;
            //_mapFallOff = VoxelNoiseHelpers.Get2DFallOffMap(size.x, size.y);
            if (_randomSeed) RandomSeed();
            if (mode == NoiseMode.TwoDimensional) {
                _map2D = VoxelNoiseHelpers.Get2DNoiseMap(size, size, scale, seed, offset2D, noiseSettings, out _minValue, out _maxValue);
            }else if (mode == NoiseMode.ThreeDimensional) {
                _maxLayer = size;
                if (Application.isPlaying) _map3D = VoxelNoiseHelpers.Get3DNoiseMapJob(size, size, size, scale, seed, offset3D, noiseSettings, out _minValue, out _maxValue);
                else _map3D = VoxelNoiseHelpers.Get3DNoiseMap(size, size, size, scale, seed, offset3D, noiseSettings, out _minValue, out _maxValue);
            }else if (mode == NoiseMode.Planet) {
                _maxLayer = _realSize; 
                if (Application.isPlaying) _map3D = VoxelNoiseHelpers.GetPlanetNoiseMapJob(size, additive, scale, seed, offset3D, noiseSettings, out _minValue, out _maxValue);
                else _map3D = VoxelNoiseHelpers.GetPlanetNoiseMap(size, additive, scale, seed, offset3D, noiseSettings, out _minValue, out _maxValue);
            }
            layerToView = Mathf.CeilToInt(_maxLayer / 2) + 1;
            _generationTime = Time.realtimeSinceStartup - startTime;
        }
        private void UpdateVisuals() {
            var startTime = Time.realtimeSinceStartup;
            if (mode == NoiseMode.TwoDimensional) {
                if (_map2D == null) GetNoise();
                _output = new Texture2D(_map2D!.GetLength(0), _map2D.GetLength(1));
                _colorMap = VoxelNoiseHelpers.GetLayerColors2D(_map2D, displayColors, out _minValue, out _maxValue);
            }else {
                if (_map3D == null) GetNoise();
                _output = new Texture2D(_map3D!.GetLength(0), _map3D.GetLength(1));
                _colorMap = VoxelNoiseHelpers.GetLayerColors3D(_map3D, layerToView - 1, displayColors, out _minValue, out _maxValue, mode == NoiseMode.Planet);
            }
            _output.filterMode = FilterMode.Point;
            _output.SetPixels(_colorMap);
            _output.Apply();
            _visualTime = Time.realtimeSinceStartup - startTime;
        }
        [Button, ButtonGroup("Functions")]
        public void UpdateShader() {
            var center = Vector3.zero;
            if (transform.childCount > 0) {
                center = Vector3.one * (size + additive);
                center += GetComponentInChildren<ObjectOffsetTool>().GetPosition();
            }
            planetMaterial.SetVector("_center", new Vector4(center.x, center.y, center.z));
        }

        //Getters
        public void SetSize(string text) { 
            size = Int32.Parse(text);
            UpdateReal();
        }
        public float[,,] Get3DMap() => _map3D;
        private void UpdateUI() {
            timeText.text = "Total Time - " + _totalTime.ToString();
            var sizeValue = mode == NoiseMode.Planet ? _realSize : size;
            sizeText.text = "Current Size - " + sizeValue.ToString();
        }

    }
}