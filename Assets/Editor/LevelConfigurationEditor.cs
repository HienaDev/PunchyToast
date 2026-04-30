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

            SerializedProperty newWave = list.serializedProperty.GetArrayElementAtIndex(index);

            newWave.FindPropertyRelative("allowBottomRow").boolValue = true;
            newWave.FindPropertyRelative("allowTopRow").boolValue = true;

            SerializedProperty innerList = newWave.FindPropertyRelative("clientsInWave");
            if (innerList != null)
            {
                innerList.arraySize = 0;
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

        // 1. Draw standard properties EXCEPT the boss-specific ones and the lists
        // We exclude bossPrefab and bossToastsRequired so we can draw them conditionally
        DrawPropertiesExcluding(serializedObject, new string[] { "waves", "m_Script", "bossPrefab", "bossToastsRequired" });

        SerializedProperty isBoss = serializedObject.FindProperty("isBossFight");

        // 2. Draw Boss Settings only if isBossFight is enabled
        if (isBoss.boolValue)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Boss Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("bossPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bossToastsRequired"));

            if (serializedObject.FindProperty("bossPrefab").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a Boss Prefab!", MessageType.Error);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Sequence", EditorStyles.boldLabel);
        waveList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}