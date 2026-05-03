using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppRUMBLE.Interactions.InteractionBase;
using RumbleModdingAPI.RMAPI;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class QuantumTapeRecorder : MonoBehaviour
{
    public QuantumTapeRecorder(IntPtr ptr) : base(ptr) { }

    public class QuantumState
    {
        public Vector3 position;
        public Quaternion rotation;
        public string soundName;

        public QuantumState(Vector3 pos, Quaternion rot, string sound)
        {
            position = pos;
            rotation = rot;
            soundName = sound;
        }
    }
    
    private Transform wheel1;
    private Transform wheel2;

    // Spinning control
    private bool isSpinning = false;
    public float wheel1SpinSpeed = 30f;
    public float wheel2SpinSpeed = 120f;
    public Vector3 wheelRotationAxis = new Vector3(0,0,1f);

    private AudioManager.ClipData activeClip;
    private object playbackEndToken; // coroutine token to cancel when stopping

    private QuantumObject qObject;
    public List<QuantumState> quantumStates = new List<QuantumState>();

    void Start()
    {
        wheel1 = transform.GetChild(0).GetChild(0).GetChild(0);
        wheel2 = transform.GetChild(0).GetChild(0).GetChild(1);
        
        DontDestroyOnLoad(gameObject);
        Actions.onMapInitialized += SceneLoaded;
        
        transform.position = new Vector3(-37.9188f, 8.8919f, -14.7885f);
        transform.rotation = Quaternion.Euler(0, 24.6467f, 0);

        qObject = gameObject.AddComponent<QuantumObject>();

        ButtonWithLabel startButton = new ButtonWithLabel(Vector3.zero, "Play", "PlayButton", transform);
        ButtonWithLabel stopButton = new ButtonWithLabel(Vector3.zero, "Stop", "StopButton", transform);

        startButton.button.transform.localScale = new Vector3(0.25f,0.25f,0.25f);
        stopButton.button.transform.localScale = new Vector3(0.25f,0.25f,0.25f);
        
        startButton.button.transform.localPosition = new Vector3(0.145f, 0.0355f, 0.14f);
        startButton.button.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        stopButton.button.transform.localPosition = new Vector3(0.009f, 0.0355f, 0.14f);
        stopButton.button.transform.localRotation = Quaternion.Euler(90f, 0, 0);

        startButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => PlayRecording()));
        stopButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => StopRecording()));

        SetupQuantumPositions();
    }

    private void SetupQuantumPositions()
    {
        quantumStates.Add(new QuantumState(new Vector3(-37.9188f, 8.8919f, -14.7885f), Quaternion.Euler(0, 24.6467f, 0), "ow_audio1.wav"));
        quantumStates.Add(new QuantumState(new Vector3(-16.1541f, -0.4519f, 4.4417f), Quaternion.Euler(300f, 169.4723f, 0), "ow_audio2.wav"));
        quantumStates.Add(new QuantumState(new Vector3(9.8858f, -3.3028f, -12.2372f), Quaternion.Euler(0, 222.0513f, 0), "ow_audio3.wav"));
        quantumStates.Add(new QuantumState(new Vector3(11.1949f, 0.3814f, 5.4248f), Quaternion.Euler(300.818f, 129.545f, 0), "ow_audio4.wav"));

        qObject.teleportPositions = new Dictionary<Vector3, Quaternion>();
        foreach (var state in quantumStates)
            qObject.teleportPositions.Add(state.position, state.rotation);
    }

    private void PlayRecording()
    {
        // Stop any currently playing audio and its end-of-playback timer
        StopCurrentPlayback();

        // Determine which quantum state we're in
        QuantumState currentState = null;
        Vector3 currentPos = transform.position;
        foreach (var state in quantumStates)
        {
            if (Vector3.Distance(currentPos, state.position) < 0.1f)
            {
                currentState = state;
                break;
            }
        }

        if (currentState == null)
        {
            MelonLogger.Warning("QuantumTapeRecorder: Not in a valid quantum state position!");
            return;
        }

        string audioPath = Path.Combine(Main.folderPath, currentState.soundName);
        if (!File.Exists(audioPath))
        {
            MelonLogger.Error($"Audio file not found: {audioPath}");
            return;
        }

        // Start playback and store the clip
        activeClip = AudioManager.PlaySoundIfFileExists(audioPath);
        if (activeClip == null || activeClip.Reader == null)
        {
            MelonLogger.Error("Failed to start audio playback.");
            return;
        }

        // Lock teleportation, start spinning
        qObject.canTeleport = false;
        isSpinning = true;
        MelonLogger.Msg($"Playing quantum recording: {currentState.soundName}");

        // Schedule end of playback based on the audio file length
        float duration = (float)activeClip.Reader.TotalTime.TotalSeconds;
        playbackEndToken = MelonCoroutines.Start(WaitForAudioEnd(duration));
    }

    private IEnumerator WaitForAudioEnd(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        // Only re-enable teleportation if this same clip hasn't been stopped or replaced
        qObject.canTeleport = true;
        isSpinning = false;
        activeClip = null;
    }

    private void StopCurrentPlayback()
    {
        if (activeClip != null)
        {
            AudioManager.StopPlayback(activeClip);
            activeClip = null;
        }
        if (playbackEndToken != null)
        {
            MelonCoroutines.Stop(playbackEndToken);
            playbackEndToken = null;
        }
        qObject.canTeleport = true;
        isSpinning = false;
    }

    private void StopRecording()
    {
        StopCurrentPlayback();
    }

    private void OnDestroy()
    {
        StopCurrentPlayback(); // clean up if the object is destroyed
    }

    private void Update()
    {
        if (isSpinning)
        {
            wheel1.Rotate(wheelRotationAxis, wheel1SpinSpeed * Time.deltaTime);
            wheel2.Rotate(wheelRotationAxis, wheel2SpinSpeed * Time.deltaTime);
        }
    }

    private void SceneLoaded(string mapName)
    {
        gameObject.SetActive(mapName == "Gym");

        if (activeClip != null)
        {
            AudioManager.StopPlayback(activeClip);
        }
    }
}