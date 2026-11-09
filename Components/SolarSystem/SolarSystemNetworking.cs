using System.Collections;
using System.Globalization;
using System.Text;
using Il2CppPhoton.Pun;
using MelonLoader;
using OuterWildsRumble.Components.Utils;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI;
using RumbleModdingAPI.RMAPI;
using UnityEngine;

namespace OuterWildsRumble.Components;

/// <summary>
/// Everything Photon: solar system state sync, match sync, and the signal scope / marshmallow stick RPCs.
/// Must sit on the same GameObject as the PhotonView (the solar system root).
/// </summary>
[RegisterTypeInIl2Cpp]
public class SolarSystemNetworking : MonoBehaviourPun
{
    private SolarSystemRealisticMode _realistic;

    private SolarSystemRealisticMode Realistic
    {
        get
        {
            if (_realistic == null) _realistic = GetComponent<SolarSystemRealisticMode>();
            return _realistic;
        }
    }

    public void Start()
    {
        Actions.onMapInitialized += OnMapInitialized;
        Actions.onPlayerSpawned += (player) => PlayerSpawned(player);
    }

    // ───────────────────────── Players / matches ─────────────────────────

    public void PlayerSpawned(Il2CppRUMBLE.Players.Player player)
    {
        if (Calls.Players.GetLocalPlayer().Equals(player)) return;

        SignalScope.SpawnForRemotePlayer(player);
        if (SignalScope.Local != null) SignalScope.Local.BroadcastHoldState();

        MarshmallowStick.SpawnForRemotePlayer(player);
        if (MarshmallowStick.Local != null) MarshmallowStick.Local.BroadcastState();

        if (!PhotonNetwork.IsMasterClient) return;

        //SendSolarSystemState();
    }

    private string _previousMap = "";
    private const float MatchSyncDelaySeconds = 5f;

    private static bool IsMatchMap(string m) => m == "Map0" || m == "Map1";

    private void OnMapInitialized(string mapName)
    {
        bool fromGymToMatch = _previousMap == "Gym" && IsMatchMap(mapName);
        _previousMap = mapName;
        
        if (mapName == "Park")
            CreateParkSyncButton();

        if (!fromGymToMatch) return;
        if (!OwSystemSettings.SyncSolarSystemInMatches.Value) return;
        if (!PhotonNetwork.IsMasterClient) return;

        MelonCoroutines.Start(MatchSyncRoutine());
    }

    private IEnumerator MatchSyncRoutine()
    {
        yield return new WaitForSecondsRealtime(MatchSyncDelaySeconds);

        if (this == null) yield break;
        if (!OtherPlayerHasMod()) yield break;

        DoSendSolarSystemState();
    }
    
    public void CreateParkSyncButton()
    {
        RumbleUtils.ButtonWithLabel syncButton = new RumbleUtils.ButtonWithLabel(new Vector3(-28.7464f, -0.6508f, -11.8465f), "Sync systems", "SyncSystemsButton");
        syncButton.button.transform.localRotation = Quaternion.Euler(90f, 240.4362f, 0);

        syncButton.AddOnPressed(() => SendSolarSystemState());
    }

    private bool OtherPlayerHasMod()
    {
        return Calls.Mods.doesOpponentHaveMod("OuterWildsRumble", "2.0.0", false);
    }


    private string _pendingPayload;
    private object _applyRoutine;

    private Dictionary<int, string> _codes;

    private void BuildCodes()
    {
        var s = Main.solarSystem;
        _codes = new Dictionary<int, string>();
        void Add(GameObject go, string code) { if (go != null) _codes[go.GetInstanceID()] = code; }

        Add(s.Sun, "SU");
        Add(s.SunStation, "SS");
        Add(s.HourGlassTwins, "HG");
        Add(s.TimberHearth, "TH");
        Add(s.Attlerock, "AT");
        Add(s.BrittleHollow, "BH");
        Add(s.HollowsLantern, "HL");
        Add(s.GiantsDeep, "GD");
        Add(s.OrbitalProbeCannon, "PC");
        Add(s.OrbitalProbe, "OP");
        Add(s.QuantumMoon, "QM");
        Add(s.DarkBramble, "DB");
        Add(s.WhiteHole, "WH");
        Add(s.WhiteHoleStation, "WS");
        Add(s.Interloper, "IL");
        Add(s.PlayerShip, "PS");
    }

    private string CodeOf(Transform t, Transform root)
    {
        if (_codes == null) BuildCodes();
        return _codes.TryGetValue(t.gameObject.GetInstanceID(), out var c) ? c : PathOf(t, root);
    }

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

    // ───────────────────────── Marshmallow / scope RPCs ─────────────────────────

    public void SendMarshmallowState(bool stickHeld, bool marshHeld, bool isMerged)
    {
        if (photonView == null || !PhotonNetwork.InRoom) return;
        string payload = PhotonNetwork.LocalPlayer.ActorNumber + "|" + (stickHeld ? "1" : "0")
                         + "|" + (marshHeld ? "1" : "0") + "|" + (isMerged ? "1" : "0");
        photonView.RPC("ReceiveMarshmallowState", RpcTarget.Others, payload);
    }

    [PhotonRPCs.PunRPC]
    public void ReceiveMarshmallowState(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return;
        string[] p = payload.Split('|');
        if (p.Length < 4) return;
        MarshmallowStick.SetRemoteState(int.Parse(p[0], CultureInfo.InvariantCulture), p[1] == "1", p[2] == "1", p[3] == "1");
    }

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

    // ───────────────────────── Solar system state ─────────────────────────

    private const float SyncCooldownSeconds = 3f;
    private float _lastSendTime = -999f;

    public void SendSolarSystemState()
    {
        float remaining = _lastSendTime + SyncCooldownSeconds - Time.unscaledTime;
        if (remaining > 0f) return;

        DoSendSolarSystemState();
    }

    private void DoSendSolarSystemState()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < 2) return;
        if (photonView == null)
        {
            MelonLogger.Warning("[SolarSystem] No PhotonView on this object, cannot send state.");
            return;
        }

        var C = CultureInfo.InvariantCulture;
        const string F2 = "0.##", F4 = "0.####";
        Transform root = Main.solarSystem.Root.transform;

        var orbiters = root.GetComponentsInChildren<Orbiter>(true);
        var ellipticals = root.GetComponentsInChildren<EllipticalOrbiter>(true);
        var hourGlass = Main.solarSystem.HourGlassTwins != null
            ? Main.solarSystem.HourGlassTwins.GetComponent<HourGlassTwins>()
            : null;

        var sb = new StringBuilder();

        foreach (var o in orbiters)
            sb.Append("O|").Append(CodeOf(o.transform, root)).Append('|')
                .Append(o._currentOrbitAngle.ToString(F2, C)).Append('|')
                .Append(o._currentSpinAngle.ToString(F2, C)).Append(';');

        foreach (var e in ellipticals)
            sb.Append("E|").Append(CodeOf(e.transform, root)).Append('|')
                .Append(e._currentAngle.ToString(F2, C)).Append(';');

        if (hourGlass != null)
            sb.Append("H|").Append((int)hourGlass.currentState).Append('|')
                .Append(hourGlass.flowingToAsh ? 1 : 0).Append('|')
                .Append(hourGlass.stateTimer.ToString(F2, C)).Append(';');

        // Realistic mode: what you see is defined by the system's frame rotation and the pinned
        // anchor planet, not just the orbit angles, so send those as well.
        var realistic = Realistic;
        if (realistic != null && realistic.TryGetFrame(out Quaternion r, out bool hasCustom, out Quaternion c))
        {
            sb.Append("R|").Append(CodeOf(realistic.relativeToPlanet, root)).Append('|')
                .Append(r.x.ToString(F4, C)).Append('|').Append(r.y.ToString(F4, C)).Append('|')
                .Append(r.z.ToString(F4, C)).Append('|').Append(r.w.ToString(F4, C)).Append('|');

            if (hasCustom)
            {
                sb.Append("1|")
                    .Append(c.x.ToString(F4, C)).Append('|').Append(c.y.ToString(F4, C)).Append('|')
                    .Append(c.z.ToString(F4, C)).Append('|').Append(c.w.ToString(F4, C));
            }
            else
            {
                sb.Append("0|0|0|0|1");
            }

            sb.Append(';');
        }

        _lastSendTime = Time.unscaledTime;
        MelonLogger.Msg("[SolarSystem] Host sending solar system state");
        photonView.RPC("ReceiveSolarSystemState", RpcTarget.Others, sb.ToString());
    }

    // CLIENTS ONLY: store the host's state and apply it a couple of frames later
    [PhotonRPCs.PunRPC]
    public void ReceiveSolarSystemState(string payload)
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (string.IsNullOrEmpty(payload)) return;

        if (IsMatchMap(Calls.Scene.GetSceneName()) && !OwSystemSettings.SyncSolarSystemInMatches.Value)
        {
            MelonLogger.Msg("[SolarSystem] Sync in matches disabled, ignoring host state.");
            return;
        }

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

    // Only applies the host's frame if we are anchored to the same planet; the actual frame maths is realistic mode's job
    private void ApplyFrame(bool hasFrame, string anchorName, Quaternion rootRot, bool hasCustom, Quaternion customRot)
    {
        if (!hasFrame) return;

        var realistic = Realistic;
        string myAnchor = realistic != null && realistic.relativeToPlanet != null
            ? CodeOf(realistic.relativeToPlanet, Main.solarSystem.Root.transform)
            : null;

        if (myAnchor == null || myAnchor != anchorName)
        {
            MelonLogger.Warning(
                $"[SolarSystem] Host is anchored to '{anchorName}' but we are anchored to '{myAnchor ?? "nothing"}', skipping frame sync.");
            return;
        }

        realistic.ApplyFrame(rootRot, hasCustom, customRot);

        MelonLogger.Msg($"[SolarSystem] Frame synced to anchor '{anchorName}'");
    }

    private void ApplyState(string payload)
    {
        var C = CultureInfo.InvariantCulture;
        Transform root = Main.solarSystem.Root.transform;

        var orbiterMap = new Dictionary<string, Orbiter>();
        foreach (var o in root.GetComponentsInChildren<Orbiter>(true))
            orbiterMap[CodeOf(o.transform, root)] = o;

        var ellipticalMap = new Dictionary<string, EllipticalOrbiter>();
        foreach (var e in root.GetComponentsInChildren<EllipticalOrbiter>(true))
            ellipticalMap[CodeOf(e.transform, root)] = e;

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

        // The spin angle just jumped to the host's value. Reset the baseline even if no frame block arrived,
        // otherwise realistic mode treats the jump as one huge rotation step and the sky snaps.
        if (Realistic != null)
            Realistic.ResyncSpinBaseline();

        MelonLogger.Msg($"[SolarSystem] Applied {applied} entries. Unmatched paths: {(missing.Length == 0 ? "none" : missing.ToString())}");
    }
}