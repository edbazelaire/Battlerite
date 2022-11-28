using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.UI;
using _Scripts.Infrastructure.ScriptableObjectArchitecture;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;


namespace _Scripts.Gameplay.GameplayObjects.Objects
{
    /// <summary>
    /// Class designed to only run on a client. Add this to a world-space prefab to display object health on UI.
    /// </summary>
    /// <remarks>
    /// Execution order is explicitly set such that it this class executes its LateUpdate after any Cinemachine
    /// LateUpdate calls, which may alter the final position of the game camera.
    /// </remarks>
    [DefaultExecutionOrder(300)]
    public class UIObjectHealthDisplayer: NetworkBehaviour
    {
        [SerializeField]
        UIStateDisplay m_UIStatePrefab;

        // spawned in world (only one instance of this)
        UIStateDisplay m_UIState;

        RectTransform m_UIStateRectTransform;

        bool m_UIStateActive;
        
        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        [Tooltip("UI object(s) will appear positioned at this transforms position.")]
        [SerializeField]
        Transform m_TransformToTrack;

        Camera m_Camera;

        Transform m_CanvasTransform;

        [Tooltip("World space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalWorldOffset;

        [Tooltip("Screen space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalScreenOffset;

        Vector3 m_VerticalOffset;

        // used to compute world position based on target and offsets
        Vector3 m_WorldPos;

        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
                return;
            }

            var cameraGameObject = GameObject.FindWithTag("MainCamera");
            if (cameraGameObject)
            {
                m_Camera = cameraGameObject.GetComponent<Camera>();
            }
            Assert.IsNotNull(m_Camera);

            var canvasGameObject = GameObject.FindWithTag("GameCanvas");
            if (canvasGameObject)
            {
                m_CanvasTransform = canvasGameObject.transform;
            }
            Assert.IsNotNull(m_CanvasTransform);
            
            Assert.IsNotNull(m_NetworkHealthState, "A NetworkHealthState component needs to be attached!");

            m_VerticalOffset = new Vector3(0f, m_VerticalScreenOffset, 0f);
            
            m_NetworkHealthState.hitPointsReplenished += DisplayUIHealth;
            m_NetworkHealthState.hitPointsDepleted += RemoveUIHealth;
            DisplayUIHealth();
        }

        void OnDisable()
        {
            if (m_NetworkHealthState != null)
            {
                m_NetworkHealthState.hitPointsReplenished -= DisplayUIHealth;
                m_NetworkHealthState.hitPointsDepleted -= RemoveUIHealth;
            }
        }

        void DisplayUIHealth()
        {
            if (m_NetworkHealthState == null)
            {
                Debug.Log("m_NetworkHealthState == null");
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }

            m_UIState.DisplayHealth(m_NetworkHealthState.HitPoints, m_NetworkHealthState.HitPoints.Value);
            m_UIStateActive = true;
        }

        void SpawnUIState()
        {
            m_UIState = Instantiate(m_UIStatePrefab, m_CanvasTransform);
            // make in world UI state draw under other UI elements
            m_UIState.transform.SetAsFirstSibling();
            m_UIStateRectTransform = m_UIState.GetComponent<RectTransform>();
        }

        void RemoveUIHealth()
        {
            m_UIState.HideHealth();
        }

        /// <remarks>
        /// Moving UI objects on LateUpdate ensures that the game camera is at its final position pre-render.
        /// </remarks>
        void LateUpdate()
        {
            if (m_UIStateActive && m_TransformToTrack)
            {
                var positionToTrack = m_TransformToTrack.position;
                // set world position with world offset added
                m_WorldPos.Set(
                    positionToTrack.x,
                    positionToTrack.y + m_VerticalWorldOffset,
                    positionToTrack.z);

                m_UIStateRectTransform.position = m_Camera.WorldToScreenPoint(m_WorldPos) + m_VerticalOffset;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (m_UIState != null)
            {
                Destroy(m_UIState.gameObject);
            }
        }
    }
}