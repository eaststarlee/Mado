using UnityEngine;

[CreateAssetMenu(fileName = "NewEntityId", menuName = "Save System/Entity ID")]
public class EntityIdSO : ScriptableObject
{
    [SerializeField, HideInInspector]
    private string _guid = System.Guid.NewGuid().ToString();

    public string Guid => _guid;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_guid))
        {
            _guid = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
