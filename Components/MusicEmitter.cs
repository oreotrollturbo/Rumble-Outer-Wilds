using System;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class MusicEmitter: MonoBehaviour
{
    public MusicEmitter(IntPtr ptr) : base(ptr) {}

    public string musicFileName;
    public bool isEnabled = true;
    private AudioManager.ClipData clipData;
    private float maxVolume = 1f;


    void Start()
    {
        clipData = AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, musicFileName), 0, true);
    }

    public void SetVolume(float volume)
    {
        if (clipData == null) return;
        
        if (!isEnabled) volume = 0f;

        // Always silence if inactive, regardless of requested volume
        float target = gameObject.activeSelf
            ? Mathf.Clamp(volume, 0f, maxVolume)
            : 0f;

        // Use a tighter threshold so near-zero volumes still get zeroed out
        if (Mathf.Abs(clipData.Reader.Volume - target) > 0.001f)
        {
            AudioManager.ChangeVolume(clipData, target);
        }
    }

    void OnDisable()
    {
        // Immediately silence when the GameObject is turned off
        if (clipData != null)
            AudioManager.ChangeVolume(clipData, 0f);
    }

    void OnDestroy()
    {
        // Full cleanup when the component is removed
        AudioManager.StopPlayback(clipData);
        clipData = null;
    }
}