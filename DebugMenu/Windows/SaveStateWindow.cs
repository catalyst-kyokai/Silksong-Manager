using UnityEngine;
using SilksongManager.SaveState;
using System.Collections.Generic;

namespace SilksongManager.DebugMenu.Windows
{
    public class SaveStateWindow : BaseWindow
    {
        public override int WindowId => 10011;
        public override string Title => "Save States";
        protected override Vector2 DefaultSize => new Vector2(350, 500);

        private string _newStateName = "";
        private Vector2 _scrollPosition;

        protected override void DrawContent()
        {
            // Create New State Section
            DebugMenuStyles.DrawSectionHeader("CREATE NEW STATE");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", DebugMenuStyles.Label, GUILayout.Width(50));
            _newStateName = GUILayout.TextField(_newStateName, DebugMenuStyles.TextField);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save Current State", DebugMenuStyles.Button))
            {
                SaveStateManager.CaptureState(_newStateName);
                _newStateName = ""; // Reset field
            }

            GUILayout.Space(10);
            
            // List Section
            DebugMenuStyles.DrawSectionHeader("SAVED STATES");

            var states = SaveStateManager.GetStates();
            if (states.Count == 0)
            {
                GUILayout.Label("No saved states found.", DebugMenuStyles.LabelCentered);
            }
            else
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                // Iterate backwards to show newest first
                for (int i = states.Count - 1; i >= 0; i--)
                {
                    DrawStateRow(states[i]);
                    GUILayout.Space(5);
                }

                GUILayout.EndScrollView();
            }
        }

        private void DrawStateRow(SaveStateData state)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Header: Name and Time
            GUILayout.BeginHorizontal();
            GUILayout.Label(state.GetDisplayName(), DebugMenuStyles.LabelBold);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Info: Scene, Health, Geo
            GUILayout.Label($"Scene: {state.SceneName}", DebugMenuStyles.LabelSmall);
            GUILayout.Label($"HP: {state.Health}/{state.MaxHealth}  Silk: {state.Silk}  Geo: {state.Geo}", DebugMenuStyles.LabelSmall);

            // Buttons
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Load", DebugMenuStyles.Button))
            {
                SaveStateManager.LoadState(state);
            }

            if (GUILayout.Button("Delete", DebugMenuStyles.Button, GUILayout.Width(60)))
            {
                // Confirmation could be added here, but for now instant delete
                SaveStateManager.DeleteState(state);
            }

            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
    }
}
