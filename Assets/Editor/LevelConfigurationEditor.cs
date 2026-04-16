using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(LevelConfiguration))]
public class LevelConfigurationEditor : Editor
{
    private ReorderableList waveList;

    private void OnEnable()
    {
        SerializedProperty wavesProp = serializedObject.FindProperty("waves");
        waveList = new ReorderableList(serializedObject, wavesProp, true, true, true, true);

        waveList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Level Waves (Defaults to Rows: ON)");
        };

        waveList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            // Get the newly created wave element
            SerializedProperty newWave = list.serializedProperty.GetArrayElementAtIndex(index);

            // STICKY FIX: Explicitly set booleans to true for the new element
            newWave.FindPropertyRelative("allowBottomRow").boolValue = true;
            newWave.FindPropertyRelative("allowTopRow").boolValue = true;

            // Optional: Initialize the inner client list so it's not empty/null
            SerializedProperty innerList = newWave.FindPropertyRelative("clientsInWave");
            if (innerList != null)
            {
                innerList.arraySize = 0; // Start clean or set to 1 if preferred
            }

            serializedObject.ApplyModifiedProperties();
        };

        waveList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = waveList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element,
                new GUIContent($"Wave {index + 1}"),
                true
            );
        };

        waveList.elementHeightCallback = (int index) => {
            SerializedProperty element = waveList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 4;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General Level Settings", EditorStyles.boldLabel);

        // Draw everything except the waves list and the script reference
        DrawPropertiesExcluding(serializedObject, new string[] { "waves", "m_Script" });

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Sequence", EditorStyles.boldLabel);
        waveList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}