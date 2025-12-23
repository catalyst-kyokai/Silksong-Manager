using UnityEngine;

namespace SilksongManager.Menu
{
    /// <summary>
    /// Controller attached to the mod menu screen to handle input.
    /// Handles Escape key to return to main menu.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class ModMenuController : MonoBehaviour
    {
        #region Fields

        /// <summary>Whether this controller is currently active.</summary>
        private bool _isActive = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the active state of this controller.
        /// </summary>
        /// <param name="active">Whether to activate or deactivate.</param>
        public void SetActive(bool active)
        {
            Plugin.Log.LogInfo($"ModMenuController SetActive({active}) called on instance {this.GetHashCode()}");
            _isActive = active;
        }

        #endregion

        #region Unity Lifecycle

        void Update()
        {
            if (!_isActive) return;

            if (Keybinds.ModKeybindsScreen.IsActive) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Plugin.Log.LogInfo($"Escape pressed in mod menu (instance {this.GetHashCode()}), returning to main menu");
                MainMenuHook.HandleBackPressed();
            }
        }

        #endregion
    }
}
