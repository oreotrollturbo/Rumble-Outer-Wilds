using System;
using MelonLoader;
using UnityEngine;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SolarSystem : MonoBehaviour
{
    public Transform relativeToPlanet = null;
    private Vector3 _anchorFixedPosition;
    private Quaternion _anchorFixedRotation;
    private Quaternion _lastAnchorRotation;
    private Vector3 _worldOffset;       // SolarSystem position relative to planet (world-space, set once)
    private Quaternion _rotOffset;      // SolarSystem rotation relative to planet (set once)
    public SolarSystem(IntPtr ptr) : base(ptr) {}

    public void Start()
    {
        Actions.onMapInitialized += SceneLoaded;

        transform.position = new Vector3(
            OwSystemSettings.SolarSystemGymX.Value,
            OwSystemSettings.SolarSystemGymY.Value,
            OwSystemSettings.SolarSystemGymZ.Value);
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    

    public void SetRelativeTo(GameObject planet) =>
        SetRelativeTo(planet != null ? planet.transform : null);

    private Vector3 _anchorTargetPos;

    public void SetRelativeTo(Transform planet)
    {
        relativeToPlanet = planet;

        if (planet != null)
        {
            _anchorTargetPos = planet.position;
            MelonLogger.Msg($"[SolarSystem] Anchored to '{planet.name}' at {_anchorTargetPos}");
        }
        else
        {
            MelonLogger.Msg("[SolarSystem] Relative orbit disabled.");
        }
    }

// FixedUpdate matches Orbiter's phase — no LateUpdate
    void FixedUpdate()
    {
        if (relativeToPlanet == null) return;

        Vector3 drift = _anchorTargetPos - relativeToPlanet.position;
        transform.position += drift;
        // Rotation is intentionally untouched — TimberHearth's rotation
        // already derives from SolarSystem.rotation via its Orbiter,
        // so copying it back would create a feedback loop.
    }

    public void SceneLoaded(string mapName)
    {
        relativeToPlanet = null;

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

    public void StartSolarSystem()
    {
        if (Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().hasFiredBefore)
            Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().Restart();

        Main.solarSystem.BrittleHollow.GetComponent<BrittleHollow>().SolarSystemRestart();
        Main.solarSystem.HollowsLantern.GetComponent<HollowsLantern>().SolarSystemRestart();
        Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().StartFiringSequence();
        
        SetRelativeTo(Main.solarSystem.TimberHearth);
        Main.solarSystem.TimberHearth.GetComponent<Orbiter>().disableOrbit = false;
    }
}