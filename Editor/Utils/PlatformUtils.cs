using UnityEngine;

namespace GraphViewBase {
    public static class PlatformUtils {
        public static readonly bool IsMac
            = Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;
    }
}