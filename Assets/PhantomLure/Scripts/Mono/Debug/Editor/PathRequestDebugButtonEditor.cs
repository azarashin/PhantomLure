using PhantomLure.Debugging;
using UnityEditor;
using UnityEngine;

namespace PhantomLure.Debugging.NSEditor
{
    [CustomEditor(typeof(PathRequestDebugButton))]
    public class PathRequestDebugButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var button = (PathRequestDebugButton)target;

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Send Path Request"))
            {
                button.SendPathRequest();
            }
            GUI.enabled = true;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play中にボタンを押してください。",
                    MessageType.Info);
            }
        }
    }
}