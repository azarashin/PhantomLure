using UnityEngine;
using UnityEngine.InputSystem;

namespace PhantomLure.Mono.DebugGroup
{

    public class DebugSettingHotkey : MonoBehaviour
    {
        void Update()
        {
            // キーボード未接続などの安全対策
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                DebugSetting.SetEnabled(!DebugSetting.IsEnabled());
            }
        }
    }
}