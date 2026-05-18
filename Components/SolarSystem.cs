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
    private Orbiter _anchorOrbiter;
    private Quaternion _baseRotation;
    private float _initialSpinAngle;

    public void SetRelativeTo(Transform planet)
    {
        if (_anchorOrbiter != null)
        {
            _anchorOrbiter.customRotation = null; // restore TH's normal spin visuals
            _anchorOrbiter = null;
        }

        relativeToPlanet = planet;

        if (planet != null)
        {
            _anchorTargetPos  = planet.position;
            _anchorOrbiter    = planet.GetComponent<Orbiter>();
            _baseRotation     = transform.rotation;
            _initialSpinAngle = _anchorOrbiter != null ? _anchorOrbiter._currentSpinAngle : 0f;

            if (_anchorOrbiter != null)
                // Freeze TH's visual rotation but let _currentSpinAngle keep ticking
                _anchorOrbiter.customRotation = planet.rotation;

            MelonLogger.Msg($"[SolarSystem] Anchored to '{planet.name}'");
        }
        else
        {
            MelonLogger.Msg("[SolarSystem] Relative orbit disabled.");
        }
    }

    void FixedUpdate()
    {
        Vector3 drift = _anchorTargetPos - relativeToPlanet.position;
        transform.position += drift;

        // _currentSpinAngle still ticks (spinEnabled untouched), driving SolarSystem rotation
        // but TH's own transform.rotation is frozen via customRotation
        if (_anchorOrbiter != null)
        {
            float spinDelta = _anchorOrbiter._currentSpinAngle - _initialSpinAngle;
            transform.rotation = _baseRotation * Quaternion.AngleAxis(spinDelta, _anchorOrbiter.spinAxis);
        }
    }

    public void SceneLoaded(string mapName)
    {
        if (_anchorOrbiter != null)
        {
            //_anchorOrbiter.customRotation = null;
            //_anchorOrbiter = null;
        }
        //relativeToPlanet = null;

        switch (mapName)
        {
            case "Gym":
                transform.position = new Vector3(
                    OwSystemSettings.SolarSystemGymX.Value,
                    OwSystemSettings.SolarSystemGymY.Value,
                    OwSystemSettings.SolarSystemGymZ.Value);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                
                GameObject.Find("SCENE").transform.GetChild(4).gameObject.SetActive(false);
                GameObject.Find("SCENE").transform.GetChild(3).gameObject.SetActive(false);
                
                break;

            case "Map0":
                transform.position = new Vector3(
                    OwSystemSettings.SolarSystemRingX.Value,
                    OwSystemSettings.SolarSystemRingY.Value,
                    OwSystemSettings.SolarSystemRingZ.Value);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                
                GameObject.Find("Scene").transform.GetChild(0).gameObject.SetActive(false);
                GameObject.Find("Scene").transform.GetChild(2).gameObject.SetActive(false);
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
                
                GameObject.Find("SCENE").transform.GetChild(0).gameObject.SetActive(false);
                GameObject.Find("SCENE").transform.GetChild(3).gameObject.SetActive(false);
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

    void Scale(float scale)
    {
        SolarSystemScaler.Apply(scale);
    }
}