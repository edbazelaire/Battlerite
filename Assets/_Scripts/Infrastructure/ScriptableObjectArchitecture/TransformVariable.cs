using UnityEngine;

namespace _Scripts.Infrastructure.ScriptableObjectArchitecture
{
    /// <summary>
    /// A ScriptableObject which contains a reference to a Transform component. This can be used to remove dependencies
    /// between scene objects.
    /// </summary>
    [CreateAssetMenu]
    public class TransformVariable : ScriptableObject
    {
        public Transform Value;
    }
}
