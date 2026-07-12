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
    private Vector3 _worldOffset;
    private Quaternion _rotOffset;
    
    private Quaternion _tiltRotation  = Quaternion.identity;  // what you set in SceneLoaded — never changes
    private Quaternion _orbitalOffset = Quaternion.identity;  // accumulated orbital motion — changes every frame
    public SolarSystem(IntPtr ptr) : base(ptr) {}

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
        
    }

    public void SetRelativeTo(GameObject planet) =>
        SetRelativeTo(planet != null ? planet.transform : null);

    private Orbiter _anchorOrbiter;
    private EllipticalOrbiter _anchorEllipticalOrbiter;
    private float _initialSpinAngle;
    private float _lastSpinAngle;
    private float _lastOrbitAngle; // Added to track orbital progress alongside spin
    private Quaternion _lastEllipticalRotation;           

    public void SetRelativeTo(Transform planet)
    {
        _tiltRotation  = transform.rotation;
        _orbitalOffset = Quaternion.identity;
        // 1. Re-enable OLD anchor children first
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
            // 2. Disable NEW anchor children after
            for (int i = 0; i < planet.childCount; i++)
            {
                Transform child = planet.GetChild(i);
                if (child != null)
                {
                    MelonLogger.Msg("Got child disabling");
                    child.gameObject.SetActive(false);
                }
            }

            _anchorOrbiter           = planet.GetComponent<Orbiter>();
            _anchorEllipticalOrbiter = planet.GetComponent<EllipticalOrbiter>();

            _initialSpinAngle = _anchorOrbiter != null ? _anchorOrbiter._currentSpinAngle : 0f;
            _lastSpinAngle    = _initialSpinAngle;
            _lastOrbitAngle   = _anchorOrbiter != null ? _anchorOrbiter._currentOrbitAngle : 0f; // Initialized here

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
            float currentSpinAngle  = _anchorOrbiter._currentSpinAngle;
            float currentOrbitAngle = _anchorOrbiter._currentOrbitAngle;

            // Combine both deltas to account for zero-spin bodies like Dark Bramble
            float deltaAngle = (currentSpinAngle - _lastSpinAngle) + (currentOrbitAngle - _lastOrbitAngle);
            _lastSpinAngle   = currentSpinAngle;
            _lastOrbitAngle  = currentOrbitAngle;

            Quaternion deltaRotation = Quaternion.AngleAxis(deltaAngle, _tiltRotation * Vector3.up);
            _orbitalOffset     = deltaRotation * _orbitalOffset;
            transform.position = deltaRotation * transform.position;
            transform.rotation = _orbitalOffset * _tiltRotation;
        }
        else if (_anchorEllipticalOrbiter != null)
        {
            Quaternion currentRot    = relativeToPlanet.rotation;
            Quaternion deltaRotation = currentRot * Quaternion.Inverse(_lastEllipticalRotation);
            _lastEllipticalRotation  = currentRot;

            _orbitalOffset     = deltaRotation * _orbitalOffset;
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
                    GameObject.Find("SCENE").transform.GetChild(4).gameObject.SetActive(false);
                    GameObject.Find("SCENE").transform.GetChild(3).gameObject.SetActive(false);
                    break;

                case "Map0":
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    SetRelativeTo(Main.solarSystem.HourGlassTwins);
                    GameObject.Find("Scene").transform.GetChild(0).gameObject.SetActive(false);
                    GameObject.Find("Scene").transform.GetChild(2).gameObject.SetActive(false);
                    break;

                case "Map1":
                    transform.rotation = Quaternion.Euler(0, 90, 0);
                    SetRelativeTo(Main.solarSystem.BrittleHollow);
                    break;

                case "Park":
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    SetRelativeTo(Main.solarSystem.HearthianMapSatelite);
                    GameObject.Find("SCENE").transform.GetChild(0).gameObject.SetActive(false);
                    GameObject.Find("SCENE").transform.GetChild(3).gameObject.SetActive(false);
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
}