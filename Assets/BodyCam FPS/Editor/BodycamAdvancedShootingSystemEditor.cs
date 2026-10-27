using UnityEditor;

namespace RealisticBodycam.Editor
{
    [CustomEditor(typeof(BodycamAdvancedShootingSystem))]
    [CanEditMultipleObjects]
    public class BodycamAdvancedShootingSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty reloadTypeProperty = serializedObject.FindProperty("reloadType");
            bool usesShellReload = reloadTypeProperty != null &&
                                   reloadTypeProperty.enumValueIndex ==
                                   (int)BodycamAdvancedShootingSystem.ReloadType.ShellByShell;

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (ShouldHideProperty(property.name, usesShellReload))
                    continue;

                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        static bool ShouldHideProperty(string propertyName, bool usesShellReload)
        {
            if (usesShellReload)
                return propertyName == "reloadDuration" ||
                       propertyName == "fullReloadSound" ||
                       propertyName == "fullReloadShakeIntensity";

            return propertyName == "shellInsertSound" ||
                   propertyName == "shellInsertDuration" ||
                   propertyName == "shellAnimationOverlap" ||
                   propertyName == "allowShellReloadInterrupt" ||
                   propertyName == "shellReloadShakeIntensity" ||
                   propertyName == "shellReloadShakeDuration";
        }
    }
}
