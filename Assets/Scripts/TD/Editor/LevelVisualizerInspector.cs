#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TD.Core;

namespace TD.Editor
{
    [CustomEditor(typeof(LevelVisualizer))]
    public class LevelVisualizerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var vis = (LevelVisualizer)target;

            EditorGUILayout.LabelField("Config", EditorStyles.boldLabel);
            vis.levelId = EditorGUILayout.TextField("Level Id", vis.levelId);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fallback Colors", EditorStyles.boldLabel);
            vis.pathColor = EditorGUILayout.ColorField("Path Color", vis.pathColor);
            vis.slotColor = EditorGUILayout.ColorField("Slot Color", vis.slotColor);

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Now"))
            {
                vis.RefreshValidation();
            }

            var sum = vis.Summary;
            if (sum != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation Summary", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Level", sum.levelId);
                EditorGUILayout.LabelField("Elements/Towers/Enemies", $"{sum.elementsCount} / {sum.towersCount} / {sum.enemiesCount}");
                EditorGUILayout.LabelField("Path/Slots/Waves", $"{sum.pathsCount} / {sum.buildSlotsCount} / {sum.wavesCount}");
                EditorGUILayout.LabelField("Issues", sum.issuesCount.ToString());
                EditorGUILayout.LabelField("Last Checked", sum.lastCheckedAt);
            }

            var issues = vis.Issues;
            if (issues != null && issues.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);
                foreach (var i in issues)
                {
                    EditorGUILayout.HelpBox(i, MessageType.Error);
                }
            }
        }
    }
}
#endif
