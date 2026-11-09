using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace OuterWildsRumble.Components;

/// <summary>
/// Realistic mode: pins one planet to the world origin and makes the rest of the system
/// (and the sky) move around it, including counter-rotating the sky against the planet's spin.
/// Lives on the same GameObject as <see cref="SolarSystem"/> and drives its transform.
/// </summary>
[RegisterTypeInIl2Cpp]
public class SolarSystemRealisticMode : MonoBehaviour
{
    public Transform relativeToPlanet = null;

    private Quaternion _tiltRotation = Quaternion.identity;
    private Quaternion _orbitalOffset = Quaternion.identity;

    // World-space axis the sky counter-rotates around while anchored: the anchor planet's own SPIN axis
    // (the planet is pinned in world space, so its spin is applied to the sky in reverse).
    private Vector3 _anchorAxis = Vector3.up;

    private Orbiter _anchorOrbiter;
    private EllipticalOrbiter _anchorEllipticalOrbiter;
    private float _lastSpinAngle;
    private Quaternion _lastEllipticalRotation;

    public void SetRelativeTo(GameObject planet) =>
        SetRelativeTo(planet != null ? planet.transform : null);

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
                    child.gameObject.SetActive(false);
                }
            }

            _anchorOrbiter = planet.GetComponent<Orbiter>();
            _anchorEllipticalOrbiter = planet.GetComponent<EllipticalOrbiter>();

            // Spin axis in world space (Orbiter spins about its LOCAL spinAxis, so rotate it by the planet's rotation)
            _anchorAxis = _anchorOrbiter != null
                ? planet.rotation * _anchorOrbiter.spinAxis.normalized
                : _tiltRotation * Vector3.up;

            _lastSpinAngle = _anchorOrbiter != null ? _anchorOrbiter._currentSpinAngle : 0f;

            if (_anchorEllipticalOrbiter != null)
                _lastEllipticalRotation = planet.rotation;

            Vector3 offsetToOrigin = Vector3.zero - planet.position;
            transform.position += offsetToOrigin;

            if (_anchorOrbiter != null)
                _anchorOrbiter.customRotation = planet.rotation;

            MelonLogger.Msg($"[SolarSystem] Anchored '{planet.name}'.");
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
            // The anchor planet is pinned in world space, so its spin shows up as the sky turning the
            // opposite way around its spin axis. The planet's ORBIT is already handled by the Orbiter moving it
            // around the sun and us re-centring on it below, so it must NOT be added here (that double counted it).
            float currentSpinAngle = _anchorOrbiter._currentSpinAngle;
            float spinDelta = Mathf.DeltaAngle(_lastSpinAngle, currentSpinAngle);
            _lastSpinAngle = currentSpinAngle;

            Quaternion deltaRotation = Quaternion.AngleAxis(-spinDelta, _anchorAxis);
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

    /// <summary>
    /// Realistic-mode setup for a freshly loaded map: places the system and anchors the right planet.
    /// </summary>
    public void ApplyScene(string mapName)
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

                var scene = GameObject.Find("SCENE");
                if (scene != null)
                {
                    scene.transform.GetChild(2).gameObject.SetActive(false);
                    scene.transform.GetChild(3).gameObject.SetActive(false);
                }

                var vfx = GameObject.Find("SCENE VFX/SFX");
                if (vfx != null) vfx.SetActive(false);
                break;
        }
    }

    // ─────────────── Hooks used by the networking component ───────────────

    /// <summary>Current frame of reference, for sending to other clients. False when nothing is anchored.</summary>
    [HideFromIl2Cpp]
    public bool TryGetFrame(out Quaternion systemRotation, out bool hasCustom, out Quaternion customRotation)
    {
        systemRotation = transform.rotation;
        hasCustom = false;
        customRotation = Quaternion.identity;

        if (relativeToPlanet == null) return false;

        if (_anchorOrbiter != null && _anchorOrbiter.customRotation.HasValue)
        {
            hasCustom = true;
            customRotation = _anchorOrbiter.customRotation.Value;
        }

        return true;
    }

    /// <summary>Mirrors the host's frame (system rotation + pinned anchor planet) so both clients see the same sky.</summary>
    public void ApplyFrame(Quaternion rootRot, bool hasCustom, Quaternion customRot)
    {
        _tiltRotation = rootRot;
        _orbitalOffset = Quaternion.identity;
        transform.rotation = rootRot;

        if (_anchorOrbiter != null)
        {
            if (hasCustom) _anchorOrbiter.customRotation = customRot;

            // Spin axis in world space, from the planet's pinned rotation
            Quaternion anchorRot = hasCustom ? customRot : _anchorOrbiter.transform.rotation;
            _anchorAxis = anchorRot * _anchorOrbiter.spinAxis.normalized;
        }
    }
    
    public void ResyncSpinBaseline()
    {
        if (_anchorOrbiter != null)
            _lastSpinAngle = _anchorOrbiter._currentSpinAngle;
    }
}