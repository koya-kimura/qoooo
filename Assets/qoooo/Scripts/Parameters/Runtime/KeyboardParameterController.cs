using qoooo.Parameters.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace qoooo.Parameters.Runtime
{
    [DefaultExecutionOrder(-80)]
    // Adapts keyboard shortcuts into owner-held OneshotParameters resolved from the registry.
    // It never calls visual consumers directly or owns duplicate Parameter state.
    public sealed class KeyboardParameterController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Key _tapKey = Key.Space;
        [SerializeField] private Key _syncBeatKey = Key.B;
        [SerializeField] private Key _syncBarKey = Key.B;
        [SerializeField] private MidiParameterRegistry _registry;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _registry == null || IsTextInputFocused()) return;

            if (keyboard[_tapKey].wasPressedThisFrame) Trigger("rhythm.tap");

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                if (keyboard[_syncBarKey].wasPressedThisFrame) Trigger("rhythm.sync-bar");
                return;
            }

            if (keyboard[_syncBeatKey].wasPressedThisFrame) Trigger("rhythm.sync-beat");
        }

        private void Trigger(string parameterId)
        {
            if (_registry.TryGet(parameterId, out var parameter) && parameter is OneshotParameter oneshot)
                oneshot.Trigger();
        }

        private bool IsTextInputFocused()
        {
            var focused = _uiDocument?.rootVisualElement?.panel?.focusController?.focusedElement as VisualElement;
            for (var element = focused; element != null; element = element.parent)
                if (element is TextInputBaseField<string>) return true;
            return false;
        }
    }
}