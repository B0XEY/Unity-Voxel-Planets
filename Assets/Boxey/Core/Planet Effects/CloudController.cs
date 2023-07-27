using System;
using Boxey.Core.Static;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Boxey.Core {
    public class CloudController : MonoBehaviour {
        private PlanetCreator _planet;
        private float _offset;
        private Camera _cam;
        private Matrix4x4 _matrix;

        public void SendUpdatedData(PlanetSettings settings) {
            if (_planetSettings != settings) _planetSettings = settings;
        }

        [Title("Planet Info")] 
        [ShowInInspector, ReadOnly] private LodData _planetLOD;
        [ShowInInspector, ReadOnly, InlineEditor] private PlanetSettings _planetSettings;
        
        [Title("Default Info")]
        [SerializeField] private float cloudHeight = 15;
        [SerializeField, Range(0.01f, 5f)] private float cloudSpacing = 1;
        
        [Title("Settings")] 
        [SerializeField] private Mesh cloudMesh;
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private int cloudDrawLayer;

        private void Awake() {
            _cam = Helpers.GetCamera;
            _planet = GetComponent<PlanetCreator>();
        }

        private void Update() {
            _planetLOD = _planet.GetCurrentLOD();
            for (int i = 0; i < _planetLOD.cloudLayers; i++) {
                float mult = (float)i / _planetLOD.cloudLayers * cloudSpacing;
                _matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * (mult + cloudHeight));
                Graphics.DrawMesh(cloudMesh, _matrix, cloudMaterial, cloudDrawLayer, _cam);
            }
        }
    }
}
