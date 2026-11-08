#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class KeepHierarchyExpanded
{
    private static string expandedState;

    static KeepHierarchyExpanded()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Right before exiting Play mode, save expanded objects
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            var hierarchyWindow = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow"));
            if (hierarchyWindow != null)
            {
                // Forces Unity's internal scene state to cache current foldouts
                EditorApplication.DirtyHierarchyWindowSorting();
            }
        }
    }
}
#endif