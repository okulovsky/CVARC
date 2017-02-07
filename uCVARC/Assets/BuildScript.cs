﻿
#if UNITY_EDITOR

using UnityEditor;

class BuildScript
{
    static void PerformBuild()
    {
        var scenes = new[] { "Assets/Intro.unity", "Assets/Round.unity", "Assets/LogRound.unity" };
        BuildPipeline.BuildPlayer(scenes, "..\\Binaries\\cvarc.exe", BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}

#endif