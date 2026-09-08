using System.Runtime.InteropServices;

namespace Game.Client.Common
{
    public static class WebPointerInput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void GamePointerArm(int enabled);
#endif
        public static void Arm(bool enabled)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GamePointerArm(enabled ? 1 : 0);
#endif
        }
    }
}
