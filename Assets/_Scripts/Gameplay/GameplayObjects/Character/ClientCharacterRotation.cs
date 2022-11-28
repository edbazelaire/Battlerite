using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    public class ClientCharacterRotation: NetworkBehaviour
    {
        [SerializeField] 
        private NetworkCharacterState NetState;
        
        private Vector3 _mousePosition;
        private Camera _camera;
        
        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                // dont need to do anything else if not the owner
                return;
            }

            _camera = Camera.main;
            
            Assert.IsNotNull(_camera, 
                "Main Camera not set");
        }
        
        void Update() {
            var currentMousePosition = GetMousePosition();
            
            if (currentMousePosition != _mousePosition) {
                NetState.RcvMousePositionServerRpc(_mousePosition);

                _mousePosition = currentMousePosition;
                Quaternion rotationToLookAt = Quaternion.LookRotation(currentMousePosition - transform.position);
                transform.eulerAngles = new Vector3(0, rotationToLookAt.eulerAngles.y, 0);

                NetState.RecvRotationServerRPC(transform.rotation);
            }
        }
        
        private Vector3 GetMousePosition()
        {
            if (Physics.Raycast(_camera.ScreenPointToRay(UnityEngine.Input.mousePosition), out var hit, Mathf.Infinity))
            {
                return hit.point;
            }
            
            return default;
        }
    }
}