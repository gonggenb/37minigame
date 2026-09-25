using UnityEngine;

namespace WuxiaRoguelite.Runtime
{
    public static class WebMobileRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL starts at the project's mobile quality tier; avoid native-platform changes.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 20f);
            QualitySettings.shadowCascades = 0;
#endif
        }
    }
}
