using Boxey.Core.Components;
using Boxey.Core.Static;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core {
    public class Terraformer : MonoBehaviour {
        private PlanetCreator m_target;
        private PlanetList m_list;
        private bool m_terraforming;
        public void SetList() {
            planets = m_list.GetPlanets();
        }

        [Title("Terraforming", titleAlignment: TitleAlignments.Centered)] 
        [SerializeField] private bool doTerraform = true;
        [ShowIf("@doTerraform")]
        [SerializeField] private Transform point;
        [ShowIf("@doTerraform")]
        [SerializeField, Range(1,5)] private int updateRange = 1;
        [ShowIf("@doTerraform")]
        [SerializeField, PropertyRange(.25f, 10)] private float brushRadius = 2.5f;
        [ShowIf("@doTerraform")]
        [SerializeField,Range(0.01f, 15f)] private float brushSpeed = 5;

        [Title("Planets", titleAlignment: TitleAlignments.Centered)] 
        [SerializeField] private PlanetCreator[] planets;

        private void Awake() {
            TryGetComponent(out m_list);
        }
        private void Update() {
            if (planets.Length == 0) return;
            var ray = Helpers.GetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                point.position = hit.point;
                if (hit.transform.name == "Sun") return;
                if (!doTerraform && hit.transform.gameObject.layer != 3) return;
                m_target = hit.transform.parent.GetComponentInParent<PlanetCreator>();
                if (m_target == null) return;
                point.SetParent(m_target.transform);
                var numbers = hit.transform.name.Split(",");
                var currentChunk = new Vector3Int(int.Parse(numbers[0]), int.Parse(numbers[1]), int.Parse(numbers[2]));
                float3 terraformPoint = (point.localPosition + -m_target.chunkOffset);
                if (Input.GetKey(KeyCode.Mouse0)) {
                    m_terraforming = true;
                    m_target.Terrafrom(currentChunk, terraformPoint, updateRange, brushRadius, brushSpeed, true);
                }
                else if (Input.GetKey(KeyCode.Mouse1)) {
                    m_terraforming = true;
                    m_target.Terrafrom(currentChunk, terraformPoint, updateRange, brushRadius, brushSpeed, false);
                }
                if (Input.GetKeyUp(KeyCode.Mouse0) && m_terraforming) {
                    m_terraforming = false;
                    m_target.FinishTerraform();
                }
                else if (Input.GetKeyUp(KeyCode.Mouse1)) {
                    m_terraforming = false;
                    m_target.FinishTerraform();
                }
            }
        }
    }
}
