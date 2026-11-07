using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Dormant in normal play. Explicit command-line modes provide reproducible evidence.
public sealed class BottleFlipVerification : MonoBehaviour
{
    [Serializable] public class Check { public string name; public bool passed; public string observed; }
    [Serializable] public class Results { public string unityVersion; public string platform; public string utc; public List<Check> checks = new List<Check>(); }
    Results results = new Results();
    BottleFlipGameManager game;
    string output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "--verify-game") >= 0 || Array.IndexOf(args, "--record-demo") >= 0)
            new GameObject("Verification Runner").AddComponent<BottleFlipVerification>();
    }
    IEnumerator Start()
    {
        Application.runInBackground = true;
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "--evidence-dir");
        output = i >= 0 ? args[i + 1] : Application.persistentDataPath;
        Directory.CreateDirectory(output);
        yield return null;
        game = FindFirstObjectByType<BottleFlipGameManager>();
        if (Array.IndexOf(args, "--record-demo") >= 0) { yield return Record(); yield break; }
        results.unityVersion = Application.unityVersion;
        results.platform = SystemInfo.operatingSystem + " / " + SystemInfo.processorType;
        results.utc = DateTime.UtcNow.ToString("o");
        CheckThat("Initial state", game.Score == 0 && game.CurrentPlatform == 0 && game.Bottle.CanLaunch, "Score=" + game.Score + ", platform=" + game.CurrentPlatform);
        float[] holds = {0f, .18f, .75f, 1.35f, 4f};
        for (int run = 0; run < 10; run++)
        {
            game.Replay();
            for(int step=0; step<3; step++)
            {
                yield return UntilReady();
                game.Bottle.Launch(holds[(run + step) % holds.Length], new Vector2((run % 2 == 0 ? 1 : -1) * 300, 0));
                float start = Time.time;
                while (game.Attempts < step + 1 && Time.time-start < 6f) yield return null;
                CheckThat("Course " + (run+1) + " landing " + (step+1), game.Score == (step+1)*100 && game.CurrentPlatform == step+1, "Score="+game.Score+", platform="+game.CurrentPlatform+", hold="+holds[(run+step)%holds.Length]);
            }
            CheckThat("Course " + (run+1) + " completion", game.CourseComplete && game.Score == 300 && game.Attempts == 3, "Complete="+game.CourseComplete+", attempts="+game.Attempts);
        }
        game.Bottle.Launch(); yield return new WaitForSeconds(.3f);
        CheckThat("Launch after completion is ignored", game.Score == 300 && game.Attempts == 3, "Score="+game.Score+", attempts="+game.Attempts);
        game.Replay();
        CheckThat("Replay clears progress", game.Score == 0 && game.Attempts == 0 && game.CurrentPlatform == 0 && game.Bottle.CanLaunch, "Score="+game.Score+", attempts="+game.Attempts);
        game.Bottle.Launch(); game.Bottle.Launch(4f); game.Bottle.Launch(0f);
        yield return UntilReady();
        CheckThat("Repeated launch calls count once", game.Score == 100 && game.Attempts == 1, "Score="+game.Score+", attempts="+game.Attempts);
        game.Bottle.Launch(); yield return new WaitForSeconds(.2f); game.Replay(); yield return new WaitForSeconds(1.8f);
        CheckThat("Replay during flight cancels old outcome", game.Score == 0 && game.Attempts == 0 && game.Bottle.CanLaunch, "Score="+game.Score+", attempts="+game.Attempts);
        game.Bottle.Launch(); yield return UntilReady();
        game.Bottle.Launch();
        game.Bottle.Body.position = new Vector3(6f, 3f, 3.25f); game.Bottle.Body.linearVelocity = Vector3.down * 8f;
        float mark = Time.time;
        while (game.Attempts < 2 && Time.time-mark < 6f) yield return null;
        yield return new WaitForSeconds(1f);
        CheckThat("Miss retains checkpoint and score", game.Score == 100 && game.CurrentPlatform == 1 && game.Attempts == 2 && game.Bottle.CanLaunch, "Score="+game.Score+", platform="+game.CurrentPlatform+", attempts="+game.Attempts);
        game.Bottle.Launch(); yield return UntilReady();
        CheckThat("Successful retry advances", game.Score == 200 && game.CurrentPlatform == 2, "Score="+game.Score+", platform="+game.CurrentPlatform);
        game.Replay(); game.Bottle.Launch(); game.Bottle.Body.position = new Vector3(50, 100, 50); game.Bottle.Body.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(5.2f);
        CheckThat("Off-course timeout returns to start", game.Score == 0 && game.Attempts == 1 && game.CurrentPlatform == 0 && game.Bottle.CanLaunch, "Score="+game.Score+", attempts="+game.Attempts);
        File.WriteAllText(Path.Combine(output,"verification.json"), JsonUtility.ToJson(results,true));
        bool passed = results.checks.TrueForAll(c => c.passed);
        Debug.Log("BOTTLEFLIP_VERIFICATION " + results.checks.Count + " checks; passed="+passed);
        Application.Quit(passed ? 0 : 2);
    }
    IEnumerator UntilReady() { float start = Time.time; while (!game.Bottle.CanLaunch && !game.CourseComplete && Time.time-start < 7f) yield return null; }
    void CheckThat(string name, bool pass, string observed) { results.checks.Add(new Check{name=name,passed=pass,observed=observed}); Debug.Log(name+": "+pass+" "+observed); }
    IEnumerator Record()
    {
        Time.captureFramerate = 24;
        QualitySettings.vSyncCount = 0;
        int total = 35*24;
        for (int frame=0; frame<total; frame++)
        {
            if(frame==24*4 || frame==24*8 || frame==24*12 || frame==24*22 || frame==24*26 || frame==24*30) game.Bottle.Launch(.18f);
            if(frame==24*18) game.Replay();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"frame_"+frame.ToString("D5")+".png"));
        }
        yield return null;
        Application.Quit();
    }
}
