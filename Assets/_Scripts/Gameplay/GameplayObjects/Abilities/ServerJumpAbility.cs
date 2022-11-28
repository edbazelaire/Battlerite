using System;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public class ServerJumpAbility: ServerAbility
    {
        // [SerializeField] 
        // private GameObject m_ReplaceGfx;
        //
        // public float Speed;
        //
        // private GameObject _characterGfx;
        // private GameObject _replacedGfx;
        // private Collider _characterCollider;
        //
        // private float _range;
        // private Vector3 _direction;
        // private Vector3 _initialPosition;
        //
        // public void Initialize(ulong spawnerId, ActionDescription description, Vector3 mousePosition)
        // {
        //     base.Initialize(spawnerId, description);
        //
        //     NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(_spawnerId, out var spawnerNet);
        //     var characterPosition = spawnerNet.transform.position;
        //     
        //     _range = GetFinalRange(characterPosition, mousePosition);
        //     _direction = mousePosition - characterPosition;
        //     _direction.y = 0;
        //     
        //     var clientCharacterVisualization = serverCharacter.GetComponentInChildren<ClientCharacterVisualization>();
        //     if (clientCharacterVisualization != null)
        //     {
        //         _characterGfx = clientCharacterVisualization.transform.GetChild(0).gameObject;
        //     }
        //
        //     _characterCollider = _serverCharacter.GetComponent<Collider>();
        // }
        //
        // public override void OnNetworkSpawn()
        // {
        //     base.OnNetworkSpawn();
        //     
        //     // replace graphics
        //     SwapGraphics(true);
        //     
        //     // remove collider
        //     SwapCollider(false);
        //     
        //     // set movement in motion
        //     _initialPosition = transform.position;
        //     _serverCharacter.Movement.Jump(_direction, Speed);
        // }
        //
        // public override bool WhileActive()
        // {
        //     var travelledDistance = HelpFunctions.Distance2D(_initialPosition, _serverCharacter.physicsWrapper.Transform.position);
        //     if (travelledDistance >= _range)
        //     {
        //         return false;
        //     }
        //
        //     return true;
        // }
        //
        // private void SwapGraphics(bool toReplacement)
        // {
        //     if (m_ReplaceGfx == null) { return; }
        //     
        //     Assert.IsNotNull(_characterGfx,
        //         $"object {gameObject} has no children graphics in ClientCharacterVisualization");
        //
        //     if (toReplacement)
        //     {
        //         _characterGfx.gameObject.SetActive(false);
        //         _replacedGfx = Instantiate(m_ReplaceGfx, _serverCharacter.gameObject.transform);
        //         _replacedGfx.transform.position += m_ReplaceGfx.transform.localPosition;
        //     }
        //     else
        //     {
        //         Destroy(_replacedGfx.gameObject);
        //         _characterGfx.gameObject.SetActive(true);
        //     }
        // }
        //
        // private void SwapCollider(bool activate)
        // {
        //     if (_characterCollider != null)
        //     {
        //         _characterCollider.enabled = activate;
        //     }
        // }
        //
        // public override void Kill()
        // {
        //     _serverCharacter.Movement.StopForcedMovement();
        //     SwapGraphics(false);
        //     SwapCollider(true);
        //
        //     base.Kill();
        // }
    }
}