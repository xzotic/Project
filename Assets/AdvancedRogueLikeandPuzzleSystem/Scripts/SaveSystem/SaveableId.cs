using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Gives a scene object a stable GUID so <see cref="GameSession"/> can remember it across
    /// sessions (consumed pickups, opened chests and doors). The id is generated in the Editor
    /// (OnValidate) and serialized into the scene. Duplicating an object copies its id; clear
    /// the field on the copy to generate a new one.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveableId : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Generated automatically. Changing it makes existing saves forget this object.")]
        private string id = "";

        public string Id => id;

        public bool HasId => !string.IsNullOrEmpty(id);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                Assign(System.Guid.NewGuid().ToString("N"));
            }
        }

        public void Assign(string newId)
        {
            id = newId;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
