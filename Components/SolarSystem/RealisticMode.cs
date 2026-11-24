using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class RealisticMode : MonoBehaviour
{
    private static readonly Vector3 GymSpotOffset = new Vector3(0.5f, 0.5f, 0);
    private const float GymSpotHeading = 0f;
    private const float GymSurfaceRadius = 0f;
    private static readonly Vector3 GymPositionOffset = Vector3.zero;

    private static readonly Vector3 RingSpotOffset = Vector3.up;
    private const float RingSpotHeading = 0f;
    private const float RingSurfaceRadius = 0f;
    private static readonly Vector3 RingPositionOffset = new Vector3(0, 0.001f, 0);

    private static readonly Vector3 PitSpotOffset = Vector3.forward;
    private const float PitSpotHeading = 90f;
    private const float PitSurfaceRadius = 0f;
    private static readonly Vector3 PitPositionOffset = new Vector3(0, 0, -0.36f);

    private static readonly Vector3 ParkSpotOffset = Vector3.forward;
    private const float ParkSpotHeading = 0f;
    private const float ParkSurfaceRadius = 0f;
    private static readonly Vector3 ParkPositionOffset = Vector3.zero;

    public Transform relativeToPlanet;

    private Quaternion _tiltRotation = Quaternion.identity;
    private Quaternion _orbitalOffset = Quaternion.identity;
    private Quaternion _anchorWorld = Quaternion.identity;

    private Vector3 _spotLocalOffset = Vector3.up;
    private float _spotHeading = 0f;
    private float _spotSurfaceRadius = 0f;
    private Vector3 _spotPositionOffset = Vector3.zero;
    private float _surfaceLocal = 0f;

    private Orbiter _anchorOrbiter;
    private Orbiter _anchorCarrier;
    private EllipticalOrbiter _anchorEllipticalOrbiter;
    private Quaternion _lastEllipticalRotation;

    private static readonly List<MeshCollider> AnchorColliders = new List<MeshCollider>();

    private readonly List<GameObject> _disabledSatelliteChildren = new List<GameObject>();

    public void SetRelativeTo(GameObject planet) =>
        SetRelativeTo(planet != null ? planet.transform : null, _spotLocalOffset, _spotHeading, _spotSurfaceRadius,
            _spotPositionOffset);

    public void SetRelativeTo(Transform planet) =>
        SetRelativeTo(planet, _spotLocalOffset, _spotHeading, _spotSurfaceRadius, _spotPositionOffset);

    [HideFromIl2Cpp]
    public void SetRelativeTo(Transform planet, Vector3 localOffset, float heading, float surfaceRadius = 0f,
        Vector3 positionOffset = default)
    {
        _spotLocalOffset = localOffset;
        _spotHeading = heading;
        _spotSurfaceRadius = surfaceRadius;
        _spotPositionOffset = positionOffset;

        RemoveAnchorColliders();
        RestoreSatelliteChildren();

        if (_anchorOrbiter != null)
            _anchorOrbiter.customRotation = null;

        _anchorOrbiter = null;
        _anchorCarrier = null;
        _anchorEllipticalOrbiter = null;

        relativeToPlanet = planet;
        _surfaceLocal = 0f;
        _anchorWorld = Quaternion.identity;
        _orbitalOffset = Quaternion.identity;

        if (planet != null)
        {
            _anchorEllipticalOrbiter = planet.GetComponent<EllipticalOrbiter>();
            _anchorOrbiter = planet.GetComponent<Orbiter>();

            if (_anchorEllipticalOrbiter == null)
                _anchorCarrier = FindCarrier(planet);

            if (_anchorCarrier != null)
            {
                _anchorWorld = BuildAnchorWorld(localOffset, heading);

                transform.rotation = _anchorWorld * Quaternion.Inverse(GetAnchorSystemRotation());
                _tiltRotation = transform.rotation;

                _anchorCarrier.transform.rotation = transform.rotation * _anchorCarrier.GetSystemSpinRotation();

                if (_anchorOrbiter != null)
                    _anchorOrbiter.customRotation = _anchorWorld;

                _surfaceLocal = MeasureSurface(planet, surfaceRadius);
            }
            else
            {
                _tiltRotation = transform.rotation;

                if (_anchorEllipticalOrbiter != null)
                    _lastEllipticalRotation = planet.rotation;
            }

            AddAnchorColliders(planet);

            transform.position += SurfaceTarget() - planet.position;

            // Satellite as the parent: disable all of its children (restored on the next anchor change).
            if (Main.solarSystem.HearthianMapSatelite != null &&
                planet == Main.solarSystem.HearthianMapSatelite.transform)
            {
                DisableSatelliteChildren(planet);
            }

            MelonLogger.Msg(
                $"[SolarSystem] Anchored '{planet.name}' (surface distance {_surfaceLocal * planet.lossyScale.x:F3}).");
        }
        else
        {
            _tiltRotation = transform.rotation;
            MelonLogger.Msg("[SolarSystem] Relative orbit disabled.");
        }
    }

    private void DisableSatelliteChildren(Transform planet)
    {
        RestoreSatelliteChildren();

        for (int i = 0; i < planet.childCount; i++)
        {
            Transform child = planet.GetChild(i);
            if (child == null) continue;

            GameObject go = child.gameObject;
            if (!go.activeSelf) continue; // leave already-inactive children alone

            go.SetActive(false);
            _disabledSatelliteChildren.Add(go);
        }

        MelonLogger.Msg($"[SolarSystem] Disabled {_disabledSatelliteChildren.Count} satellite children.");
    }

    private void RestoreSatelliteChildren()
    {
        for (int i = 0; i < _disabledSatelliteChildren.Count; i++)
        {
            GameObject go = _disabledSatelliteChildren[i];
            if (go != null)
                go.SetActive(true);
        }

        _disabledSatelliteChildren.Clear();
    }

    private static Orbiter FindCarrier(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            Orbiter orbiter = t.GetComponent<Orbiter>();
            if (orbiter != null) return orbiter;
            t = t.parent;
        }

        return null;
    }

    private Quaternion GetAnchorSystemRotation()
    {
        Transform carrierTransform = _anchorCarrier.transform;

        Quaternion chain = Quaternion.identity;
        Transform t = relativeToPlanet;
        while (t != null && t != carrierTransform)
        {
            chain = t.localRotation * chain;
            t = t.parent;
        }

        return _anchorCarrier.GetSystemSpinRotation() * chain;
    }

    private static Quaternion BuildAnchorWorld(Vector3 localOffset, float heading)
    {
        Vector3 dir = localOffset.sqrMagnitude > 0.000001f ? localOffset.normalized : Vector3.up;

        return Quaternion.AngleAxis(heading, Vector3.up) * Quaternion.FromToRotation(dir, Vector3.up);
    }

    private Vector3 SurfaceTarget()
    {
        if (relativeToPlanet == null) return Vector3.zero;

        Vector3 offset = _anchorWorld * (_spotPositionOffset * OwSystemSettings.SolarSystemScale.Value);

        return Vector3.down * (_surfaceLocal * relativeToPlanet.lossyScale.x) - offset;
    }

    private static void RemoveAnchorColliders()
    {
        for (int i = 0; i < AnchorColliders.Count; i++)
        {
            MeshCollider collider = AnchorColliders[i];
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
        }

        AnchorColliders.Clear();
    }

    private static void AddAnchorColliders(Transform planet)
    {
        for (int i = 0; i < planet.childCount; i++)
        {
            Transform child = planet.GetChild(i);
            if (child == null) continue;

            MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                collider.sharedMesh = filter.sharedMesh;

            AnchorColliders.Add(collider);
        }
    }

    private static float MeasureSurface(Transform planet, float radiusOverride)
    {
        float scale = planet.lossyScale.x;
        if (scale <= 0f) return 0f;

        if (radiusOverride > 0f) return radiusOverride;

        var filters = planet.GetComponentsInChildren<MeshFilter>(true);

        float bestRadius = 0f;
        float bestAlongSpot = 0f;
        Vector3 bestCenter = planet.position;
        string bestName = null;

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null) continue;

            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 s = filter.transform.lossyScale;
            float a = Mathf.Max(bounds.extents.x * Mathf.Abs(s.x), 0.000001f);
            float b = Mathf.Max(bounds.extents.y * Mathf.Abs(s.y), 0.000001f);
            float c = Mathf.Max(bounds.extents.z * Mathf.Abs(s.z), 0.000001f);
            float radius = Mathf.Max(a, Mathf.Max(b, c));

            if (radius > bestRadius)
            {
                Vector3 d = Quaternion.Inverse(filter.transform.rotation) * Vector3.up;
                float k = (d.x * d.x) / (a * a) + (d.y * d.y) / (b * b) + (d.z * d.z) / (c * c);

                bestRadius = radius;
                bestAlongSpot = 1f / Mathf.Sqrt(k);
                bestCenter = filter.transform.TransformPoint(bounds.center);
                bestName = filter.gameObject.name;
            }
        }

        if (bestName == null)
        {
            MelonLogger.Warning(
                $"[SolarSystem] No mesh found on '{planet.name}', set a surface radius for this scene.");
            return 0f;
        }

        float edge = Vector3.Dot(bestCenter - planet.position, Vector3.up) + bestAlongSpot;
        // MelonLogger.Msg(
        //     $"[SolarSystem] Surface measured from mesh '{bestName}' (radius {bestRadius:F3}, along spot {bestAlongSpot:F3}).");

        return edge / scale;
    }

    void FixedUpdate()
    {
        if (relativeToPlanet == null) return;

        if (_anchorCarrier != null)
        {
            transform.rotation = _anchorWorld * Quaternion.Inverse(GetAnchorSystemRotation());
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

        transform.position += SurfaceTarget() - relativeToPlanet.position;
    }

    public void ApplyScene(string mapName)
    {
        transform.position = Vector3.zero;

        switch (mapName)
        {
            case "Gym":
                SetRelativeTo(Main.solarSystem.TimberHearth.transform, GymSpotOffset, GymSpotHeading,
                    GymSurfaceRadius, GymPositionOffset);
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
                SetRelativeTo(Main.solarSystem.HourGlassTwins.transform.GetChild(0), RingSpotOffset,
                    RingSpotHeading, RingSurfaceRadius, RingPositionOffset);
                SetupCaveTwinRing();
                break;

            case "Map1":
                SetRelativeTo(Main.solarSystem.BrittleHollow.transform, PitSpotOffset, PitSpotHeading,
                    PitSurfaceRadius, PitPositionOffset);
                SetupBrittleHollowPit();
                break;

            case "Park":
                SetRelativeTo(Main.solarSystem.HearthianMapSatelite.transform, ParkSpotOffset, ParkSpotHeading,
                    ParkSurfaceRadius, ParkPositionOffset);

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
        
        DisableOcclusionCulling();
    }

    public void SetupCaveTwinRing()
    {
        Transform sceneObject = GameObject.Find("Scene").transform;
        sceneObject.GetChild(0).gameObject.SetActive(false);
        sceneObject.GetChild(1).gameObject.SetActive(false);
        sceneObject.GetChild(2).gameObject.SetActive(false);
        sceneObject.GetChild(4).gameObject.SetActive(false);
        sceneObject.GetChild(5).gameObject.SetActive(false);
        
        Transform ringCollision = sceneObject.GetChild(3);
        ringCollision.GetChild(1).gameObject.SetActive(false);
        
        Transform floor = ringCollision.GetChild(0);

        MeshCollider col = floor.GetComponent<MeshCollider>();
        MeshFilter filter = floor.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = col.sharedMesh;

        MeshRenderer renderer = floor.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Main.solarSystem.HourGlassTwins.transform.GetChild(0)
            .GetChild(0).GetChild(5).GetComponent<MeshRenderer>().sharedMaterial;
    }

    public void SetupBrittleHollowPit()
    {
        Transform sceneObject = GameObject.Find("Scene").transform;
        sceneObject.GetChild(2).gameObject.SetActive(false);
        
        Transform map1Visuals = sceneObject.GetChild(0);
        Material brittleHollowMaterial = Main.solarSystem.BrittleHollow.transform.GetChild(0).
            GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
        for (int i = 0; i < map1Visuals.childCount; i++)
        {
            map1Visuals.GetChild(i).gameObject.GetComponent<MeshRenderer>().SetMaterial(brittleHollowMaterial);
        }
    }

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

    public void ApplyFrame(Quaternion rootRot, bool hasCustom, Quaternion customRot)
    {
        _tiltRotation = rootRot;
        _orbitalOffset = Quaternion.identity;
        transform.rotation = rootRot;

        if (_anchorOrbiter != null && hasCustom)
            _anchorOrbiter.customRotation = customRot;
    }

    public void RestoreBaseFrame()
    {
        _orbitalOffset = Quaternion.identity;
        if (relativeToPlanet != null)
            transform.rotation = _tiltRotation;
    }

    public void ReanchorAfterReset()
    {
        if (relativeToPlanet != null)
            SetRelativeTo(relativeToPlanet, _spotLocalOffset, _spotHeading, _spotSurfaceRadius,
                _spotPositionOffset);
    }

    public void DisableOcclusionCulling()
    {
        Calls.Players.GetLocalPlayer().Controller.gameObject.transform.GetChild(2).GetChild(0).GetChild(0)
            .gameObject
            .GetComponent<Camera>().useOcclusionCulling = false;
    }
}