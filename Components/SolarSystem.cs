using System.Collections;
using System.Globalization;
using System.Text;
using Il2CppPhoton.Pun;
using MelonLoader;
using UnityEngine;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SolarSystem : MonoBehaviourPun
{
    public Transform relativeToPlanet = null;
    private Vector3 _anchorFixedPosition;
    private Quaternion _anchorFixedRotation;
    private Quaternion _lastAnchorRotation;
    private Vector3 _worldOffset;
    private Quaternion _rotOffset;

    private Quaternion _tiltRotation = Quaternion.identity;
    private Quaternion _orbitalOffset = Quaternion.identity;

    public void Start()
    {
        Actions.onMapInitialized += SceneLoaded;

        transform.position = new Vector3(
            OwSystemSettings.SolarSystemGymX.Value,
            OwSystemSettings.SolarSystemGymY.Value,
            OwSystemSettings.SolarSystemGymZ.Value);
        if (OwSystemSettings.RealisticMode.Value)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }

        Actions.onPlayerSpawned += (player) => PlayerSpawned(player);
    }

    public void PlayerSpawned(Il2CppRUMBLE.Players.Player player)
    {
        // Ignore our own spawn
        if (Calls.Players.GetLocalPlayer().Equals(player)) return;

        // Everyone: give the newcomer a scope on their belt, and tell them if we're holding ours
        SignalScope.SpawnForRemotePlayer(player);
        if (SignalScope.Local != null) SignalScope.Local.BroadcastHoldState();

        // Host only: orbit state sync
        if (!PhotonNetwork.IsMasterClient) return;

        SendSolarSystemState();
    }

    public void SetRelativeTo(GameObject planet) =>
        SetRelativeTo(planet != null ? planet.transform : null);

    private Orbiter _anchorOrbiter;
    private EllipticalOrbiter _anchorEllipticalOrbiter;
    private float _initialSpinAngle;
    private float _lastSpinAngle;
    private float _lastOrbitAngle;
    private Quaternion _lastEllipticalRotation;

    public void SetRelativeTo(Transform planet)
    {
        _tiltRotation = transform.rotation;
        _orbitalOffset = Quaternion.identity;

        if (relativeToPlanet != null)
        {
            for (int i = 0; i < relativeToPlanet.childCount; i++)
            {
                Transform child = relativeToPlanet.GetChild(i);
                if (child != null)
                    child.gameObject.SetActive(true);
            }
        }

        if (_anchorOrbiter != null)
        {
            _anchorOrbiter.customRotation = null;
            _anchorOrbiter = null;
        }

        _anchorEllipticalOrbiter = null;

        relativeToPlanet = planet;

        if (planet != null)
        {
            for (int i = 0; i < planet.childCount; i++)
            {
                Transform child = planet.GetChild(i);
                if (child != null)
                {
                    MelonLogger.Msg("Got child disabling");
                    child.gameObject.SetActive(false);
                }
            }

            _anchorOrbiter = planet.GetComponent<Orbiter>();
            _anchorEllipticalOrbiter = planet.GetComponent<EllipticalOrbiter>();

            _initialSpinAngle = _anchorOrbiter != null ? _anchorOrbiter._currentSpinAngle : 0f;
            _lastSpinAngle = _initialSpinAngle;
            _lastOrbitAngle = _anchorOrbiter != null ? _anchorOrbiter._currentOrbitAngle : 0f; // Initialized here

            if (_anchorEllipticalOrbiter != null)
                _lastEllipticalRotation = planet.rotation;

            Vector3 offsetToOrigin = Vector3.zero - planet.position;
            transform.position += offsetToOrigin;

            if (_anchorOrbiter != null)
                _anchorOrbiter.customRotation = planet.rotation;

            MelonLogger.Msg($"[SolarSystem] Anchored '{planet.name}' to world origin (0,0,0).");
        }
        else
        {
            MelonLogger.Msg("[SolarSystem] Relative orbit disabled.");
        }
    }

    void FixedUpdate()
    {
        if (relativeToPlanet == null) return;

        if (_anchorOrbiter != null)
        {
            float currentSpinAngle = _anchorOrbiter._currentSpinAngle;
            float currentOrbitAngle = _anchorOrbiter._currentOrbitAngle;


            float deltaAngle = (currentSpinAngle - _lastSpinAngle) + (currentOrbitAngle - _lastOrbitAngle);
            _lastSpinAngle = currentSpinAngle;
            _lastOrbitAngle = currentOrbitAngle;

            Quaternion deltaRotation = Quaternion.AngleAxis(deltaAngle, _tiltRotation * Vector3.up);
            _orbitalOffset = deltaRotation * _orbitalOffset;
            transform.position = deltaRotation * transform.position;
            transform.rotation = _orbitalOffset * _tiltRotation;
        }
        else if (_anchorEllipticalOrbiter != null)
        {
            Quaternion currentRot = relativeToPlanet.rotation;
            Quaternion deltaRotation = currentRot * Quaternion.Inverse(_lastEllipticalRotation);
            _lastEllipticalRotation = currentRot;

            _orbitalOffset = deltaRotation * _orbitalOffset;
            transform.position = deltaRotation * transform.position;
            transform.rotation = _orbitalOffset * _tiltRotation;
        }

        transform.position += Vector3.zero - relativeToPlanet.position;
    }

    public void SceneLoaded(string mapName)
    {
        if (!OwSystemSettings.RealisticMode.Value)
        {
            switch (mapName)
            {
                case "Gym":
                    transform.position = new Vector3(
                        OwSystemSettings.SolarSystemGymX.Value,
                        OwSystemSettings.SolarSystemGymY.Value,
                        OwSystemSettings.SolarSystemGymZ.Value);
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    break;

                case "Map0":
                    transform.position = new Vector3(
                        OwSystemSettings.SolarSystemRingX.Value,
                        OwSystemSettings.SolarSystemRingY.Value,
                        OwSystemSettings.SolarSystemRingZ.Value);
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    break;

                case "Map1":
                    transform.position = new Vector3(
                        OwSystemSettings.SolarSystemPitX.Value,
                        OwSystemSettings.SolarSystemPitY.Value,
                        OwSystemSettings.SolarSystemPitZ.Value);
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    break;

                case "Park":
                    transform.position = new Vector3(
                        OwSystemSettings.SolarSystemParkX.Value,
                        OwSystemSettings.SolarSystemParkY.Value,
                        OwSystemSettings.SolarSystemParkZ.Value);
                    transform.rotation = Quaternion.Euler(0, 124.1311f, 0);
                    break;
            }
        }
        else
        {
            transform.position = Vector3.zero;

            switch (mapName)
            {
                case "Gym":
                    transform.rotation = Quaternion.Euler(90, 0, 0);
                    SetRelativeTo(Main.solarSystem.TimberHearth);
                    GameObject.Find("SCENE VFX/SFX").SetActive(false);
                    GameObject.Find("INTERACTABLES").transform.GetChild(6).GetChild(5).gameObject.SetActive(false);
                    var sceneObj = GameObject.Find("SCENE");
                    if (sceneObj != null)
                    {
                        sceneObj.transform.GetChild(4).gameObject.SetActive(false);
                        sceneObj.transform.GetChild(3).gameObject.SetActive(false);
                    }
                    else
                    {
                        MelonLogger.Warning(
                            "[SolarSystem] Could not find active 'SCENE' object — skipping child deactivation.");
                    }

                    break;

                case "Map0":
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    SetRelativeTo(Main.solarSystem.HourGlassTwins);
                    GameObject.Find("Scene").transform.GetChild(1).gameObject.SetActive(false);
                    break;

                case "Map1":
                    transform.rotation = Quaternion.Euler(0, 90, 0);
                    SetRelativeTo(Main.solarSystem.BrittleHollow);
                    break;

                case "Park":
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    SetRelativeTo(Main.solarSystem.HearthianMapSatelite);
                    GameObject.Find("SCENE").transform.GetChild(2).gameObject.SetActive(false);
                    GameObject.Find("SCENE").transform.GetChild(3).gameObject.SetActive(false);
                    GameObject.Find("SCENE VFX/SFX").SetActive(false);
                    break;
            }
        }
    }

    public void StartSolarSystem()
    {
        if (Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().hasFiredBefore)
            Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().Restart();

        Main.solarSystem.BrittleHollow.GetComponent<BrittleHollow>().SolarSystemRestart();
        Main.solarSystem.HollowsLantern.GetComponent<HollowsLantern>().SolarSystemRestart();
        Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().StartFiringSequence();

        if (OwSystemSettings.RealisticMode.Value)
        {
            SetRelativeTo(Main.solarSystem.TimberHearth);
        }
    }

    void Scale(float scale)
    {
        SolarSystemScaler.Apply(scale);
    }

    // ───────────────────────── Networking ─────────────────────────

    private string _pendingPayload;
    private object _applyRoutine;

    // Name-only path (no sibling index) so a different child order on either client can't break matching
    private static string PathOf(Transform t, Transform root)
    {
        var sb = new StringBuilder();
        while (t != null && t != root)
        {
            sb.Insert(0, "/" + t.name);
            t = t.parent;
        }

        return sb.ToString();
    }

    // ── Signal scope hold-state sync ──

    // ANY CLIENT -> everyone else. String payload ("actor|0/1"), same style as the state sync,
    // to keep Il2Cpp RPC marshalling simple.
    public void SendScopeHolding(bool holding)
    {
        if (photonView == null || !PhotonNetwork.InRoom) return;

        string payload = PhotonNetwork.LocalPlayer.ActorNumber + "|" + (holding ? "1" : "0");
        photonView.RPC("ReceiveScopeHolding", RpcTarget.Others, payload);
    }

    [PhotonRPCs.PunRPC]
    public void ReceiveScopeHolding(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return;

        string[] p = payload.Split('|');
        if (p.Length < 2) return;

        SignalScope.SetRemoteHolding(int.Parse(p[0], CultureInfo.InvariantCulture), p[1] == "1");
    }

    // HOST ONLY: build the payload and send it to everyone else
    public void SendSolarSystemState()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2) return;
        if (photonView == null)
        {
            MelonLogger.Warning("[SolarSystem] No PhotonView on this object, cannot send state.");
            return;
        }

        var C = CultureInfo.InvariantCulture;
        Transform root = Main.solarSystem.Root.transform;

        var orbiters = root.GetComponentsInChildren<Orbiter>(true);
        var ellipticals = root.GetComponentsInChildren<EllipticalOrbiter>(true);
        var hourGlass = Main.solarSystem.HourGlassTwins != null
            ? Main.solarSystem.HourGlassTwins.GetComponent<HourGlassTwins>()
            : null;

        var sb = new StringBuilder();

        foreach (var o in orbiters)
            sb.Append("O|").Append(PathOf(o.transform, root)).Append('|')
                .Append(o._currentOrbitAngle.ToString("R", C)).Append('|')
                .Append(o._currentSpinAngle.ToString("R", C)).Append(';');

        foreach (var e in ellipticals)
            sb.Append("E|").Append(PathOf(e.transform, root)).Append('|')
                .Append(e._currentAngle.ToString("R", C)).Append(';');

        if (hourGlass != null)
            sb.Append("H|").Append((int)hourGlass.currentState).Append('|')
                .Append(hourGlass.flowingToAsh ? 1 : 0).Append('|')
                .Append(hourGlass.stateTimer.ToString("R", C)).Append(';');

        // Realistic mode: what you see is defined by the system's frame rotation and the pinned
        // anchor planet, not just the orbit angles, so send those as well.
        if (relativeToPlanet != null)
        {
            Quaternion r = transform.rotation;
            sb.Append("R|").Append(relativeToPlanet.name).Append('|')
                .Append(r.x.ToString("R", C)).Append('|').Append(r.y.ToString("R", C)).Append('|')
                .Append(r.z.ToString("R", C)).Append('|').Append(r.w.ToString("R", C)).Append('|');

            if (_anchorOrbiter != null && _anchorOrbiter.customRotation.HasValue)
            {
                Quaternion c = _anchorOrbiter.customRotation.Value;
                sb.Append("1|")
                    .Append(c.x.ToString("R", C)).Append('|').Append(c.y.ToString("R", C)).Append('|')
                    .Append(c.z.ToString("R", C)).Append('|').Append(c.w.ToString("R", C));
            }
            else
            {
                sb.Append("0|0|0|0|1");
            }

            sb.Append(';');
        }

        MelonLogger.Msg("[SolarSystem] Host sending solar system state");
        photonView.RPC("ReceiveSolarSystemState", RpcTarget.Others, sb.ToString());
    }

    // CLIENTS ONLY: store the host's state and apply it a couple of frames later
    [PhotonRPCs.PunRPC]
    public void ReceiveSolarSystemState(string payload)
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (string.IsNullOrEmpty(payload)) return;

        MelonLogger.Msg($"[SolarSystem] Client received state ({payload.Length} chars)");

        _pendingPayload = payload;
        if (_applyRoutine != null)
        {
            try { MelonCoroutines.Stop(_applyRoutine); } catch { }
        }

        _applyRoutine = MelonCoroutines.Start(ApplyStateAfterFrames());
    }

    // Orbiter.Start() randomises angles on the first frame after it's added, which would
    // overwrite the synced values if the RPC lands right after setup. Wait for that first.
    private IEnumerator ApplyStateAfterFrames()
    {
        yield return null;
        yield return null;

        if (this == null) yield break;

        try
        {
            ApplyState(_pendingPayload);
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error("[SolarSystem] Failed to apply state: " + ex);
        }

        _applyRoutine = null;
    }

    // Mirrors the host's frame (system rotation + pinned anchor planet) so both clients see the same sky
    private void ApplyFrame(bool hasFrame, string anchorName, Quaternion rootRot, bool hasCustom, Quaternion customRot)
    {
        if (!hasFrame) return;

        if (relativeToPlanet == null || relativeToPlanet.name != anchorName)
        {
            MelonLogger.Warning(
                $"[SolarSystem] Host is anchored to '{anchorName}' but we are anchored to '{(relativeToPlanet != null ? relativeToPlanet.name : "nothing")}', skipping frame sync.");
            return;
        }

        _tiltRotation = rootRot;
        _orbitalOffset = Quaternion.identity;
        transform.rotation = rootRot;

        if (_anchorOrbiter != null)
        {
            if (hasCustom) _anchorOrbiter.customRotation = customRot;

            // The angles just jumped, so don't let FixedUpdate treat the jump as one huge rotation step
            _lastSpinAngle = _anchorOrbiter._currentSpinAngle;
            _lastOrbitAngle = _anchorOrbiter._currentOrbitAngle;
        }

        MelonLogger.Msg($"[SolarSystem] Frame synced to anchor '{anchorName}'");
    }

    private void ApplyState(string payload)
    {
        var C = CultureInfo.InvariantCulture;
        Transform root = Main.solarSystem.Root.transform;

        var orbiterMap = new Dictionary<string, Orbiter>();
        foreach (var o in root.GetComponentsInChildren<Orbiter>(true))
            orbiterMap[PathOf(o.transform, root)] = o;

        var ellipticalMap = new Dictionary<string, EllipticalOrbiter>();
        foreach (var e in root.GetComponentsInChildren<EllipticalOrbiter>(true))
            ellipticalMap[PathOf(e.transform, root)] = e;

        var hourGlass = Main.solarSystem.HourGlassTwins != null
            ? Main.solarSystem.HourGlassTwins.GetComponent<HourGlassTwins>()
            : null;

        int applied = 0;
        var missing = new StringBuilder();

        bool hasFrame = false, hasCustom = false;
        string anchorName = null;
        Quaternion rootRot = Quaternion.identity, customRot = Quaternion.identity;

        foreach (string entry in payload.Split(';'))
        {
            if (entry.Length == 0) continue;
            string[] p = entry.Split('|');

            switch (p[0])
            {
                case "O":
                    if (orbiterMap.TryGetValue(p[1], out var orb))
                    {
                        float orbitAngle = float.Parse(p[2], C);
                        float spinAngle = float.Parse(p[3], C);

                        orb.randomisePos = false; // make sure a late Start() can't re-randomise
                        orb._currentOrbitAngle = orbitAngle;

                        // Rotation accumulates on transform.rotation, so apply the spin *difference*
                        if (orb.spinEnabled && !orb.customRotation.HasValue)
                        {
                            float diff = Mathf.DeltaAngle(orb._currentSpinAngle, spinAngle);
                            orb.transform.rotation *= Quaternion.AngleAxis(diff, orb.spinAxis);
                        }

                        orb._currentSpinAngle = spinAngle;
                        applied++;
                    }
                    else
                    {
                        missing.Append(p[1]).Append(' ');
                    }

                    break;

                case "E":
                    if (ellipticalMap.TryGetValue(p[1], out var ell))
                    {
                        ell.randomOrbitAngle = false;
                        ell._currentAngle = float.Parse(p[2], C);
                        applied++;
                    }
                    else
                    {
                        missing.Append(p[1]).Append(' ');
                    }

                    break;

                case "R":
                    anchorName = p[1];
                    rootRot = new Quaternion(float.Parse(p[2], C), float.Parse(p[3], C),
                        float.Parse(p[4], C), float.Parse(p[5], C));
                    hasCustom = p[6] == "1";
                    customRot = new Quaternion(float.Parse(p[7], C), float.Parse(p[8], C),
                        float.Parse(p[9], C), float.Parse(p[10], C));
                    hasFrame = true;
                    break;

                case "H":
                    if (hourGlass != null)
                    {
                        hourGlass.currentState = (HourGlassTwins.State)int.Parse(p[1], C);
                        hourGlass.flowingToAsh = p[2] == "1";
                        hourGlass.stateTimer = float.Parse(p[3], C);

                        float progress = hourGlass.currentState == HourGlassTwins.State.Transferring
                            ? Mathf.Clamp01(hourGlass.stateTimer / hourGlass.transferDurationSeconds)
                            : 0f;
                        hourGlass.SetSandScales(progress);
                        applied++;
                    }

                    break;
            }
        }

        ApplyFrame(hasFrame, anchorName, rootRot, hasCustom, customRot);

        MelonLogger.Msg($"[SolarSystem] Applied {applied} entries. Unmatched paths: {(missing.Length == 0 ? "none" : missing.ToString())}");
    }
}