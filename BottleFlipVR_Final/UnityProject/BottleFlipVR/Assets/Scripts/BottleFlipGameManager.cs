using System.Collections;
using UnityEngine;

public sealed class BottleFlipGameManager : MonoBehaviour
{
    [SerializeField] private BottleFlipController bottle;
    [SerializeField] private Transform[] platforms;
    private int platformIndex, score, attempts;
    private string message = "Tap SPACE or click to flip";
    private bool courseComplete;
    public int Score => score;
    public int CurrentPlatform => platformIndex;
    public int Attempts => attempts;
    public bool CourseComplete => courseComplete;
    public BottleFlipController Bottle => bottle;

    private void Start()
    {
        if (bottle == null || platforms == null || platforms.Length < 2) return;
        bottle.OutcomeResolved += HandleOutcome;
        Replay();
    }

    private void OnDestroy()
    {
        if (bottle != null) bottle.OutcomeResolved -= HandleOutcome;
    }

    private void HandleOutcome(BottleFlipController.Outcome outcome)
    {
        if (courseComplete) return;
        attempts++;
        if (outcome == BottleFlipController.Outcome.Land)
        {
            score += 100;
            platformIndex++;
            courseComplete = platformIndex >= platforms.Length - 1;
            message = courseComplete ? "COURSE COMPLETE!  Press R to replay" : "Landed!  Getting ready for the next flip...";
            if (courseComplete) bottle.ClearTarget();
            else StartCoroutine(PrepareAfterDelay(.6f));
        }
        else
        {
            message = "Try again - your checkpoint is saved";
            StartCoroutine(PrepareAfterDelay(.8f));
        }
    }

    private IEnumerator PrepareAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetBottleOnPlatform(platformIndex);
        message = bottle.EasyLanding ? "Tap SPACE or click - landing is automatic" : "Hold, adjust your spin, then release";
    }

    private void SetBottleOnPlatform(int index)
    {
        Collider pad = platforms[index].GetComponent<Collider>();
        bottle.SetSpawn(bottle.SurfaceSpawn(pad), Quaternion.identity);
        if (index + 1 < platforms.Length) bottle.SetTarget(platforms[index + 1].GetComponent<Collider>());
        else bottle.ClearTarget();
    }

    public void Replay()
    {
        StopAllCoroutines(); // Cancels an old retry/advance that otherwise moves a restarted bottle.
        score = attempts = platformIndex = 0;
        courseComplete = false;
        SetBottleOnPlatform(0);
        message = "Tap SPACE or click anywhere outside this panel";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Replay();
    }

    private static float HudScale => Mathf.Max(.25f, Mathf.Min(Screen.width / 1200f, Screen.height / 760f));
    private static Rect HudRect => new Rect(20f, 20f, 455f, 235f);
    public static bool PointerOverHud()
    {
        Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / HudScale;
        return HudRect.Contains(point);
    }

    private void OnGUI()
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * HudScale);
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        GUIStyle text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, normal = { textColor = Color.white } };
        GUIStyle accent = new GUIStyle(text) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(.35f, 1f, .8f) } };
        GUI.color = new Color(.035f, .07f, .14f, .96f);
        GUI.DrawTexture(HudRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(40, 34, 420, 34), bottle != null && bottle.EasyLanding ? "BOTTLE FLIP  /  EASY" : "BOTTLE FLIP", title);
        GUI.Label(new Rect(40, 73, 420, 28), $"Score  {score:000}     Platform  {platformIndex + 1}/{(platforms == null ? 0 : platforms.Length)}", text);
        GUI.Label(new Rect(40, 109, 412, 50), message, accent);
        GUI.Label(new Rect(40, 165, 412, 26), bottle != null && bottle.EasyLanding ? "Auto aim + auto spin + automatic landing" : "Space / mouse: hold, then release", text);
        if (bottle != null && bottle.IsCharging)
        {
            GUI.color = new Color(.1f, .3f, .4f);
            GUI.DrawTexture(new Rect(40, 207, 265, 14), Texture2D.whiteTexture);
            GUI.color = new Color(.2f, .9f, 1f);
            GUI.DrawTexture(new Rect(40, 207, 265 * bottle.Charge01, 14), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else GUI.Label(new Rect(40, 202, 260, 30), bottle == null ? "" : bottle.State, text);
        if (GUI.Button(new Rect(338, 199, 112, 34), "Replay (R)")) Replay();
        GUI.matrix = oldMatrix;
    }
}
