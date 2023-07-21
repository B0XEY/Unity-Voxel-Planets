using System;
using Boxey.Scripts.Planets.Static;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Scripts.Planets.Generation {
    public class Terraformer : MonoBehaviour {
        private PlanetCreator _target;
        
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
            GetAllPlanets();
        }

        private void Update() {
            _target = GetTerraformTarget();
            if (_target == null) return;
            point.SetParent(_target.transform);
            var r = Helpers.GetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out var hit)) {
                point.position = hit.point;
                if (!doTerraform && hit.transform.gameObject.layer != 3) return;
                var numbers = hit.transform.name.Split(",");
                var currentChunk = new Vector3Int(int.Parse(numbers[0]), int.Parse(numbers[1]), int.Parse(numbers[2]));
                float3 terraformPoint = (point.localPosition + -_target.chunkOffset);
                if (Input.GetKey(KeyCode.Mouse0)) {
                    _target.Terrafrom(currentChunk, terraformPoint, updateRange, brushRadius, brushSpeed, true);
                }else if (Input.GetKey(KeyCode.Mouse1)) {
                    _target.Terrafrom(currentChunk, terraformPoint, updateRange, brushRadius, brushSpeed, false);
                }
            }
        }
        
        PlanetCreator GetTerraformTarget() {
            PlanetCreator target = null;
            float minDist = Mathf.Infinity;
            Vector3 currentPos = transform.position;
            foreach (PlanetCreator t in planets) {
                float dist = Vector3.Distance(t.transform.position, currentPos);
                if (dist < minDist) {
                    target = t;
                    minDist = dist;
                }
            }
            return target;
        }
        
        [Button("Get Planets", ButtonSizes.Large)]
        public void GetAllPlanets() {
            planets = FindObjectsOfType<PlanetCreator>();
        }
    }
}
