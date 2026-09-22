using UnityEngine;

namespace qoooo.Foundation.Runtime
{
    [DefaultExecutionOrder(-10000)]
    // Unity startup時にprocess frame-rate policyを適用する。visual stateは所有しない。
    public class FrameRateController : MonoBehaviour
    {
        private const int TargetFrameRate = 60;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
