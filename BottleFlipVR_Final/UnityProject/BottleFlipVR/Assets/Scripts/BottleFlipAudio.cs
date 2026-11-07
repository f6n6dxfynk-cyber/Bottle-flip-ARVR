using UnityEngine;

/// <summary>
/// Small procedural feedback tones keep the prototype self-contained while preserving the synopsis' audio cues.
/// </summary>
public sealed class BottleFlipAudio : MonoBehaviour
{
    private static BottleFlipAudio instance;
    private AudioSource source;
    private AudioClip launchClip;
    private AudioClip landClip;
    private AudioClip failClip;

    private void Awake()
    {
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        launchClip = Tone("Launch", 360f, 0.12f, 0.12f);
        landClip = Tone("Land", 740f, 0.2f, 0.2f);
        failClip = Tone("Fail", 160f, 0.28f, 0.18f);
    }

    public static void PlayLaunch(Vector3 position)
    {
        if (instance != null) instance.Play(instance.launchClip, position, 0.65f);
    }

    public static void PlayOutcome(BottleFlipController.Outcome outcome, Vector3 position)
    {
        if (instance == null) return;
        instance.Play(outcome == BottleFlipController.Outcome.Land ? instance.landClip : instance.failClip, position, 0.85f);
    }

    private void Play(AudioClip clip, Vector3 position, float volume)
    {
        source.transform.position = position;
        source.PlayOneShot(clip, volume);
    }

    private static AudioClip Tone(string name, float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float envelope = 1f - (i / (float)samples);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * volume;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
