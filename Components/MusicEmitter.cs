using System;
using System.Collections;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class MusicEmitter : MonoBehaviour
{
    public MusicEmitter(IntPtr ptr) : base(ptr) {}

    public string musicFileName;
    public bool isEnabled = true;
    private AudioManager.ClipData clipData;
    private float maxVolume = 1f;
    
    public float detectionAngle = 8f;

    // LE sync
    private static readonly List<MusicEmitter> pendingSync = new();
    private static bool syncCoroutineRunning = false;

    void Start()
    {
        clipData = AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, musicFileName), 0f, true);

        if (clipData != null)
            pendingSync.Add(this);
        
        if (!syncCoroutineRunning)
        {
            syncCoroutineRunning = true;
            MelonCoroutines.Start(SyncAllEmitters());
        }
    }

    /// <summary>
    /// Waits one frame (so every emitters Start() has fired), then resets
    /// all readers to position 0 in the same update — perfect sync.
    /// </summary>
    private static IEnumerator SyncAllEmitters()
    {
        yield return null;
        
        foreach (MusicEmitter emitter in pendingSync)
        {
            if (emitter?.clipData?.Reader != null)
                emitter.clipData.Reader.Position = 0;
        }

        MelonLogger.Msg($"[MusicEmitter] Synced {pendingSync.Count} emitters.");

        pendingSync.Clear();
        syncCoroutineRunning = false;
    }

    public void SetVolume(float volume)
    {
        if (clipData == null) return;

        if (!isEnabled) volume = 0f;

        float target = gameObject.activeSelf
            ? Mathf.Clamp(volume, 0f, maxVolume)
            : 0f;

        if (Mathf.Abs(clipData.Reader.Volume - target) > 0.001f)
            AudioManager.ChangeVolume(clipData, target);
    }

    void OnDisable()
    {
        if (clipData != null)
            AudioManager.ChangeVolume(clipData, 0f);
    }

    void OnDestroy()
    {
        pendingSync.Remove(this); //remove if destroyed before sync fires
        AudioManager.StopPlayback(clipData);
        clipData = null;
    }
}