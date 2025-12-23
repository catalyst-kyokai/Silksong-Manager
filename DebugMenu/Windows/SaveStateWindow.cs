using UnityEngine;
using SilksongManager.SaveState;
using System.Collections.Generic;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Save state management window for creating and loading save states.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class SaveStateWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10011;
        public override string Title => "Save States";
        protected override Vector2 DefaultSize => new Vector2(350, 500);

        #endregion

        #region Private Fields

        /// <summary>Name input for new save state.</summary>
        private string _newStateName = "";
        /// <summary>Scroll position for state list.</summary>
        private Vector2 _scrollPosition;

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            DebugMenuStyles.DrawSectionHeader("CREATE NEW STATE");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", DebugMenuStyles.Label, GUILayout.Width(50));
            _newStateName = GUILayout.TextField(_newStateName, DebugMenuStyles.TextField);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save Current State", DebugMenuStyles.Button))
            {
                SaveStateManager.CaptureState(_newStateName);
                _newStateName = "";
            }

            GUILayout.Space(10);

            DebugMenuStyles.DrawSectionHeader("SAVED STATES");

            var states = SaveStateManager.GetStates();
            if (states.Count == 0)
            {
                GUILayout.Label("No saved states found.", DebugMenuStyles.LabelCentered);
            }
            else
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

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

            GUILayout.BeginHorizontal();
            GUILayout.Label(state.GetDisplayName(), DebugMenuStyles.LabelBold);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label($"Scene: {state.SceneName}", DebugMenuStyles.LabelSmall);
            GUILayout.Label($"HP: {state.Health}/{state.MaxHealth}  Silk: {state.Silk}  Geo: {state.Geo}", DebugMenuStyles.LabelSmall);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Load", DebugMenuStyles.Button))
            {
                SaveStateManager.LoadState(state);
            }

            if (GUILayout.Button("Delete", DebugMenuStyles.Button, GUILayout.Width(60)))
            {
                SaveStateManager.DeleteState(state);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        #endregion
    }
}
