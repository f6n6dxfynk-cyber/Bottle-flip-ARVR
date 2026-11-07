using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BottleFlipFinalBuild
{
    [MenuItem("Bottle Flip/Build macOS Demo")]
    public static void Build()
    {
        BottleFlipSceneBuilder.Validate();
        PlayerSettings.productName = "Bottle Flip VR";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.macOS.buildNumber = "2";
        string outputFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Builds"));
        System.IO.Directory.CreateDirectory(outputFolder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[]{"Assets/Scenes/BottleFlipVR.unity"},
            locationPathName = System.IO.Path.Combine(outputFolder, "BottleFlipVR.app"),
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        if(report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("FINAL_BUILD_OK "+report.summary.totalSize);
    }
}
