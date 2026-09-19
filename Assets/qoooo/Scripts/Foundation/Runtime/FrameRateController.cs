using UnityEngine;

namespace qoooo.Foundation.Runtime
{
    [DefaultExecutionOrder(-10000)]
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
