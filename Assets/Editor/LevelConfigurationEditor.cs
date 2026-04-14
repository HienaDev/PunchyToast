using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(LevelConfiguration))]
public class LevelConfigurationEditor : Editor
{
    private ReorderableList waveList;

    private void OnEnable()
    {
        // Link to the "waves" property in the ScriptableObject
        SerializedProperty wavesProp = serializedObject.FindProperty("waves");

        waveList = new ReorderableList(serializedObject, wavesProp, true, true, true, true);

        // Header Label
        waveList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Level Waves (Auto-Expanding)");
        };

        waveList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            // Apply properties so the new element is "real" in Unity's memory
            list.serializedProperty.serializedObject.ApplyModifiedProperties();

            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);

            // Force the Wave foldout to be open
            newElement.isExpanded = true;

            // Safely find the inner list
            SerializedProperty innerList = newElement.FindPropertyRelative("clientsInWave");

            if (innerList != null)
            {
                innerList.arraySize = 1; // Start with one client
                // Force the first client entry to be open too
                if (innerList.arraySize > 0)
                {
                    innerList.GetArrayElementAtIndex(0).isExpanded = true;
                }
            }

            // Apply again to save the expansion and inner list changes
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        };

        // Draw the elements
        waveList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = waveList.serializedProperty.GetArrayElementAtIndex(index);

            // Shift rect slightly for padding
            rect.y += 2;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element,
                new GUIContent($"Wave {index + 1}"),
                true
            );
        };

        // Adjust height based on whether the wave is expanded
        waveList.elementHeightCallback = (int index) => {
            SerializedProperty element = waveList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 4;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        waveList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}