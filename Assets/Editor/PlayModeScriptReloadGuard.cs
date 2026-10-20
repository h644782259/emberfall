using UnityEditor;
namespace Emberfall.Editor
{
    // Runtime services and active combats cannot survive a hot domain reload.
    // Defer changed assemblies until Play stops, keeping live tests intact.
    [InitializeOnLoad]
    internal static class PlayModeScriptReloadGuard
    {
        private static bool locked;
        static PlayModeScriptReloadGuard()
        {
            EditorApplication.playModeStateChanged+=Changed;
            EditorApplication.quitting+=Release;
            if(EditorApplication.isPlaying)Acquire();
        }
        private static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)Acquire();
            if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)Release();
        }
        private static void Acquire(){if(locked)return;EditorApplication.LockReloadAssemblies();locked=true;}
        private static void Release(){if(!locked)return;locked=false;EditorApplication.UnlockReloadAssemblies();}
    }
}
