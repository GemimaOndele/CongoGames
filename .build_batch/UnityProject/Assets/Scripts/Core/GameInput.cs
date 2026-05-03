using UnityEngine;
using UnityEngine.InputSystem;

namespace CongoGames.Core
{
    /// <summary>
    /// Accès clavier / pointeur via le <b>Input System</b> (ne pas utiliser l’ancien <see cref="Input"/>).
    /// </summary>
    public static class GameInput
    {
        public static bool DigitKeyDown1To9(int oneTo9)
        {
            if (Keyboard.current == null) return false;
            Keyboard k = Keyboard.current;
            return oneTo9 switch
            {
                1 => k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame,
                2 => k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame,
                3 => k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame,
                4 => k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame,
                5 => k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame,
                6 => k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame,
                7 => k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame,
                8 => k.digit8Key.wasPressedThisFrame || k.numpad8Key.wasPressedThisFrame,
                9 => k.digit9Key.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame,
                _ => false
            };
        }

        /// <summary>Touche 1–9 (clavier + pavé) pour raccourci mode, index 0 = touche 1, …, 8 = touche 9.</summary>
        public static bool TryGetModeSlotKey0To8Down(out int index)
        {
            for (int i = 0; i < 9; i++)
            {
                if (DigitKeyDown1To9(i + 1))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public static bool F9Down()
        {
            return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
        }

        public static bool F10Down()
        {
            return Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame;
        }

        /// <summary>Clic / premier pointeur (déverrouillage audio WebGL, etc.).</summary>
        public static bool AnyPrimaryPointerDown()
        {
            return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
        }
    }
}
