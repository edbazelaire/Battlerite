using System.Collections;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.Objects;
using _Scripts.Infrastructure;
using Unity.Netcode;
using UnityEngine;

public class ServerSpawner : NetworkBehaviour
{
    // networked object that will be spawned in waves
    [SerializeField]
    GameObject m_SpawnObject;

    [SerializeField]
    private Vector3 m_SpawnOffset = new (0, 1.35f, 0);

    [SerializeField] private float m_TimeBetweenSpawn;
    
    private NetworkObject m_SpawnedNetworkObject;
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }
        
        if (m_SpawnObject == null)
        {
            throw new System.ArgumentNullException("m_SpawnObject");
        }

        SpawnObject();
    }

    private void SpawnObject()
    {
        m_SpawnedNetworkObject = Instantiate(m_SpawnObject, transform.position + m_SpawnOffset, Quaternion.identity).GetComponent<NetworkObject>();

        m_SpawnedNetworkObject.GetComponent<ServerDestructibleObject>().DestroyedObject += OnDestroyedObject;
        m_SpawnedNetworkObject.Spawn(true);
    }

    private void OnDestroyedObject(ServerCharacter inflicter)
    {
        m_SpawnedNetworkObject.GetComponent<ServerDestructibleObject>().DestroyedObject -= OnDestroyedObject;
        m_SpawnedNetworkObject.Despawn();
        m_SpawnedNetworkObject = null;
        
        StartCoroutine(SetNextSpawn());
    }

    private IEnumerator SetNextSpawn()
    {
        yield return new WaitForSeconds(m_TimeBetweenSpawn);

        SpawnObject();
    }
}
