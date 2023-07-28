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
        public void SetCustomShaderData(Vector3 center, float radius) {
            _cloudMat.SetFloat("_Radius", radius);
            _cloudMat.SetFloat("_CloudSize", _planetSettings.cloudSize);
            _cloudMat.SetFloat("_CloudCutoff", _planetSettings.cloudiness);
            _cloudMat.SetFloat("_CloudSoftness", _planetSettings.cloudSoftness);
            _cloudMat.SetFloat("_CloudSpeed", _planetSettings.cloudSpeed);
            _cloudMat.SetColor("_CloudColor", _planetSettings.cloudColor);
            _cloudMat.SetVector("_Center", new Vector4(center.x,center.y,center.z));
        }

        [Title("Planet Info")] 
        [ShowInInspector, ReadOnly] private LodData _planetLOD;
        [ShowInInspector, ReadOnly, InlineEditor] private PlanetSettings _planetSettings;
        [ShowInInspector, ReadOnly, InlineEditor] private Material _cloudMat;
        
        [Title("Default Info")]
        [SerializeField] private Material cloud;
        [SerializeField] private float cloudHeight = 15;
        [SerializeField, Range(0.01f, 5f)] private float cloudSpacing = 1;
        
        [Title("Settings")] 
        [SerializeField] private Mesh cloudMesh;
        [SerializeField] private int cloudDrawLayer;

        private void Awake() {
            _cam = Helpers.GetCamera;
            _planet = GetComponent<PlanetCreator>();
            //_cloudMat = new Material(Shader.Find("Clouds"));
            _cloudMat = cloud;
        }

        private void Update() {
            _planetLOD = _planet.GetCurrentLOD();
            for (int i = 0; i < _planetLOD.cloudLayers; i++) {
                float mult = (float)i / _planetLOD.cloudLayers * cloudSpacing;
                _matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * (mult + cloudHeight));
                Graphics.DrawMesh(cloudMesh, _matrix, _cloudMat, cloudDrawLayer, _cam);
            }
        }
    }
}
