using UnityEngine;

public static class Sound
{
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || Camera.main == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
    }
}
