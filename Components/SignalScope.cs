using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppRUMBLE.Players;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SignalScope : MonoBehaviour
{
    public Player player;
    public Transform rightHandTransform;
    public Transform beltTransform;
    public GameObject Camera;
    public GameObject Screen;

    private const float hold_distance = 0.115f;
    private const float holdThreshold = 0.9f;
    private const float releaseThreshold = 0.1f;
    
    private float currentFOV;
    
    public float zoomIncrement = 4.6f;
    
    public float minZoom = 120f;
    public float maxZoom = 1.5f;

    private float startingZoom;
    
    public float zoomedInAngleScale  = 0.0625f;
    public float zoomedOutAngleScale = 1.875f;

    private bool isHolding = false;

    private Vector3 beltLocalPosition = new Vector3(0.1f, 0f, -0.1f);
    private Quaternion beltLocalRotation = Quaternion.Euler(0, 0, 270);

    private Vector3 handLocalPosition = new Vector3(0, 0, 0.1f);
    private Quaternion handLocalRotation = Quaternion.Euler(0, 90, 0);
    
    private Vector3 zoomInButtonLocalPosition = new Vector3(-0.01f, 0.05f, -0.015f);
    private Quaternion zoomInButtonLocalRotation = Quaternion.Euler(90f, 180f, 0);
    
    private Vector3 zoomOutButtonLocalPosition = new Vector3(0.01f, 0.05f, -0.015f);
    private Quaternion zoomOutButtonLocalRotation = Quaternion.Euler(90f, 180f, 0);
    
    private Vector3 toggleCamButtonLocalPosition = new Vector3(0.056f, 0.057f, 0);
    private Quaternion toggleCamButtonLocalRotation = Quaternion.Euler(19.2368f, 90f, 0);
    
    private GameObject ZoomInButton;
    private GameObject ZoomOutButton;
    private GameObject ToggleScreenButton;

    private bool hasSetUp;
    
    Dictionary<GameObject, MusicEmitter> musicEmitters = new Dictionary<GameObject, MusicEmitter>();

    // --- VIRTUAL PARENTING VARIABLES ---
    private Vector3 scopeVelocity = Vector3.zero;
    private Quaternion scopeSmoothedRot;
    
    public float scopePositionSmoothTime = 0.05f;
    public float scopeRotationSmoothing = 15f;
    // ------------------------------------

    public bool playMusic = true;
    public bool grabDuringMatches = true;

    // --- STATIC / NO-SIGNAL SOUND ---
    // Fill this in once you have the file path — same folderPath convention as the other clips.
    public string staticSoundFileName = "signalscope_static.wav";
    public float staticVolume = 1f;
    private AudioManager.ClipData staticClip;
    // ---------------------------------

    private bool allMusicOff;

    public SignalScope(IntPtr ptr) : base(ptr) {}

    void Start()
    {
        gameObject.name = "SignalScope";
       
        Camera = transform.GetChild(0).gameObject;
        startingZoom = Camera.GetComponent<Camera>().fieldOfView;
        currentFOV = Camera.GetComponent<Camera>().fieldOfView;
        Camera.GetComponent<Camera>().farClipPlane = OwSystemSettings.ViewDistance.Value;
        Screen = gameObject.transform.GetChild(35).gameObject;

        MelonCoroutines.Start(FindPlayerAndSetup());

        SetupButtons();
        CacheMusicEmitters();
        SetupStaticSound();
    }

    void CacheMusicEmitters()
    {
        Type type = typeof(SolarSystemData);
        
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(GameObject))
            {
                GameObject go = field.GetValue(Main.solarSystem) as GameObject;

                if (go != null)
                {
                    if (go.TryGetComponent<MusicEmitter>(out MusicEmitter emitter))
                    {
                        musicEmitters.Add(go, emitter);
                    }
                }
            }
        }
    }

    void SetupStaticSound()
    {
        // Added to the mixer silent immediately, same trick MusicEmitter uses —
        // HandleMusicChange fades this in whenever no real signal is audible.
        staticClip = AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, staticSoundFileName), 0f, true);
    }

    void SetupButtons()
    {
        Action zoomInAction = () =>
        {
            currentFOV = Camera.GetComponent<Camera>().fieldOfView;
            Camera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(currentFOV - zoomIncrement, maxZoom, minZoom);
        };

        Action zoomOutAction = () =>
        {
            currentFOV = Camera.GetComponent<Camera>().fieldOfView;
            Camera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(currentFOV + zoomIncrement, maxZoom, minZoom);
        };

        Action toggleScreenAction = () =>
        {
            Screen.SetActive(!Screen.activeSelf);
        };
        
        ZoomInButton = Create.NewButton(zoomInAction);
        ZoomInButton.name = "ZoomInButton";
        
        ZoomOutButton = Create.NewButton(zoomOutAction);
        ZoomOutButton.name = "ZoomOutButton";
        
        ToggleScreenButton = Create.NewButton(toggleScreenAction);
        ToggleScreenButton.name = "ToggleScreenButton";
        
        ZoomInButton.transform.SetParent(transform, false);
        ZoomOutButton.transform.SetParent(transform, false);
        ToggleScreenButton.transform.SetParent(transform, false);
        
        ZoomInButton.transform.localPosition = zoomInButtonLocalPosition;
        ZoomOutButton.transform.localPosition = zoomOutButtonLocalPosition;
        ToggleScreenButton.transform.localPosition = toggleCamButtonLocalPosition;

        ZoomInButton.transform.localRotation = zoomInButtonLocalRotation;
        ZoomOutButton.transform.localRotation = zoomOutButtonLocalRotation;
        ToggleScreenButton.transform.localRotation = toggleCamButtonLocalRotation; 
        
        ZoomInButton.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        ZoomOutButton.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        ToggleScreenButton.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
    }

    void FixedUpdate()
    {
        if (!hasSetUp) return;
        
        float rightTrigger = Calls.ControllerMap.RightController.GetTrigger();

        if (!isHolding && rightTrigger > holdThreshold && IsHandCloseEnough(rightHandTransform.position) && (grabDuringMatches || !Main.isInMatch))
        {
            Grab();
        }
        else if (isHolding && (rightTrigger <= releaseThreshold || (!grabDuringMatches && Main.isInMatch)))
        {
            ReleaseToBelt();
        }

        HandleMusicChange();
    }

    // --- VIRTUAL PARENTING LOGIC ---
    void LateUpdate()
    {
        if (!hasSetUp || !isHolding) return;

        // Compute where the scope ideally sits relative to the hand
        Vector3 idealPos = rightHandTransform.TransformPoint(handLocalPosition);
        Quaternion idealRot = rightHandTransform.rotation * handLocalRotation;

        // Smoothly drive the scope there — camera inherits this as a plain child
        transform.position = Vector3.SmoothDamp(
            transform.position, idealPos, ref scopeVelocity, scopePositionSmoothTime);

        scopeSmoothedRot = Quaternion.Slerp(
            scopeSmoothedRot, idealRot, Time.deltaTime * scopeRotationSmoothing);

        transform.rotation = scopeSmoothedRot;
    }

    public IEnumerator FindPlayerAndSetup()
    {
        while (Calls.Players.GetLocalPlayer() == null || 
               Calls.Players.GetLocalPlayer().Controller == null || 
               Calls.Players.GetLocalPlayer().Controller.PlayerVisuals == null)
        {
            if (this == null) yield break; 
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);
    
        if (this == null) yield break;

        player = Calls.Players.GetLocalPlayer();

        beltTransform = player.Controller.PlayerVisuals.transform.GetChild(1).GetChild(0).GetChild(3);
        rightHandTransform = player.Controller.gameObject.transform.GetChild(2).GetChild(2);
    
        ReleaseToBelt();
        MelonLogger.Msg("SignalScope: Player found, scope attached to belt.");
        hasSetUp = true;
    }

    private void Grab()
    {
        // Detach from belt but do NOT re-parent to the hand
        transform.SetParent(null);

        // Snap to the correct position immediately so there's no slide-in on first grab
        Vector3 idealPos = rightHandTransform.TransformPoint(handLocalPosition);
        Quaternion idealRot = rightHandTransform.rotation * handLocalRotation;
        transform.position = idealPos;
        transform.rotation = idealRot;
        scopeSmoothedRot = idealRot;
        scopeVelocity = Vector3.zero;

        isHolding = true;
        EnableScreen(true);
    }

    private void ReleaseToBelt()
    {
        transform.SetParent(beltTransform);
        transform.localPosition = beltLocalPosition;
        transform.localRotation = beltLocalRotation;

        isHolding = false;
        EnableScreen(false);
    }

    void EnableScreen(bool enable)
    {
        Camera.SetActive(enable);
        Screen.SetActive(enable);
    }

    bool IsHandCloseEnough(Vector3 handPos)
    {
        float distance = Vector3.Distance(gameObject.transform.position, handPos);
        return distance <= hold_distance;
    }

    private void HandleMusicChange()
    {
        if (!isHolding || !playMusic)
        {
            TurnOffAllMusic();
            return;
        }

        allMusicOff = false;

        // Compute zoom multiplier once — 1.0 at default FOV
        float angleMultiplier;
        if (currentFOV <= startingZoom)
        {
            float t = Mathf.InverseLerp(startingZoom, maxZoom, currentFOV); // 0→1 as you zoom in
            angleMultiplier = Mathf.Lerp(1f, zoomedInAngleScale, t);
        }
        else
        {
            float t = Mathf.InverseLerp(startingZoom, minZoom, currentFOV); // 0→1 as you zoom out
            angleMultiplier = Mathf.Lerp(1f, zoomedOutAngleScale, t);
        }

        float strongestSignal = 0f;

        foreach (KeyValuePair<GameObject, MusicEmitter> entry in musicEmitters)
        {
            GameObject body = entry.Key;
            MusicEmitter emitter = entry.Value;

            if (!body.activeSelf)
            {
                emitter.SetVolume(0);
                continue;
            }

            float strength = GetSignalStrengthForTarget(body, emitter.detectionAngle * angleMultiplier);
            emitter.SetVolume(strength);

            if (strength > strongestSignal) strongestSignal = strength;
        }

        UpdateStaticVolume(strongestSignal);
    }

    private void UpdateStaticVolume(float strongestSignal)
    {
        if (staticClip == null) return;
        
        if (!OwSystemSettings.SignalScopePlayStatic.Value) AudioManager.ChangeVolume(staticClip, 0);

        // Static fills in whenever nothing else is audible, and fades out
        // smoothly as the scope tunes into a real music emitter.
        float target = staticVolume * (1f - strongestSignal);
        AudioManager.ChangeVolume(staticClip, target);
    }

    private float GetSignalStrengthForTarget(GameObject target, float detectionAngle)
    {
        Vector3 scopePos     = Camera.transform.position;
        Vector3 scopeForward = Camera.transform.forward;
        Vector3 dirToTarget  = (target.transform.position - scopePos).normalized;

        float angleToTarget = Vector3.Angle(scopeForward, dirToTarget);

        if (angleToTarget > detectionAngle)
            return 0f;

        return 1f - (angleToTarget / detectionAngle);
    }
    public void TurnOffAllMusic()
    {
        if (allMusicOff) return;
        allMusicOff = true;
        foreach (var emitter in musicEmitters.Values)
        {
            emitter.SetVolume(0f);
        }

        if (staticClip != null)
            AudioManager.ChangeVolume(staticClip, 0f);
    }

    public void StopMusicEmitter(GameObject go)
    {
        if (musicEmitters.TryGetValue(go, out MusicEmitter emitter))
        {
            emitter.SetVolume(0f);
        }
        musicEmitters.Remove(go);
    }

    void OnDestroy()
    {
        if (staticClip != null)
            AudioManager.StopPlayback(staticClip);
    }
}