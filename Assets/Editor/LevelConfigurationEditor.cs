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
                innerList.GetArrayElementAtIndex(0).isExpanded = true;
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
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General & Toaster Settings", EditorStyles.boldLabel);

        // This will now show levelNumber AND the 4 hover variables automatically
        DrawPropertiesExcluding(serializedObject, new string[] { "waves", "m_Script" });

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Settings", EditorStyles.boldLabel);

        waveList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}