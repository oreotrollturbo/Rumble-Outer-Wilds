using System.Collections;
using MelonLoader;
using UnityEngine;
using Il2CppPhoton.Pun;
using Il2CppRUMBLE.Players;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;
using Object = UnityEngine.Object;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class MarshmallowStick : MonoBehaviour
{
    public static MarshmallowStick Local;
    public static readonly Dictionary<int, MarshmallowStick> Remote = new Dictionary<int, MarshmallowStick>();

    private static readonly Dictionary<int, int> pendingState = new Dictionary<int, int>();
    
    public static GameObject marshmallowStickAsset;
    public static GameObject marshmallowAsset;

    public bool isLocal = true;
    public Player owner;
    public int ownerActor = -1;

    private bool remoteStickHeld;
    private bool remoteMarshHeld;
    private bool remoteMerged;

    private const string MarshmallowName = "Marshmallow";
    private const string StickName = "RoastingStick";
    private const string SpotName = "MarshMallowSpot";

    private const float holdThreshold = 0.9f;
    private const float releaseThreshold = 0.1f;

    private float prevRt;
    private float prevLt;

    private class Item
    {
        public Transform t;
        public Vector3 baseScale = Vector3.one;
        public bool held;
    }

    private readonly Item stick = new Item();
    private readonly Item marsh = new Item();

    private Vector3 spotLocalPos;
    private Quaternion spotLocalRot = Quaternion.identity;
    private Vector3 rootBaseScale = Vector3.one;

    private bool merged;
    private float lastMergeChange = -999f;

    public Player player;
    public Transform backTransform;
    public Transform beltTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    private bool hasSetUp;
    private bool itemsReady;

    public MarshmallowStick(IntPtr ptr) : base(ptr) { }

    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    private static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);

    // Fixed offsets (these used to be config settings; values are the old defaults).
    private static readonly Vector3 HolderPos = V(0.1237f, 0.4571f, -0.1834f);
    private static readonly Quaternion HolderRot = Q(73.5842f, 258.0787f, 287.7444f);

    private static readonly Vector3 StickBackPos = V(0f, 0f, 0f);
    private static readonly Quaternion StickBackRot = Q(0f, 0f, 0f);

    private static readonly Vector3 MarshBeltPos = V(-0.067f, 0f, 0.1f);
    private static readonly Quaternion MarshBeltRot = Q(0f, 0f, 0f);

    private static readonly Vector3 StickHandPos = V(-0.2f, 0f, 0.1f);
    private static readonly Quaternion StickHandRot = Q(0f, 0f, 0f);

    private static readonly Vector3 MarshHandPos = V(0f, 0f, 0f);
    private static readonly Quaternion MarshHandRot = Q(-90f, 0f, 0f);

    private static readonly Vector3 MergedPos = V(0f, 0f, 0f);
    private static readonly Quaternion MergedRot = Q(0f, 0f, 0f);

    public static void ClearRegistry()
    {
        Local = null;
        Remote.Clear();
    }

    public static void SpawnLocal()
    {
        GameObject go = BuildMarshmallowStick();
        if (go == null) return;
        var comp = go.AddComponent<MarshmallowStick>();
        comp.isLocal = true;
        Local = comp;
        comp.Refresh();
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

    public static void SpawnForRemotePlayer(Player p)
    {
        if (p == null) return;
        if (Calls.Players.GetLocalPlayer().Equals(p)) return;
        MelonCoroutines.Start(SpawnWhenReady(p));
    }

    private static IEnumerator SpawnWhenReady(Player p)
    {
        float waited = 0f;
        int actor = -1;
        while (waited < 20f)
        {
            actor = GetActor(p);
            if (actor >= 0) break;
            yield return new WaitForSeconds(0.5f);
            waited += 0.5f;
        }

        if (actor < 0) yield break;
        if (Remote.TryGetValue(actor, out var existing) && existing != null) yield break;

        GameObject go = BuildMarshmallowStick();
        if (go == null) yield break;
        var comp = go.AddComponent<MarshmallowStick>();
        comp.isLocal = false;
        comp.owner = p;
        comp.ownerActor = actor;

        if (pendingState.TryGetValue(actor, out int bits))
        {
            comp.remoteStickHeld = (bits & 1) != 0;
            comp.remoteMarshHeld = (bits & 2) != 0;
            comp.remoteMerged = (bits & 4) != 0;
        }

        Remote[actor] = comp;
        comp.Refresh();
    }

    public static void SetRemoteState(int actor, bool stickHeld, bool marshHeld, bool isMerged)
    {
        pendingState[actor] = (stickHeld ? 1 : 0) | (marshHeld ? 2 : 0) | (isMerged ? 4 : 0);

        if (Remote.TryGetValue(actor, out var m) && m != null)
        {
            m.remoteStickHeld = stickHeld;
            m.remoteMarshHeld = marshHeld;
            m.remoteMerged = isMerged;
        }
    }

    public void BroadcastState()
    {
        if (!isLocal || !hasSetUp) return;
        if (Main.solarSystem.Root == null) return;

        var networking = Main.solarSystem.Root.GetComponent<SolarSystemNetworking>();
        if (networking != null) networking.SendMarshmallowState(stick.held, marsh.held, merged);
    }

    public static void RefreshAll()
    {
        if (Local != null) Local.Refresh();
        foreach (var kv in Remote)
            if (kv.Value != null) kv.Value.Refresh();
    }

    public void Refresh()
    {
        bool on = OwSystemSettings.MarshmallowStickIsEnabled.Value;

        if (!on && hasSetUp)
        {
            merged = false;
            stick.held = false;
            marsh.held = false;
        }

        SetItemsActive(on);
        gameObject.SetActive(on);
    }

    private void SetItemsActive(bool on)
    {
        if (!itemsReady) return;
        if (stick.t != null) stick.t.gameObject.SetActive(on);
        if (marsh.t != null) marsh.t.gameObject.SetActive(on);
    }

    void Start()
    {
        gameObject.name = isLocal ? "MarshmallowStick" : $"MarshmallowStick_Remote_{ownerActor}";

        stick.t = transform.Find(StickName);
        marsh.t = transform.Find(MarshmallowName);
        Transform spot = stick.t != null ? stick.t.Find(SpotName) : null;

        if (stick.t == null || marsh.t == null || spot == null)
        {
            MelonLogger.Error("MarshmallowStick: prefab is missing RoastingStick / Marshmallow / MarshMallowSpot.");
            enabled = false;
            return;
        }

        spotLocalPos = spot.localPosition;
        spotLocalRot = spot.localRotation;

        rootBaseScale = transform.localScale;
        stick.baseScale = stick.t.localScale;
        marsh.baseScale = marsh.t.localScale;

        stick.t.SetParent(null, false);
        marsh.t.SetParent(null, false);

        itemsReady = true;
        SetItemsActive(OwSystemSettings.MarshmallowStickIsEnabled.Value);

        MelonCoroutines.Start(FindPlayerAndSetup());
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

        backTransform = GetBack(player);
        beltTransform = GetBelt(player);
        leftHandTransform = GetLeftHand(player);
        rightHandTransform = GetRightHand(player);

        stick.held = false;
        marsh.held = false;

        hasSetUp = true;
        MelonLogger.Msg($"MarshmallowStick: {(isLocal ? "local" : "remote " + ownerActor)} player found, following back.");

        Refresh();
    }

    private Transform GetBack(Player p)
    {
        return p.Controller.PlayerVisuals.transform.GetChild(1).GetChild(0).GetChild(4);
    }
    
    private Transform GetBelt(Player p)
    {
        return p.Controller.PlayerVisuals.transform.GetChild(1).GetChild(0).GetChild(2);
    }

    private Transform GetRightHand(Player p) => p.Controller.gameObject.transform.GetChild(2).GetChild(2);
    private Transform GetLeftHand(Player p) => p.Controller.gameObject.transform.GetChild(2).GetChild(1);

    void FixedUpdate()
    {
        if (!hasSetUp || !itemsReady) return;
        if (backTransform == null || leftHandTransform == null || rightHandTransform == null) return;

        if (isLocal) LocalUpdate();
        else RemoteUpdate();
    }

    private void LocalUpdate()
    {
        float rt = Calls.ControllerMap.RightController.GetTrigger();
        float lt = Calls.ControllerMap.LeftController.GetTrigger();
        bool rtPressed = rt > holdThreshold && prevRt <= holdThreshold;
        bool ltPressed = lt > holdThreshold && prevLt <= holdThreshold;
        prevRt = rt;
        prevLt = lt;
        bool changed = false;

        if (!stick.held)
        {
            if (rtPressed && IsCloseEnough(stick.t.position, rightHandTransform.position))
            {
                GrabStick();
                changed = true;
            }
        }
        else if (rt <= releaseThreshold)
        {
            SendStickToBack();
            changed = true;
        }

        if (!marsh.held)
        {
            bool cooldownOver = Time.time - lastMergeChange >= OwSystemSettings.MarshmallowMergeCooldown.Value;

            if (ltPressed
                && IsCloseEnough(marsh.t.position, leftHandTransform.position)
                && (!merged || cooldownOver))
            {
                if (merged) Unmerge();
                GrabMarsh();
                changed = true;
            }
        }
        else
        {
            if (CanMerge())
            {
                Merge();
                changed = true;
            }
            else if (lt <= releaseThreshold)
            {
                SendMarshToBack();
                changed = true;
            }
        }

        if (changed) BroadcastState();
    }

    private void RemoteUpdate()
    {
        if (remoteStickHeld && !stick.held) GrabStick();
        else if (!remoteStickHeld && stick.held) SendStickToBack();

        if (remoteMerged)
        {
            if (!merged) Merge();
        }
        else
        {
            if (merged) Unmerge();

            if (remoteMarshHeld && !marsh.held) GrabMarsh();
            else if (!remoteMarshHeld && marsh.held) SendMarshToBack();
        }

        if (player == null)
        {
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        if (!hasSetUp || !itemsReady) return;
        if (backTransform == null || stick.t == null || marsh.t == null) return;

        transform.localScale = Vector3.Scale(backTransform.lossyScale, rootBaseScale);
        transform.position = backTransform.TransformPoint(HolderPos);
        transform.rotation = backTransform.rotation * HolderRot;

        Vector3 rootScale = transform.lossyScale;
        stick.t.localScale = Vector3.Scale(rootScale, stick.baseScale);
        marsh.t.localScale = Vector3.Scale(rootScale, marsh.baseScale);

        if (stick.held && rightHandTransform != null) Follow(stick, rightHandTransform, StickHandPos, StickHandRot);
        else PlaceOnHolder(stick, StickBackPos, StickBackRot);

        if (merged)
        {
            Quaternion spotRot = stick.t.rotation * spotLocalRot;
            marsh.t.position = stick.t.TransformPoint(spotLocalPos) + spotRot * MergedPos;
            marsh.t.rotation = spotRot * MergedRot;
        }
        else if (marsh.held && leftHandTransform != null) Follow(marsh, leftHandTransform, MarshHandPos, MarshHandRot);
        else PlaceOnBelt(marsh, MarshBeltPos, MarshBeltRot);
    }

    private void PlaceOnHolder(Item it, Vector3 localPos, Quaternion localRot)
    {
        it.t.position = transform.TransformPoint(localPos);
        it.t.rotation = transform.rotation * localRot;
    }
    
    private void PlaceOnBelt(Item it, Vector3 localPos, Quaternion localRot)
    {
        if (beltTransform == null)
        {
            PlaceOnHolder(it, localPos, localRot);
            return;
        }

        it.t.position = beltTransform.TransformPoint(localPos);
        it.t.rotation = beltTransform.rotation * localRot;
    }

    private void Follow(Item it, Transform hand, Vector3 localPos, Quaternion localRot)
    {
        it.t.position = hand.TransformPoint(localPos);
        it.t.rotation = hand.rotation * localRot;
    }

    private void Hold(Item it, Transform hand, Vector3 localPos, Quaternion localRot)
    {
        it.t.position = hand.TransformPoint(localPos);
        it.t.rotation = hand.rotation * localRot;

        it.held = true;
    }

    private void GrabStick() => Hold(stick, rightHandTransform, StickHandPos, StickHandRot);

    private void GrabMarsh() => Hold(marsh, leftHandTransform, MarshHandPos, MarshHandRot);

    private void SendStickToBack()
    {
        stick.held = false;
    }

    private void SendMarshToBack()
    {
        marsh.held = false;
    }

    private bool CanMerge()
    {
        if (merged || !marsh.held) return false;
        if (Time.time - lastMergeChange < OwSystemSettings.MarshmallowMergeCooldown.Value) return false;
        return Vector3.Distance(marsh.t.position, stick.t.TransformPoint(spotLocalPos)) <= OwSystemSettings.MarshmallowMergeDistance.Value;
    }

    private void Merge()
    {
        marsh.held = false;
        merged = true;
        lastMergeChange = Time.time;
    }

    private void Unmerge()
    {
        merged = false;
        lastMergeChange = Time.time;
    }

    private bool IsCloseEnough(Vector3 itemPos, Vector3 handPos) =>
        Vector3.Distance(itemPos, handPos) <= OwSystemSettings.MarshmallowHoldDistance.Value;

    void OnDestroy()
    {
        if (stick.t != null) Destroy(stick.t.gameObject);
        if (marsh.t != null) Destroy(marsh.t.gameObject);

        if (!isLocal && Remote.TryGetValue(ownerActor, out var s) && s == this)
            Remote.Remove(ownerActor);

        if (isLocal && Local == this)
            Local = null;
    }

    public static GameObject BuildMarshmallowStick()
    {
        if (marshmallowStickAsset == null || marshmallowAsset == null) return null;

        var root = new GameObject("MarshmallowStick");

        var stick = Instantiate(marshmallowStickAsset);
        stick.name = "RoastingStick";
        stick.transform.SetParent(root.transform, false);
        stick.transform.localScale *= 0.6f;

        var spot = new GameObject("MarshMallowSpot");
        spot.transform.SetParent(stick.transform, false);
        spot.transform.localPosition = new Vector3(-0.006f, -0.009f, 1.85f);

        var marsh = Instantiate(marshmallowAsset);
        marsh.name = "Marshmallow";
        marsh.transform.SetParent(root.transform, false);
        marsh.transform.localScale *= 0.9f;

        return root;
    }
}