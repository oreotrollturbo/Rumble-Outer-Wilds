using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class HollowsLantern : MonoBehaviour
{
    public HollowsLantern(IntPtr ptr) : base(ptr) { }

    // ── Lava ────────────────────────────────────────────────────────────────
    private Transform lavaTransform;
    public Vector3 lavaStartScale         = new Vector3(89f, 89f, 89f);
    public Vector3 targetLavaScale        = new Vector3(84f, 84f, 84f);
    public float   lavaShrinkDuration     = 60f * 19;
    private float  lavaElapsed            = 0f;

    // ── Volcanos / meteors ───────────────────────────────────────────────────
    private Transform  volcanosTransform;
    private GameObject meteorPrefab;
    private Transform  brittleHollowTransform;
    private Transform  whiteHoleTransform;

    public float meteorSpawnIntervalMin    = 6f;
    public float meteorSpawnIntervalMax    = 40f;
    public float meteorSpawnIntervalMaxEnd = 20f;
    public float meteorDriftSpeed          = 0.7f;
    public float meteorPullSpeed           = 1.4f;
    public float meteorPullRadius          = 40f;
    public float meteorDestroyRadius       = 0.03f;

    // ── Survivor / white-hole drift ──────────────────────────────────────────
    // Chance (0–1) that a meteor survives impact and drifts from the white hole
    public float survivorChance      = 0.15f;
    public float survivorSpitRange   = 0.9f;   // matches BrittleHollow.spitYRange
    public float survivorSpitSpeed   = 0.1f;   // matches BrittleHollow.spitSpeed
    public float survivorDriftSpeed  = 0.09f;  // matches BrittleHollow.driftSpeed
    public float survivorDriftRadius = 9f;     // matches BrittleHollow.driftMaxRadius

    // ── Internal state ───────────────────────────────────────────────────────
    private bool             _cancelled         = false;
    private List<object>     _activeCoroutines  = new();
    private List<GameObject> _survivingMeteors  = new();   // meteors drifting near white hole

    // ────────────────────────────────────────────────────────────────────────

    void Start()
    {
        lavaTransform     = transform.GetChild(1);
        volcanosTransform = transform.GetChild(2);

        lavaTransform.localScale = lavaStartScale;

        meteorPrefab           = Main.solarSystem.LanternMeteor;
        brittleHollowTransform = Main.solarSystem.BrittleHollow.transform;

        if (Main.solarSystem.WhiteHole != null)
            whiteHoleTransform = Main.solarSystem.WhiteHole.transform;
        else
            MelonLogger.Warning("[HollowsLantern] WhiteHole is null — survivor meteors disabled.");

        StartTracked(SpawnMeteorLoop());
    }

    // ── Solar-system reset ───────────────────────────────────────────────────
    public void SolarSystemRestart()
    {
        if (lavaTransform == null) return;
        _cancelled = true;

        foreach (var handle in _activeCoroutines)
        {
            if (handle == null) continue;
            try { MelonCoroutines.Stop(handle); } catch { }
        }
        _activeCoroutines.Clear();

        // Destroy any meteors still drifting near the white hole
        foreach (var meteor in _survivingMeteors)
        {
            if (meteor != null)
                GameObject.Destroy(meteor);
        }
        _survivingMeteors.Clear();

        // Refresh white-hole reference in case the scene reloaded
        if (Main.solarSystem.WhiteHole != null)
            whiteHoleTransform = Main.solarSystem.WhiteHole.transform;

        // Reset lava
        lavaElapsed = 0f;
        lavaTransform.localScale = lavaStartScale;

        _cancelled = false;
        StartTracked(SpawnMeteorLoop());

        MelonLogger.Msg("[HollowsLantern] SolarSystemRestart complete.");
    }

    // ── Lava shrink ──────────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (lavaElapsed < lavaShrinkDuration)
        {
            lavaElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(lavaElapsed / lavaShrinkDuration);
            lavaTransform.localScale = Vector3.Lerp(lavaStartScale, targetLavaScale, t);
        }
    }

    // ── Coroutine tracking ───────────────────────────────────────────────────
    private void StartTracked(IEnumerator routine)
    {
        object handle = MelonCoroutines.Start(routine);
        _activeCoroutines.Add(handle);
    }

    // ── Meteor spawning ──────────────────────────────────────────────────────
    private IEnumerator SpawnMeteorLoop()
    {
        while (!_cancelled)
        {
            float t          = Mathf.Clamp01(lavaElapsed / lavaShrinkDuration);
            float currentMax = Mathf.Lerp(meteorSpawnIntervalMax, meteorSpawnIntervalMaxEnd, t);
            float delay      = Random.Range(meteorSpawnIntervalMin, currentMax);
            yield return new WaitForSeconds(delay);

            if (!_cancelled)
                SpawnMeteor();
        }
    }

    private void SpawnMeteor()
    {
        if (meteorPrefab == null || volcanosTransform == null || brittleHollowTransform == null)
            return;

        int childCount = volcanosTransform.childCount;
        if (childCount == 0) return;

        Transform launchPoint = volcanosTransform.GetChild(Random.Range(0, childCount));

        GameObject meteor = GameObject.Instantiate(meteorPrefab,
                                                   launchPoint.position,
                                                   launchPoint.rotation);

        meteor.transform.SetParent(brittleHollowTransform, worldPositionStays: true);

        Vector3 driftDir = Random.onUnitSphere;
        StartTracked(DriveMeteor(meteor, driftDir));
    }

    // ── Per-meteor movement ──────────────────────────────────────────────────
    private IEnumerator DriveMeteor(GameObject meteor, Vector3 driftDir)
    {
        while (meteor != null && !_cancelled)
        {
            Vector3 hollowCenter = brittleHollowTransform.TransformPoint(Vector3.zero);
            float   distToCenter = Vector3.Distance(meteor.transform.position, hollowCenter);

            if (distToCenter <= meteorDestroyRadius)
            {
                // Small chance: survive and drift from the white hole instead
                if (whiteHoleTransform != null && Random.value < survivorChance)
                {
                    // Detach from BrittleHollow before handing off
                    meteor.transform.SetParent(null, worldPositionStays: true);
                    _survivingMeteors.Add(meteor);
                    StartTracked(SurvivorMeteorRoutine(meteor));
                }
                else
                {
                    GameObject.Destroy(meteor);
                }
                yield break;
            }

            if (distToCenter <= meteorPullRadius)
            {
                Vector3 toCenter = (hollowCenter - meteor.transform.position).normalized;
                meteor.transform.position += toCenter * (meteorPullSpeed * Time.deltaTime);
            }
            else
            {
                meteor.transform.position += driftDir * (meteorDriftSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    // ── Survivor: spit out of white hole then drift ──────────────────────────
    private IEnumerator SurvivorMeteorRoutine(GameObject meteor)
    {
        if (meteor == null || whiteHoleTransform == null) yield break;

        // Teleport to the white hole
        meteor.transform.position = whiteHoleTransform.position;

        // Spit toward a random nearby offset (mirrors BrittleHollow.BreakPiece)
        Vector3 targetOffset = new Vector3(
            Random.Range(-survivorSpitRange, survivorSpitRange),
            Random.Range(-survivorSpitRange, survivorSpitRange),
            Random.Range(-survivorSpitRange, survivorSpitRange)
        );

        while (meteor != null && whiteHoleTransform != null && !_cancelled)
        {
            Vector3 targetWorld = whiteHoleTransform.position + targetOffset;
            if (Vector3.Distance(meteor.transform.position, targetWorld) <= 0.01f) break;
            meteor.transform.position = Vector3.MoveTowards(
                meteor.transform.position, targetWorld, survivorSpitSpeed * Time.deltaTime);
            yield return null;
        }

        if (meteor == null || _cancelled) yield break;

        // Drift around the white hole (mirrors BrittleHollow.DriftPiece)
        Vector3 driftDir  = Random.onUnitSphere;
        float   dirTimer  = Random.Range(3f, 6f);
        float   elapsed   = 0f;

        while (meteor != null && whiteHoleTransform != null && !_cancelled)
        {
            float dt  = Time.deltaTime;
            elapsed  += dt;

            meteor.transform.position += driftDir * (survivorDriftSpeed * dt);

            // Soft leash: nudge back if it strays too far
            Vector3 toWhiteHole = whiteHoleTransform.position - meteor.transform.position;
            if (toWhiteHole.magnitude > survivorDriftRadius)
                meteor.transform.position += toWhiteHole.normalized * (survivorDriftSpeed * dt * 2f);

            if (elapsed >= dirTimer)
            {
                driftDir = Random.onUnitSphere;
                dirTimer = Random.Range(3f, 6f);
                elapsed  = 0f;
            }

            yield return null;
        }

        // Coroutine ended due to cancellation or null refs; the Destroy is handled by SolarSystemRestart
    }
}