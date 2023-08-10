using Boxey.Core;
using Boxey.Planets.Core.Editor;
using Boxey.Planets.Core.Generation.Data_Classes;
using Boxey.Planets.Core.Static;
using UnityEngine;

namespace Boxey.Planets.Core.Planet_Effects {
    public class SkyManager : MonoBehaviour {
        private PlanetCreator m_planet;
        private float m_offset;
        private Camera m_cam;
        private Matrix4x4 m_matrix;
        private bool m_isCreated;

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
            m_isCreated = true;
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

        public SkyManager(Material cloudMat)
        {
            this.cloudMat = cloudMat;
        }

        private void Awake() {
            m_cam = Helpers.GetCamera;
            m_planet = GetComponent<PlanetCreator>();
        }

        private void Update() {
            if (!m_isCreated || !planetSettings.hasAtmosphere) return;
            planetLOD = m_planet.GetCurrentLOD();
            for (int i = 0; i < planetLOD.cloudLayers; i++) {
                float mult = (float)i / planetLOD.cloudLayers * cloudSpacing;
                m_matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * (mult + cloudHeight));
                Graphics.DrawMesh(cloudMesh, m_matrix, cloudMat, cloudDrawLayer, m_cam);
            }
        }
    }
}
