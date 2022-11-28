using _Scripts.Infrastructure.ScriptableObjectArchitecture;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers
{
    /// <summary>
    /// Class which registers a transform to an associated TransformVariable ScriptableObject.
    /// </summary>
    public class TransformRegister : MonoBehaviour
    {
        [SerializeField]
        TransformVariable m_TransformVariable;

        void OnEnable()
        {
            m_TransformVariable.Value = transform;
        }

        void OnDisable()
        {
            m_TransformVariable.Value = null;
        }
    }
}
