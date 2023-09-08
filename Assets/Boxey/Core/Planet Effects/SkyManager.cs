using Boxey.Core.Static;
using Boxey.Planets.Core.Editor;
using UnityEngine;

namespace Boxey.Core {
    public class SkyManager : MonoBehaviour {
        private PlanetCreator _planet;
        private float _offset;
        private Camera _cam;
        private Matrix4x4 _matrix;
        private bool _isCreated;

        public void SendUpdatedData(PlanetSettings settings) {
            if (planetSettings != settings) planetSettings = settings;
        }
        public void SetCustomShaderData(Vector3 center, float radius) {
            cloudMat = new Material(cloud);
            cloudMat.SetFloat("_Radius", radius);
            cloudMat.SetFloat("_CloudSize", planetSettings.cloudSize);
            cloudMat.SetFloat("_CloudCutoff", planetSettings.cloudiness);
            cloudMat.SetFloat("_CloudSoftness", planetSettings.cloudSoftness);
            cloudMat.SetFloat("_CloudSpeed", planetSettings.cloudSpeed);
            cloudMat.SetColor("_CloudColor", planetSettings.cloudColor);
            cloudMat.SetVector("_Center", new Vector4(center.x,center.y,center.z));
            _isCreated = true;
        }

        [Header("Planet Info")]
        [Line]
        [SerializeField, ShowOnly] private LodData planetLOD;
        [SerializeField, ShowOnly] private PlanetSettings planetSettings;
        [SerializeField, ShowOnly] private Material cloudMat;
        
        [Header("Default Info")]
        [Line]
        [SerializeField] private Material cloud;
        [SerializeField] private float cloudHeight = 15;
        [SerializeField, Range(0.01f, 5f)] private float cloudSpacing = 1;
        
        [Header("Settings")]
        [Line]
        [SerializeField] private Mesh cloudMesh;
        [SerializeField] private int cloudDrawLayer;

        private void Awake() {
            _cam = Helpers.GetCamera;
            _planet = GetComponent<PlanetCreator>();
        }

        private void Update() {
            if (!_isCreated || !planetSettings.hasAtmosphere) return;
            planetLOD = _planet.GetCurrentLOD();
            for (int i = 0; i < planetLOD.cloudLayers; i++) {
                float mult = (float)i / planetLOD.cloudLayers * cloudSpacing;
                _matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * (mult + cloudHeight));
                Graphics.DrawMesh(cloudMesh, _matrix, cloudMat, cloudDrawLayer, _cam);
            }
        }
    }
}
