using Dread.Systems.Core;
using UnityEngine;

namespace Dread.Systems.UI
{
    /// <summary>
    /// Reusable cursor + local-player input capture for modal Dread panels (UI-1).
    /// While captured, the cursor is unlocked and visible and the local player's
    /// look/move input is suppressed via <see cref="PlayerInputLockCompat"/>.
    ///
    /// The game re-asserts cursor lock every frame, so callers must invoke
    /// <see cref="Maintain"/> from Update/OnGUI while the panel is open. State is
    /// saved on first <see cref="Capture"/> and restored on <see cref="Release"/>,
    /// so nested or repeated calls are safe.
    /// </summary>
    internal sealed class DreadInputCapture
    {
        private bool _cursorCaptured;
        private bool _inputLocked;
        private CursorLockMode _savedLockState;
        private bool _savedCursorVisible;

        /// <summary>True between <see cref="Capture"/> and <see cref="Release"/>.</summary>
        public bool Active => _cursorCaptured || _inputLocked;

        /// <summary>Save cursor state, unlock the cursor, and lock player input.</summary>
        public void Capture()
        {
            Maintain();
            LockPlayerInput(true);
            _inputLocked = true;
        }

        /// <summary>
        /// Re-assert the unlocked cursor. Cheap to call every frame; saves the
        /// pre-capture cursor state once on the first call.
        /// </summary>
        public void Maintain()
        {
            if (!_cursorCaptured)
            {
                _savedLockState = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                _cursorCaptured = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Restore the saved cursor state and release the player input lock.</summary>
        public void Release()
        {
            if (_cursorCaptured)
            {
                Cursor.lockState = _savedLockState;
                Cursor.visible = _savedCursorVisible;
                _cursorCaptured = false;
            }

            if (_inputLocked)
            {
                LockPlayerInput(false);
                _inputLocked = false;
            }
        }

        private static void LockPlayerInput(bool locked)
        {
            var pc = PlayerController.instance;
            if ((object)pc == null)
                return;

            PlayerInputLockCompat.SetLocked(pc, locked);
        }
    }
}
