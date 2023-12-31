using Boxey.Planets.Core.Editor;
using UnityEngine;

namespace Boxey.Planets.Extras {
    public class CameraMovement : MonoBehaviour {
        private float _rotationX;
        private float _rotationY;
        [Header("Movement")]
        [Line (1.5f, .5f,.5f,.5f)]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        
 
        

        private void Awake() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.P)) {
                var scale = Time.timeScale == 0 ? 1 : 0;
                Time.timeScale = scale;
                if (scale == 1) {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }else
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            }

            float mult = 1;
            if (Input.GetKey(KeyCode.LeftShift)) mult = 10;
            float translationX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime * mult;
            float translationZ = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime * mult;
            float translationY = Input.GetAxis("UpDown") * movementSpeed * Time.deltaTime * mult;
            transform.Translate(new Vector3(translationX, translationY, translationZ));
            
            _rotationX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            _rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
            transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0f);
        }
    }
}
