using Boxey.Core.Components;
using Boxey.Core.Static;
using Boxey.Planets.Core.Editor;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core {
    public class Terraformer : MonoBehaviour {
        private GameObject _point;
        private PlanetCreator _target;
        private PlanetList _list;
        private bool _terraforming;
        public void SetList() {
            planets = _list.GetPlanets();
        }

        [Header("Terraforming")]
        [Line]
        [SerializeField] private bool doTerraform = true;
        [ShowIf("doTerraform")]
        [SerializeField, Range(1,5)] private int updateRange = 1;
        [ShowIf("doTerraform")]
        [SerializeField, Range(.25f, 10)] private float brushRadius = 2.5f;
        [ShowIf("doTerraform")]
        [SerializeField, Range(0.01f, 1f)] private float brushSpeed = .4f;

        [Header("Planets")]
        [Line]
        [SerializeField] private PlanetCreator[] planets = new PlanetCreator[1];

        private void Awake(){
            _list = FindObjectOfType<PlanetList>();
            _point = new GameObject("Terraform Point");
        }
        private void Update() {
            if (planets.Length == 0) return;
            var ray = Helpers.GetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                _point.transform.position = hit.point;
                if (hit.transform.name == "Sun") return;
                if (!doTerraform && hit.transform.gameObject.layer != 3) return;
                _target = hit.transform.parent.GetComponentInParent<PlanetCreator>();
                if (_target == null) return;
                _point.transform.SetParent(_target.transform);
                var numbers = hit.transform.name.Split(",");
                var currentChunk = new Vector3Int(int.Parse(numbers[0]), int.Parse(numbers[1]), int.Parse(numbers[2]));
                float3 terraformPoint = (_point.transform.localPosition + -_target.chunkOffset);
                if (Input.GetKey(KeyCode.Mouse0)) {
                    _terraforming = true;
                    _target.Terrafrom(currentChunk, terraformPoint, updateRange, brushRadius, brushSpeed, true);
                }
                else if (Input.GetKey(KeyCode.Mouse1)) {
                    _terraforming = true;
                    _target.Terrafrom(currentChunk, terraformPoint, updateRange, brushRadius, brushSpeed, false);
                }
                if (Input.GetKeyUp(KeyCode.Mouse0) && _terraforming) {
                    _terraforming = false;
                    _target.FinishTerraform();
                }
                else if (Input.GetKeyUp(KeyCode.Mouse1)) {
                    _terraforming = false;
                    _target.FinishTerraform();
                }
            }
        }
    }
}
