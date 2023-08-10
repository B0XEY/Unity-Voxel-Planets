using Boxey.Core;
using Boxey.Planets.Core.Components;
using Boxey.Planets.Core.Editor;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Generation {
    public class Terraformer : MonoBehaviour {
        private PlanetCreator _target;
        private PlanetList _list;
        private bool _terraforming;
        public void SetList() {
            planets = _list.GetPlanets();
        }

        [Header("Terraforming")]
        [Line]
        [SerializeField] private bool doTerraform = true;
        [ShowIf("@doTerraform")]
        [SerializeField] private Transform point;
        [ShowIf("@doTerraform")]
        [SerializeField, Range(1,5)] private int updateRange = 1;
        [ShowIf("@doTerraform")]
        [SerializeField, Range(.25f, 10)] private float brushRadius = 2.5f;
        [ShowIf("@doTerraform")]
        [SerializeField,Range(0.01f, 1f)] private float brushSpeed = .4f;

        [Header("Planets")]
        [Line]
        [SerializeField] private PlanetCreator[] planets;

        private void Awake() {
            TryGetComponent(out _list);
        }
        private void Update() {
            if (planets.Length == 0) return;
            var ray = Helpers.GetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                point.position = hit.point;
                if (hit.transform.name == "Sun") return;
                if (!doTerraform && hit.transform.gameObject.layer != 3) return;
                _target = hit.transform.parent.GetComponentInParent<PlanetCreator>();
                if (_target == null) return;
                point.SetParent(_target.transform);
                var numbers = hit.transform.name.Split(",");
                var currentChunk = new Vector3Int(int.Parse(numbers[0]), int.Parse(numbers[1]), int.Parse(numbers[2]));
                float3 terraformPoint = (point.localPosition + -_target.chunkOffset);
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
