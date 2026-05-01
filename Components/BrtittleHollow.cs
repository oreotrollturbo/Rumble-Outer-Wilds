using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class BrittleHollow : MonoBehaviour
{
    public BrittleHollow(IntPtr ptr) : base(ptr) {}

    internal List<Transform>                   destructibleParts      = new();
    internal Dictionary<Transform, Vector3>    originalLocalPositions = new();
    internal Dictionary<Transform, Quaternion> originalLocalRotations = new();
    internal Dictionary<Transform, Vector3>    originalLocalScales    = new();
    internal Dictionary<Transform, Transform>  originalParents        = new();

    internal Transform blackHole;
    internal Transform whiteHole;

    internal float breakIntervalMin = 28f;
    internal float breakIntervalMax = 43f;
    internal float suckSpeed        = 1f;
    internal float suckStopDistance = 0.05f;
    internal float spitYRange       = 0.9f;
    internal float spitSpeed        = 0.1f;
    internal float driftSpeed       = 0.09f;
    internal float driftMaxRadius   = 9f;

    internal bool cancelled = false;

    private List<object> _activeCoroutines = new();

    void Start()
    {
        var bhRoot = transform.GetChild(0);
        for (int i = 0; i < bhRoot.childCount; i++)
        {
            Transform group = bhRoot.GetChild(i);
            if (group.gameObject.name.Contains("Unbreakable")) continue;

            for (int z = 0; z < group.childCount; z++)
            {
                Transform child = group.GetChild(z);
                destructibleParts.Add(child);
                originalLocalPositions[child] = child.localPosition;
                originalLocalRotations[child] = child.localRotation;
                originalLocalScales[child]    = child.localScale;
                originalParents[child]        = child.parent;
            }
        }

        Transform bht = transform.Find("BlackHole");
        if (bht != null)
            blackHole = bht;
        else
            MelonLogger.Warning("[BrittleHollow] Could not find BlackHole child.");

        if (Main.solarSystem.WhiteHole != null)
            whiteHole = Main.solarSystem.WhiteHole.transform;
        else
            MelonLogger.Error("[BrittleHollow] WhiteHole is null in SolarSystemData!");

        MelonLogger.Msg($"[BrittleHollow] Start — {destructibleParts.Count} destructible pieces found.");
        StartBreakRoutine();
    }

    public void SolarSystemRestart()
    {
        cancelled = true;

        foreach (var handle in _activeCoroutines)
        {
            if (handle == null) continue;
            try { MelonCoroutines.Stop(handle); }
            catch { }
        }
        _activeCoroutines.Clear();

        cancelled = false;

        if (Main.solarSystem.WhiteHole != null)
            whiteHole = Main.solarSystem.WhiteHole.transform;
        else
            MelonLogger.Error("[BrittleHollow] WhiteHole is null during SolarSystemRestart!");

        foreach (Transform piece in destructibleParts)
        {
            if (piece == null) continue;

            if (originalParents.TryGetValue(piece, out Transform originalParent) && originalParent != null)
                piece.SetParent(originalParent, worldPositionStays: false);

            if (originalLocalPositions.TryGetValue(piece, out Vector3 pos))
                piece.localPosition = pos;
            if (originalLocalRotations.TryGetValue(piece, out Quaternion rot))
                piece.localRotation = rot;
            if (originalLocalScales.TryGetValue(piece, out Vector3 scale))
                piece.localScale = scale;

            piece.gameObject.SetActive(true);
        }

        MelonLogger.Msg("[BrittleHollow] SolarSystemRestart — all pieces restored.");
        StartBreakRoutine();
    }

    private void StartBreakRoutine()
    {
        StartTracked(BrittleHollowCoroutines.BreakPiecesRoutine(this));
    }

    internal void StartTracked(IEnumerator routine)
    {
        object handle = MelonCoroutines.Start(routine);
        _activeCoroutines.Add(handle);
    }
}

internal static class BrittleHollowCoroutines
{
    private static bool IsSupernovaActive()
    {
        var sun = Main.solarSystem.Sun;
        if (sun == null) return false;
        var supernovaSun = sun.GetComponent<SupernovaSun>();
        if (supernovaSun == null) return false;
        return supernovaSun.currentPhase == SupernovaSun.Phase.Done;
    }

    public static IEnumerator BreakPiecesRoutine(BrittleHollow bh)
    {
        var remaining = new List<Transform>(bh.destructibleParts);
        MelonLogger.Msg($"[BrittleHollow] BreakPiecesRoutine started — {remaining.Count} pieces queued.");

        while (remaining.Count > 0)
        {
            if (bh.cancelled) yield break;

            float wait = Random.Range(bh.breakIntervalMin, bh.breakIntervalMax);
            MelonLogger.Msg($"[BrittleHollow] Next break in {wait:F1}s — {remaining.Count} pieces remaining.");
            yield return new WaitForSeconds(wait);

            if (bh.cancelled) yield break;

            int idx         = Random.Range(0, remaining.Count);
            Transform piece = remaining[idx];
            remaining.RemoveAt(idx);

            if (piece == null) continue;

            MelonLogger.Msg($"[BrittleHollow] Breaking piece: {piece.name}");
            bh.StartTracked(BreakPiece(bh, piece));
        }

        MelonLogger.Msg("[BrittleHollow] All pieces have been queued for breaking.");
    }

    public static IEnumerator BreakPiece(BrittleHollow bh, Transform piece)
    {
        if (bh.blackHole == null || bh.whiteHole == null)
        {
            MelonLogger.Warning($"[BrittleHollow] BreakPiece aborted ({piece?.name}) — blackHole or whiteHole is null.");
            yield break;
        }

        piece.SetParent(null, worldPositionStays: true);

        while (piece != null && bh.blackHole != null &&
               Vector3.Distance(piece.position, bh.blackHole.position) > bh.suckStopDistance)
        {
            if (bh.cancelled) yield break;
            Vector3 toBlackHole = (bh.blackHole.position - piece.position).normalized;
            piece.position += toBlackHole * (bh.suckSpeed * Time.deltaTime);
            yield return null;
        }

        if (piece == null || bh.whiteHole == null) yield break;

        piece.gameObject.SetActive(false);
        yield return new WaitForSeconds(Random.Range(0.3f, 1.0f));

        if (bh.cancelled) yield break;
        if (piece == null || bh.whiteHole == null) yield break;

        piece.SetParent(null, worldPositionStays: true);
        piece.position = bh.whiteHole.position;

        Vector3 targetOffset = new Vector3(
            Random.Range(-bh.spitYRange, bh.spitYRange),
            Random.Range(-bh.spitYRange, bh.spitYRange),
            Random.Range(-bh.spitYRange, bh.spitYRange)
        );

        piece.gameObject.SetActive(true);

        while (piece != null && bh.whiteHole != null)
        {
            if (bh.cancelled) yield break;
            Vector3 targetWorld = bh.whiteHole.position + targetOffset;
            if (Vector3.Distance(piece.position, targetWorld) <= 0.01f) break;
            piece.position = Vector3.MoveTowards(piece.position, targetWorld, bh.spitSpeed * Time.deltaTime);
            yield return null;
        }

        if (piece == null || bh.whiteHole == null) yield break;

        bh.StartTracked(DriftPiece(bh, piece));
    }

    private static IEnumerator DriftPiece(BrittleHollow bh, Transform piece)
    {
        if (piece == null) yield break;

        Vector3 driftDir = Random.onUnitSphere;
        float   dirTimer = Random.Range(3f, 6f);
        float   elapsed  = 0f;

        while (piece != null && bh.whiteHole != null)
        {
            if (bh.cancelled) yield break;

            if (IsSupernovaActive())
            {
                piece.gameObject.SetActive(false);
                yield break;
            }

            float dt = Time.deltaTime;
            elapsed += dt;

            piece.position += driftDir * (bh.driftSpeed * dt);

            Vector3 toWhiteHole = bh.whiteHole.position - piece.position;
            if (toWhiteHole.magnitude > bh.driftMaxRadius)
                piece.position += toWhiteHole.normalized * (bh.driftSpeed * dt * 2f);

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