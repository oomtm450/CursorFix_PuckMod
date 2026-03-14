using HarmonyLib;
using oomtm450PuckMod_CursorFix.Configs;
using oomtm450PuckMod_CursorFix.SystemFunc;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace oomtm450PuckMod_CursorFix {
    /// <summary>
    /// Class containing the main code for the CursorFix patch.
    /// </summary>
    public class CursorFix : IPuckMod {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private const string MOD_VERSION = "1.0.0";
        #endregion

        #region Fields
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        /// <summary>
        /// Bool, true if the mod has been patched in.
        /// </summary>
        private static bool _harmonyPatched = false;

        /// <summary>
        /// InputAction, action used to toggle the cursor.
        /// </summary>
        private static InputAction _toggleCursorVisibility;

        /// <summary>
        /// DateTime, last time in UTC that _toggleCursorVisibility was fired.
        /// </summary>
        private static DateTime _lastTimeInputWasFired;

        /// <summary>
        /// Bool, true if the cursor was toggled.
        /// </summary>
        private static bool _toggled = false;
        #endregion

        #region Properties
        /// <summary>
        /// ClientConfig, config set by the client.
        /// </summary>
        internal static ClientConfig ClientConfig { get; set; } = new ClientConfig();
        #endregion

        /// <summary>
        /// Class that patches the Update event from PlayerInput.
        /// </summary>
        [HarmonyPatch(typeof(PlayerInput), "Update")]
        public class PlayerInput_Update_Patch {
            [HarmonyPrefix]
            public static bool Prefix() {
                try {
                    if (!Application.isFocused)
                        return true;

                    UIChat chat = UIChat.Instance;

                    if (chat.IsFocused)
                        return true;

                    if (_toggleCursorVisibility.WasPressedThisFrame()) {
                        DateTime now = DateTime.UtcNow;
                        if (_lastTimeInputWasFired + TimeSpan.FromMilliseconds(200) > now)
                            return true;

                        _lastTimeInputWasFired = now;
                        if (_toggled) {
                            UIManager.Instance.isMouseActive = false;
                            Cursor.lockState = CursorLockMode.Locked;
                            Cursor.visible = false;
                            Logging.Log("Hidden cursor.", ClientConfig);
                        }
                        else {
                            UIManager.Instance.isMouseActive = true;
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                            Logging.Log("Shown cursor.", ClientConfig);
                        }
                        _toggled = !_toggled;
                    }
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(PlayerInput_Update_Patch)} Prefix().\n{ex}", ClientConfig);
                }

                return true;
            }
        }

        /// <summary>
        /// Method that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                if (_harmonyPatched)
                    return true;

                Logging.Log($"Enabling...", ClientConfig, true);

                _harmony.PatchAll();
                
                Logging.Log("Setting client sided config.", ClientConfig, true);
                ClientConfig = ClientConfig.ReadConfig();

                _toggleCursorVisibility = new InputAction(binding: $"<keyboard>/#({ClientConfig.ToggleCursorKey})");
                _toggleCursorVisibility.Enable();

                _toggled = false;

                Logging.Log($"Enabled.", ClientConfig, true);
                _harmonyPatched = true;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to enable.\n{ex}", ClientConfig);
                return false;
            }
        }

        /// <summary>
        /// Method that launches when the mod is being disabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully disabled.</returns>
        public bool OnDisable() {
            try {
                if (!_harmonyPatched)
                    return true;

                Logging.Log($"Disabling...", ClientConfig, true);

                _harmony.UnpatchSelf();

                _toggleCursorVisibility.Disable();
                _toggleCursorVisibility.Dispose();
                _toggleCursorVisibility = null;

                _toggled = false;

                Logging.Log($"Disabled.", ClientConfig, true);

                _harmonyPatched = false;
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to disable.\n{ex}", ClientConfig);
                return false;
            }
        }
    }
}
