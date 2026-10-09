using UnityEngine;

public class AudioVolumeChannel : MonoBehaviour
{
    public enum Bus { Bgm, Effects, Voice }
    public AudioSource source;
    public Bus bus;
    float originalVolume = 1f;

    public void Configure(AudioSource audioSource, Bus channel)
    {
        source = audioSource;
        bus = channel;
        originalVolume = source.volume;
        Apply();
    }

    void Update() { Apply(); }
    void Apply()
    {
        if (source != null)
            source.volume = originalVolume * (bus == Bus.Bgm ? GameSettings.Bgm : bus == Bus.Voice ? GameSettings.Voice : GameSettings.Effects);
    }
}
