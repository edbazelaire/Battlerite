using Unity.Netcode;
using UnityEngine;
public class CameraController : NetworkBehaviour
{
    public Vector3 cameraOffset;                        // position of the camera from the player
    [Range(0.01f, 1.0f)] public float smoothness = 0.5f;
    public float zoomSpeed;

    private Transform _player;                            // player to follow
    private Camera _cam;
    private float _camFOV;
    private float _mouseScrollInput;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _camFOV = _cam.fieldOfView;
    }

    public void Update()
    {
        Follow();
        Scroll();
    }

    public void SetPlayerToFollow(GameObject playerToFollow)
    {
        _player = playerToFollow.transform;
    }

    private void Follow() {
        if (_player == null) { return; }
        
        // set position of the camera to offset + player position
        Vector3 cameraPosition = cameraOffset + _player.transform.position;
        // freeze y position
        cameraPosition.y = cameraOffset.y;
        // update camera initial position
        transform.position = Vector3.Slerp(transform.position, cameraPosition, smoothness);
    }

    private void Scroll()
    {
        _mouseScrollInput = Input.GetAxis("Mouse ScrollWheel");

        _camFOV -= _mouseScrollInput * zoomSpeed;
        _camFOV = Mathf.Clamp(_camFOV, 30, 60);

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _camFOV, zoomSpeed);
    }
}
