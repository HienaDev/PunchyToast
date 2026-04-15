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
            EditorGUI.LabelField(rect, "Level Waves (Auto-Expanding)");
        };

        waveList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            list.serializedProperty.serializedObject.ApplyModifiedProperties();

            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);
            newElement.isExpanded = true;

            SerializedProperty innerList = newElement.FindPropertyRelative("clientsInWave");
            if (innerList != null)
            {
                innerList.arraySize = 1;
                if (innerList.arraySize > 0)
                {
                    innerList.GetArrayElementAtIndex(0).isExpanded = true;
                }
            }

            list.serializedProperty.serializedObject.ApplyModifiedProperties();
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
        // 1. Pull data from the script
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);

        // 2. Automatically draw everything EXCEPT the "waves" list
        // This will show levelNumber, fiveStarTime, fourStarTime, etc.
        DrawPropertiesExcluding(serializedObject, new string[] { "waves", "m_Script" });

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Settings", EditorStyles.boldLabel);

        // 3. Draw your custom ReorderableList
        waveList.DoLayoutList();

        // 4. Push changes back to the script
        serializedObject.ApplyModifiedProperties();
    }
}