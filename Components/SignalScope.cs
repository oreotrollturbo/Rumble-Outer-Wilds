using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppPhoton.Pun;
using Il2CppRUMBLE.Players;
using OuterWildsRumble.Components;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SignalScope : MonoBehaviour
{
    public static GameObject prefabSignalScope;
    // ───────────────────────── Registry ─────────────────────────
    public static SignalScope Local;
    public static readonly Dictionary<int, SignalScope> Remote = new Dictionary<int, SignalScope>();

    // A hold-state RPC can arrive before the remote scope exists, so remember it
    private static readonly Dictionary<int, bool> pendingHolding = new Dictionary<int, bool>();

    // ───────────────────────── Ownership ─────────────────────────
    public bool isLocal = true;   // set to false right after AddComponent for other players
    public Player owner;          // remote owner (null for local)
    public int ownerActor = -1;
    private bool remoteHolding;

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

    private float prevRightTrigger;

    public Vector3 beltLocalPosition = new Vector3(OwSystemSettings.SignalScopeOffsetX.Value, OwSystemSettings.SignalScopeOffsetY.Value, OwSystemSettings.SignalScopeOffsetZ.Value);
    public Quaternion beltLocalRotation = Quaternion.Euler(OwSystemSettings.SignalScopeRotationX.Value, OwSystemSettings.SignalScopeRotationY.Value, OwSystemSettings.SignalScopeRotationZ.Value);

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

    // "PARENTING" VARIABLES
    private Vector3 scopeVelocity = Vector3.zero;
    private Quaternion scopeSmoothedRot;

    public float scopePositionSmoothTime = 0.05f;
    public float scopeRotationSmoothing = 15f;

    public bool playMusic = true;
    public bool grabDuringMatches = true;

    // statique
    public string staticSoundFileName = "signalscope_static.wav";
    public float staticVolume = 1f;
    private AudioManager.ClipData staticClip;

    private bool allMusicOff;

    public SignalScope(IntPtr ptr) : base(ptr) {}

    // ───────────────────────── Static spawning / networking helpers ─────────────────────────

    /// Call on scene load, before spawning scopes. Old scopes died with the old player belts.
    public static void ClearRegistry()
    {
        Local = null;
        Remote.Clear();
    }

    private static int GetActor(Player p)
    {
        try
        {
            var pv = p?.Controller?.gameObject.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null) return pv.Owner.ActorNumber;
        }
        catch { }

        return -1;
    }

    /// Call for every non-local player (scene load + onPlayerSpawned). Safe to call repeatedly.
    public static void SpawnForRemotePlayer(Player p)
    {
        if (p == null || !OwSystemSettings.SignalScopeEnabled.Value) return;
        if (Calls.Players.GetLocalPlayer().Equals(p)) return;
        MelonCoroutines.Start(SpawnWhenReady(p));
    }

    private static IEnumerator SpawnWhenReady(Player p)
    {
        // The controller / photon view may not exist yet right after spawn
        float waited = 0f;
        int actor = -1;
        while (waited < 20f)
        {
            actor = GetActor(p);
            if (actor >= 0) break;
            yield return new WaitForSeconds(0.5f);
            waited += 0.5f;
        }

        if (actor < 0 || prefabSignalScope == null) yield break;
        if (Remote.TryGetValue(actor, out var existing) && existing != null) yield break;

        GameObject go = GameObject.Instantiate(prefabSignalScope);
        go.transform.localScale = new Vector3(2, 2, 2);

        // Start() runs on the next frame, so these fields are set in time
        SignalScope scope = go.AddComponent<SignalScope>();
        scope.isLocal = false;
        scope.owner = p;
        scope.ownerActor = actor;
        if (pendingHolding.TryGetValue(actor, out bool h)) scope.remoteHolding = h;

        Remote[actor] = scope;
    }

    /// Called by the SolarSystem RPC receiver.
    public static void SetRemoteHolding(int actor, bool holding)
    {
        pendingHolding[actor] = holding;
        if (Remote.TryGetValue(actor, out var scope) && scope != null)
            scope.remoteHolding = holding;
    }

    /// Local client tells everyone else whether it is holding its scope.
    public void BroadcastHoldState()
    {
        if (!isLocal) return;
        if (Main.solarSystem.Root == null) return;

        var networking = Main.solarSystem.Root.GetComponent<SolarSystemNetworking>();
        if (networking != null) networking.SendScopeHolding(isHolding);
    }

    // ───────────────────────── Lifecycle ─────────────────────────

    void Start()
    {
        gameObject.name = isLocal ? "SignalScope" : $"SignalScope_Remote_{ownerActor}";

        Camera = transform.GetChild(0).gameObject;
        startingZoom = Camera.GetComponent<Camera>().fieldOfView;
        currentFOV = Camera.GetComponent<Camera>().fieldOfView;
        Camera.GetComponent<Camera>().farClipPlane = OwSystemSettings.ViewDistance.Value;
        Screen = gameObject.transform.GetChild(35).gameObject;

        MelonCoroutines.Start(FindPlayerAndSetup());

        if (isLocal)
        {
            Local = this;
            SetupButtons();
            CacheMusicEmitters();
            SetupStaticSound();
        }
        // Remote scopes: no buttons, no music emitters, no static sound -> they never make noise
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

        if (isLocal)
        {
            float rightTrigger = Calls.ControllerMap.RightController.GetTrigger();
            bool pressedThisFrame = rightTrigger > holdThreshold && prevRightTrigger <= holdThreshold;
            prevRightTrigger = rightTrigger;

            if (!isHolding && pressedThisFrame && IsHandCloseEnough(rightHandTransform.position) && (grabDuringMatches || !Main.isInMatch))
            {
                Grab();
            }
            else if (isHolding && (rightTrigger <= releaseThreshold || (!grabDuringMatches && Main.isInMatch)))
            {
                ReleaseToBelt();
            }

            HandleMusicChange();
        }
        else
        {
            // Follow whatever the owner told us over the network
            if (remoteHolding && !isHolding) Grab();
            else if (!remoteHolding && isHolding) ReleaseToBelt();
        }
    }

    // "PARENTING" LOGIC
    void LateUpdate()
    {
        if (!hasSetUp || !isHolding) return;

        // ideal position
        Vector3 idealPos = rightHandTransform.TransformPoint(handLocalPosition);
        Quaternion idealRot = rightHandTransform.rotation * handLocalRotation;

        // transition to it
        transform.position = Vector3.SmoothDamp(
            transform.position, idealPos, ref scopeVelocity, scopePositionSmoothTime);

        scopeSmoothedRot = Quaternion.Slerp(
            scopeSmoothedRot, idealRot, Time.deltaTime * scopeRotationSmoothing);

        transform.rotation = scopeSmoothedRot;
    }

    public IEnumerator FindPlayerAndSetup()
    {
        Player target = null;

        while (true)
        {
            if (this == null) yield break;

            target = isLocal ? Calls.Players.GetLocalPlayer() : owner;

            if (target != null && target.Controller != null && target.Controller.PlayerVisuals != null)
                break;

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);

        if (this == null) yield break;

        player = target;

        beltTransform = player.Controller.PlayerVisuals.transform.GetChild(1).GetChild(0).GetChild(3);
        rightHandTransform = GetRightHand(player);

        ReleaseToBelt();
        MelonLogger.Msg($"SignalScope: {(isLocal ? "local" : "remote " + ownerActor)} player found, scope attached to belt.");
        hasSetUp = true;
    }

    // ASSUMPTION: remote players use the same controller hierarchy as the local one.
    // If the scope doesn't land in the remote player's hand, fix the path here.
    private Transform GetRightHand(Player p)
    {
        return p.Controller.gameObject.transform.GetChild(2).GetChild(2);
    }

    private void Grab()
    {
        transform.SetParent(null);

        // Snap to the correct position
        Vector3 idealPos = rightHandTransform.TransformPoint(handLocalPosition);
        Quaternion idealRot = rightHandTransform.rotation * handLocalRotation;
        transform.position = idealPos;
        transform.rotation = idealRot;
        scopeSmoothedRot = idealRot;
        scopeVelocity = Vector3.zero;

        isHolding = true;
        EnableScreen(true);
        BroadcastHoldState();
    }

    private void ReleaseToBelt()
    {
        transform.SetParent(beltTransform);
        transform.localPosition = beltLocalPosition;
        transform.localRotation = beltLocalRotation;

        isHolding = false;
        EnableScreen(false);

        // Don't broadcast during initial setup, only on real releases
        if (hasSetUp) BroadcastHoldState();
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

    // ───────────────────────── Music (local only) ─────────────────────────

    private void HandleMusicChange()
    {
        bool hearOthers = OwSystemSettings.MusicFromOthers.Value;
        bool localActive = isHolding && playMusic;

        if (!playMusic || (!localActive && !(hearOthers && AnyRemoteHolding())))
        {
            TurnOffAllMusic();
            return;
        }

        allMusicOff = false;

        float angleMultiplier = 1f;
        if (currentFOV <= startingZoom)
        {
            float t = Mathf.InverseLerp(startingZoom, maxZoom, currentFOV);
            angleMultiplier = Mathf.Lerp(1f, zoomedInAngleScale, t);
        }
        else
        {
            float t = Mathf.InverseLerp(startingZoom, minZoom, currentFOV);
            angleMultiplier = Mathf.Lerp(1f, zoomedOutAngleScale, t);
        }

        float strongestLocalSignal = 0f;
        List<GameObject> dead = null;

        foreach (KeyValuePair<GameObject, MusicEmitter> entry in musicEmitters)
        {
            GameObject body = entry.Key;
            MusicEmitter emitter = entry.Value;

            if (body == null || emitter == null)
            {
                if (dead == null) dead = new List<GameObject>();
                dead.Add(body);
                continue;
            }

            if (!body.activeInHierarchy)
            {
                emitter.SetVolume(0f);
                continue;
            }

            float strength = 0f;

            if (localActive)
            {
                strength = GetSignalStrengthForTarget(
                    Camera.transform, body, emitter.detectionAngle * angleMultiplier);
                if (strength > strongestLocalSignal) strongestLocalSignal = strength;
            }

            if (hearOthers)
            {
                foreach (var kv in Remote)
                {
                    SignalScope other = kv.Value;
                    if (other == null || !other.isHolding || other.Camera == null) continue;

                    // Remote zoom isn't synced, so the default angle is used
                    float s = GetSignalStrengthForTarget(
                        other.Camera.transform, body, emitter.detectionAngle);
                    if (s > strength) strength = s;
                }
            }

            emitter.SetVolume(strength);
        }

        if (dead != null)
            foreach (var go in dead)
                if (go != null) musicEmitters.Remove(go);

        // Static only reflects the local scope
        UpdateStaticVolume(localActive, strongestLocalSignal);
    }

    private bool AnyRemoteHolding()
    {
        foreach (var kv in Remote)
            if (kv.Value != null && kv.Value.isHolding) return true;
        return false;
    }

    private void UpdateStaticVolume(bool localActive, float strongestSignal)
    {
        if (staticClip == null) return;

        if (!localActive || !OwSystemSettings.SignalScopePlayStatic.Value)
        {
            AudioManager.ChangeVolume(staticClip, 0f);
            return;
        }

        // Static whenever nothing else is audible
        AudioManager.ChangeVolume(staticClip, staticVolume * (1f - strongestSignal));
    }

    private float GetSignalStrengthForTarget(Transform scopeCam, GameObject target, float detectionAngle)
    {
        Vector3 dirToTarget = (target.transform.position - scopeCam.position).normalized;
        float angleToTarget = Vector3.Angle(scopeCam.forward, dirToTarget);

        if (angleToTarget > detectionAngle) return 0f;
        return 1f - (angleToTarget / detectionAngle);
    }

    public void TurnOffAllMusic()
    {
        if (allMusicOff) return;
        allMusicOff = true;
        foreach (var emitter in musicEmitters.Values)
        {
            if (emitter != null) emitter.SetVolume(0f);
        }

        if (staticClip != null)
            AudioManager.ChangeVolume(staticClip, 0f);
    }

    public void StopMusicEmitter(GameObject go)
    {
        if (musicEmitters.TryGetValue(go, out MusicEmitter emitter))
        {
            if (emitter != null) emitter.SetVolume(0f);
        }
        musicEmitters.Remove(go);
    }

    void OnDestroy()
    {
        if (staticClip != null)
            AudioManager.StopPlayback(staticClip);

        if (!isLocal && Remote.TryGetValue(ownerActor, out var s) && s == this)
            Remote.Remove(ownerActor);

        if (isLocal && Local == this)
            Local = null;
    }
}