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
            newWave.isExpanded = true;

            newWave.FindPropertyRelative("allowBottomRow").boolValue = true;
            newWave.FindPropertyRelative("allowTopRow").boolValue = true;

            SerializedProperty innerList = newWave.FindPropertyRelative("clientsInWave");
            if (innerList != null)
            {
                innerList.arraySize = 0;
                innerList.isExpanded = true;
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

            SerializedProperty clientsProp = element.FindPropertyRelative("clientsInWave");
            if (clientsProp.isExpanded)
            {
                if (clientsProp.arraySize > 0)
                {
                    var lastClient = clientsProp.GetArrayElementAtIndex(clientsProp.arraySize - 1);
                    if (lastClient.FindPropertyRelative("customLetter").stringValue == "" && !lastClient.isExpanded)
                    {
                        lastClient.isExpanded = true;
                    }
                }
            }
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

        DrawPropertiesExcluding(serializedObject, new string[] { "waves", "m_Script", "bossPrefab", "bossToastsRequired" });

        SerializedProperty isBoss = serializedObject.FindProperty("isBossFight");

        if (isBoss.boolValue)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Boss Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bossPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bossToastsRequired"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Sequence", EditorStyles.boldLabel);
        waveList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomPropertyDrawer(typeof(LevelConfiguration.ClientData))]
public class ClientDataDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        int lines = 4;
        return (lines * EditorGUIUtility.singleLineHeight) + (lines * 2) + 12;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // --- 1. HEX COLOR LOGIC (Fixed Priority) ---
        SerializedProperty jamFlavorProp = property.FindPropertyRelative("jamFlavor");
        string flavor = jamFlavorProp.enumNames[jamFlavorProp.enumValueIndex].ToLower();

        Color barColor = new Color(0.5f, 0.5f, 0.5f, 0.2f); // Default gray
        string hex = "";

        // Check for Peanut Butter FIRST
        if (flavor.Contains("peanut"))
        {
            hex = "#634323"; // The Dark Brown hex
        }
        // Then check for standard butter
        else if (flavor.Contains("butter"))
        {
            hex = "#F5DA71"; // The Yellow hex
        }
        else if (flavor.Contains("strawberry"))
        {
            hex = "#E8403D";
        }
        else if (flavor.Contains("grape"))
        {
            hex = "#7329CF";
        }

        if (!string.IsNullOrEmpty(hex))
        {
            ColorUtility.TryParseHtmlString(hex, out barColor);
            barColor.a = 0.6f;
        }

        if (!string.IsNullOrEmpty(hex))
        {
            ColorUtility.TryParseHtmlString(hex, out barColor);
            barColor.a = 0.6f; // Slightly higher alpha since it's just a bar
        }

        // --- 2. DRAW THE HEADER BAR ONLY ---
        // Only covers the first line (the foldout/element label area)
        Rect headerBarRect = new Rect(position.x - 2, position.y, position.width + 4, EditorGUIUtility.singleLineHeight);
        EditorGUI.DrawRect(headerBarRect, barColor);

        // --- 3. DYNAMIC STYLES ---
        GUIStyle greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
        GUIStyle redStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.3f, 0.3f) } };

        SerializedProperty simultaneousProp = property.FindPropertyRelative("simultaneousToast");
        SerializedProperty slappableProp = property.FindPropertyRelative("isSlappable");

        // --- 4. DRAW CONTENT ---
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            float y = position.y + EditorGUIUtility.singleLineHeight + 2;
            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = 0;
            float halfWidth = (position.width / 2) - 5;

            // Row 1: Flavor & Char
            EditorGUI.LabelField(new Rect(position.x, y, 50, lineH), "Flavor:");
            EditorGUI.PropertyField(new Rect(position.x + 50, y, halfWidth - 50, lineH), jamFlavorProp, GUIContent.none);

            EditorGUI.LabelField(new Rect(position.x + halfWidth + 10, y, 40, lineH), "Char:");
            EditorGUI.PropertyField(new Rect(position.x + halfWidth + 50, y, position.width - (halfWidth + 50), lineH), property.FindPropertyRelative("customLetter"), GUIContent.none);

            y += lineH + spacing;

            // Row 2: Toasts Needed
            EditorGUI.LabelField(new Rect(position.x, y, 100, lineH), "Toasts Needed:");
            EditorGUI.PropertyField(new Rect(position.x + 100, y, position.width - 100, lineH), property.FindPropertyRelative("toastsNeeded"), GUIContent.none);

            y += lineH + spacing;

            // Row 3: Simultaneous
            GUIStyle simulStyle = simultaneousProp.boolValue ? greenStyle : redStyle;
            EditorGUI.LabelField(new Rect(position.x, y, 120, lineH), "Simultaneous:", simulStyle);
            EditorGUI.PropertyField(new Rect(position.x + 120, y, position.width - 120, lineH), simultaneousProp, GUIContent.none);

            y += lineH + spacing;

            // Row 4: Slappable
            GUIStyle slapStyle = slappableProp.boolValue ? greenStyle : redStyle;
            EditorGUI.LabelField(new Rect(position.x, y, 85, lineH), "Is Slappable:", slapStyle);
            EditorGUI.PropertyField(new Rect(position.x + 85, y, 20, lineH), slappableProp, GUIContent.none);

            if (slappableProp.boolValue)
            {
                Rect slapStringRect = new Rect(position.x + 110, y, position.width - 110, lineH);
                EditorGUI.PropertyField(slapStringRect, property.FindPropertyRelative("slapString"), GUIContent.none);
            }
        }

        EditorGUI.EndProperty();
    }
}