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
    public Vector3 lavaStartScale     = new Vector3(89f, 89f, 89f);
    public Vector3 targetLavaScale    = new Vector3(84f, 84f, 84f);
    public float   lavaShrinkDuration = 60f * 19;
    private float  lavaElapsed        = 0f;

    // ── Volcanos / meteors ───────────────────────────────────────────────────
    private Transform  volcanosTransform;
    private GameObject meteorPrefab;
    private Transform  brittleHollowTransform;
    private Transform  whiteHoleTransform;

    public float meteorSpawnIntervalMin    = 6f;
    public float meteorSpawnIntervalMax    = 40f;
    public float meteorSpawnIntervalMaxEnd = 20f;
    public float meteorDriftSpeed          = 0.7f;   // speed during free-drift phase
    public float meteorPullSpeed           = 1.4f;   // speed once pull phase begins
    public float meteorDestroyRadius       = 0.02f;

    // ── Free-drift phase ─────────────────────────────────────────────────────
    // Each meteor drifts aimlessly for a random duration, then switches to pull.
    public float meteorFreeTimeMin         = 2f;    // min seconds before pull starts
    public float meteorFreeTimeMax         = 50f;   // max seconds before pull starts
    // Target float shell: Lantern's own orbital distance from BrittleHollow, ±this
    public float meteorOrbitRadiusVariance = 15f;
    // Spring strength pulling the meteor back toward its target shell radius.
    // Keep small — drift should dominate within the band (crossover ~9 units at defaults).
    public float meteorRadialCorrection    = 0.08f;
    // How often each meteor picks a brand-new random direction (aimless wandering)
    public float meteorDirChangeMin        = 1.5f;
    public float meteorDirChangeMax        = 5f;

    // ── Survivor / white-hole drift ──────────────────────────────────────────
    public float survivorChance      = 0.15f;
    public float survivorSpitRange   = 0.9f;
    public float survivorSpitSpeed   = 0.1f;
    public float survivorDriftSpeed  = 0.09f;
    public float survivorDriftRadius = 9f;

    // ── Internal state ───────────────────────────────────────────────────────
    private bool             _cancelled        = false;
    private List<object>     _activeCoroutines = new();
    private List<GameObject> _survivingMeteors = new();

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

        foreach (var meteor in _survivingMeteors)
        {
            if (meteor != null)
                GameObject.Destroy(meteor);
        }
        _survivingMeteors.Clear();

        if (Main.solarSystem.WhiteHole != null)
            whiteHoleTransform = Main.solarSystem.WhiteHole.transform;

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

        Transform  launchPoint = volcanosTransform.GetChild(Random.Range(0, childCount));
        GameObject meteor      = GameObject.Instantiate(meteorPrefab,
                                                        launchPoint.position,
                                                        launchPoint.rotation);

        meteor.transform.SetParent(brittleHollowTransform, worldPositionStays: true);

        StartTracked(DriveMeteor(meteor, Random.onUnitSphere));
    }

    // ── Per-meteor movement ──────────────────────────────────────────────────
    private IEnumerator DriveMeteor(GameObject meteor, Vector3 driftDir)
    {
        if (meteor == null || brittleHollowTransform == null) yield break;

        // Each meteor gets its own target shell radius: Lantern's orbital distance ± variance.
        // Computed once at spawn so it stays consistent even if the Lantern moves later.
        Vector3 hollowCenter = brittleHollowTransform.TransformPoint(Vector3.zero);
        float   baseRadius   = Vector3.Distance(transform.position, hollowCenter);
        float   targetRadius = Mathf.Max(5f,
            baseRadius + Random.Range(-meteorOrbitRadiusVariance, meteorOrbitRadiusVariance));

        // Each meteor decides independently when it will get pulled in.
        float freeTime   = Random.Range(meteorFreeTimeMin, meteorFreeTimeMax);
        float elapsed    = 0f;

        // Direction-change bookkeeping (free-drift phase only).
        float dirTimer   = Random.Range(meteorDirChangeMin, meteorDirChangeMax);
        float dirElapsed = 0f;

        while (meteor != null && !_cancelled)
        {
            float dt     = Time.deltaTime;
            hollowCenter = brittleHollowTransform.TransformPoint(Vector3.zero);
            elapsed     += dt;
            dirElapsed  += dt;

            if (elapsed < freeTime)
            {
                // ── Free-drift phase ───────────────────────────────────────
                //
                // Roll a wholly random new direction every so often.
                // Random.onUnitSphere is not tangential, so there is no
                // systematic circulation — just messy wandering.
                if (dirElapsed >= dirTimer)
                {
                    driftDir   = Random.onUnitSphere;
                    dirTimer   = Random.Range(meteorDirChangeMin, meteorDirChangeMax);
                    dirElapsed = 0f;
                }

                meteor.transform.position += driftDir * (meteorDriftSpeed * dt);

                // Soft radial spring: gently nudges the meteor back toward its
                // target shell. The spring is intentionally weak — at default
                // values, drift beats it within ~9 units of the shell, so the
                // meteor wanders freely through the band rather than tracking it.
                //   correction/s = radialError × meteorRadialCorrection
                //   drift/s      = meteorDriftSpeed (0.7)
                //   crossover    = 0.7 / 0.08 ≈ 9 units
                float   dist        = Vector3.Distance(meteor.transform.position, hollowCenter);
                Vector3 fromCenter  = (meteor.transform.position - hollowCenter).normalized;
                float   radialError = dist - targetRadius;
                meteor.transform.position -= fromCenter * (radialError * meteorRadialCorrection * dt);
            }
            else
            {
                // ── Pull phase ─────────────────────────────────────────────
                float dist = Vector3.Distance(meteor.transform.position, hollowCenter);

                if (dist <= meteorDestroyRadius)
                {
                    if (whiteHoleTransform != null && Random.value < survivorChance)
                    {
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

                Vector3 toCenter = (hollowCenter - meteor.transform.position).normalized;
                meteor.transform.position += toCenter * (meteorPullSpeed * dt);
            }

            yield return null;
        }
    }

    // ── Survivor: spit out of white hole then drift ──────────────────────────
    private IEnumerator SurvivorMeteorRoutine(GameObject meteor)
    {
        if (meteor == null || whiteHoleTransform == null) yield break;

        meteor.transform.position = whiteHoleTransform.position;

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

        Vector3 driftDir = Random.onUnitSphere;
        float   dirTimer = Random.Range(3f, 6f);
        float   elapsed  = 0f;

        while (meteor != null && whiteHoleTransform != null && !_cancelled)
        {
            float dt  = Time.deltaTime;
            elapsed  += dt;

            meteor.transform.position += driftDir * (survivorDriftSpeed * dt);

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
    }
}