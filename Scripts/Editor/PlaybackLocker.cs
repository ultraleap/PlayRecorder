using UnityEngine;
using UnityEditor;

// This class is used to prevent entering playmode while the manager refreshes the loaded files.
// ensure class initializer is called whenever scripts recompile
[InitializeOnLoadAttribute]
public static class PlaybackLocker
{

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorApplication.isPlaying = false;
            Debug.LogError("Play mode is locked as the Playback Manager is either awaiting a file refresh or currently working.");
        }
    }

    public static void SetPlayModeEnabled(bool enabled)
    {
        EditorApplication.playModeStateChanged -= LogPlayModeState;

        if (!enabled)
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }
    }
}