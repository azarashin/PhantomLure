using UnityEngine;

namespace PhantomLure.Mono.DebugGroup
{

    public class DebugSettingHotkey : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                DebugSetting.SetEnabled(!DebugSetting.IsEnabled());
            }
        }
    }
}