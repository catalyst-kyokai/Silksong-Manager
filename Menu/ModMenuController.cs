using UnityEngine;

namespace SilksongManager.Menu
{
    /// <summary>
    /// Controller attached to the mod menu screen to handle input (Escape key).
    /// </summary>
    public class ModMenuController : MonoBehaviour
    {
        private bool _isActive = false;

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        void Update()
        {
            if (!_isActive) return;

            // Check for Escape key
            // Using direct Unity input since HeroActions API may vary
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Plugin.Log.LogInfo("Escape pressed in mod menu, returning to main menu");
                MainMenuHook.HandleBackPressed();
            }
        }
    }
}
