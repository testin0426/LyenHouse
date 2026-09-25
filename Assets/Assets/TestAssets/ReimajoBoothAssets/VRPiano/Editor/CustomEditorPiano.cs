using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ReimajoBoothAssets.Piano), true, isFallback = true)]
[CanEditMultipleObjects]
public class CustomEditorPiano : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.magenta;
        EditorGUILayout.LabelField("Reimajo Booth Asset from https://reimajo.booth.pm/", style);
        GUILayout.BeginHorizontal("Links", GUILayout.Height(25));
        if (GUILayout.Button("Open Documentation", GUILayout.Height(25f)))
        {
            Application.OpenURL(@"https://docs.google.com/document/d/1C3hytJVUq4KOV5jaunDQ2tDoiWWcwUveiGhmaaT3JiY/");
        }
        if (GUILayout.Button("Open Webshop", GUILayout.Height(25f)))
        {
            Application.OpenURL(@"https://reimajo.booth.pm/");
        }
        GUILayout.EndHorizontal();
        DrawUILine(Color.gray);
        Color cachedGuiColor = GUI.color;
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool isVisible = property.NextVisible(true);
        if (isVisible)
            do
            {
                GUI.color = cachedGuiColor;
                this.HandleProperty(property);
            } while (property.NextVisible(false));
        serializedObject.ApplyModifiedProperties();
    }

    protected void HandleProperty(SerializedProperty property)
    {
        bool isdefaultScriptProperty = property.name.Equals("m_Script") && property.type.Equals("PPtr<MonoScript>") && property.propertyType == SerializedPropertyType.ObjectReference && property.propertyPath.Equals("m_Script");
        if (isdefaultScriptProperty)
            return;
        EditorGUILayout.PropertyField(property, property.isExpanded);
    }

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        EditorGUILayout.Space();
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        rect.height = thickness;
        rect.y += padding / 2;
        rect.x -= 2;
        rect.width += 6;
        EditorGUI.DrawRect(rect, color);
        EditorGUILayout.Space();
    }
}
