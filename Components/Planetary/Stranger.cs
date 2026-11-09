using MelonLoader;
using OuterWildsRumble.UIFrameworkSettings;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class Stranger : MonoBehaviour
{
    public Transform sun;                 // the Sun
    public float satelliteAngle = -30f;   // the point on the satellite's orbit the Stranger sits in line with
    public float distance = 10.6f;        // pre-scale distance from the Sun, ~Giant's Deep
    public float modelScale = 0.0004f;    // size of the Stranger (times system scale / 30)

    public float centerSpinSpeed = 5f;        // deg/s for the Center object
    public float leaveStartSeconds = 400f;    // 6:40 after the loop starts
    public float leaveDurationSeconds = 920f; // 22:00 - 6:40, how long the trip outwards takes
    public float leaveDistanceMargin = 1.15f; // stops at satellite distance * this
    public bool cloakingFieldEnabled = true;

    public GameObject Center;
    public GameObject CloakingField;

    private const float BaseScale = 0.00015f;
    private float _elapsed;
    private Vector3 _lastDir = Vector3.forward;

    public Stranger(IntPtr ptr) : base(ptr) { }

    void Start()
    {
        EnsureRefs();
        ApplyCloak();
    }

    // Children may not be resolved yet when settings are applied (Start hasn't run), so resolve lazily
    private void EnsureRefs()
    {
        if (Center == null && transform.childCount > 0)
            Center = transform.GetChild(0).gameObject;
        if (CloakingField == null && transform.childCount > 2)
            CloakingField = transform.GetChild(2).gameObject;
    }

    private void ApplyCloak()
    {
        if (CloakingField != null)
            CloakingField.SetActive(cloakingFieldEnabled);
    }

    /// <summary>
    /// Pulls everything from settings. Scale is applied here (once per settings change)
    /// instead of every frame. Call this AFTER SolarSystemScaler.Apply.
    /// </summary>
    public void ApplySettings()
    {
        gameObject.SetActive(OwSystemSettings.StrangerEnabled.Value);

        modelScale = OwSystemSettings.StrangerScale.Value;
        distance = OwSystemSettings.StrangerDistance.Value;
        satelliteAngle = OwSystemSettings.StrangerSatelliteAngle.Value;
        centerSpinSpeed = OwSystemSettings.StrangerCenterSpinSpeed.Value;
        leaveStartSeconds = OwSystemSettings.StrangerLeaveStartSeconds.Value;
        leaveDurationSeconds = OwSystemSettings.StrangerLeaveDurationSeconds.Value;
        cloakingFieldEnabled = OwSystemSettings.StrangerCloakingField.Value;

        float systemScale = OwSystemSettings.SolarSystemScale.Value;
        transform.localScale = Vector3.one * (BaseScale * modelScale * systemScale / 30f);

        EnsureRefs();
        ApplyCloak();
    }

    /// <summary>Restarts the 6:40 timer, which snaps the Stranger back to its start position. Called when the loop restarts.</summary>
    public void ResetLoop()
    {
        _elapsed = 0f;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;

        EnsureRefs();
        if (Center != null && centerSpinSpeed != 0f)
            Center.transform.Rotate(Vector3.up, centerSpinSpeed * Time.deltaTime, Space.Self);
    }

    // Position/rotation only. Scale is not touched here.
    void LateUpdate()
    {
        if (sun == null) return;
        
        var satOrb = Main.solarSystem.HearthianMapSatelite?.GetComponent<Orbiter>();
        if (satOrb != null && Time.frameCount % 120 == 0)
        {
            float sep = Vector3.Angle(satOrb.transform.position - sun.position, transform.position - sun.position);
            MelonLogger.Msg($"[StrangerDebug] satAngle={satOrb._currentOrbitAngle:F1} target={satelliteAngle} separation={sep:F1} elapsed={_elapsed:F0}");
        }

        Transform root = Main.solarSystem.Root.transform;
        float systemScale = OwSystemSettings.SolarSystemScale.Value;

        // Direction from the Sun towards the satellite's orbit point at satelliteAngle
        if (TryGetSatelliteDirection(out Vector3 dir))
            _lastDir = dir;
        else
            dir = _lastDir;

        // Distance from the Sun, drifting outwards along the same line after leaveStartSeconds
        float startDist = distance * systemScale;
        float dist = startDist;

        if (_elapsed > leaveStartSeconds)
        {
            float t = Mathf.Clamp01((_elapsed - leaveStartSeconds) / Mathf.Max(leaveDurationSeconds, 0.01f));
            float targetDist = GetLeaveTargetDistance(startDist);
            dist = Mathf.Lerp(startDist, targetDist, Mathf.SmoothStep(0f, 1f, t));
        }

        transform.position = sun.position + dir * dist;
        transform.LookAt(sun.position, root.up);
    }

    /// <summary>
    /// Direction from the Sun to where the satellite is when its orbit angle equals satelliteAngle.
    /// Uses the Orbiter's own position math, so it stays fixed while the satellite moves.
    /// </summary>
    private bool TryGetSatelliteDirection(out Vector3 dir)
    {
        dir = Vector3.forward;

        GameObject sat = Main.solarSystem.HearthianMapSatelite;
        if (sat == null) return false;

        Orbiter orb = sat.GetComponent<Orbiter>();
        if (orb == null || orb.orbitParent == null) return false;

        Vector3 offset = orb.GetPositionAtAngle(satelliteAngle) - sun.position;
        if (offset.sqrMagnitude < 1e-6f) return false;

        dir = offset.normalized;
        return true;
    }

    private float GetLeaveTargetDistance(float startDist)
    {
        GameObject sat = Main.solarSystem.HearthianMapSatelite;
        if (sat == null) return startDist * 3f; // fallback if the satellite is missing

        float satDist = Vector3.Distance(sat.transform.position, sun.position);
        return Mathf.Max(startDist, satDist * leaveDistanceMargin);
    }
}