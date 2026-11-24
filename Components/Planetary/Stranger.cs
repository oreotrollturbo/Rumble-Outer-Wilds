using MelonLoader;
using OuterWildsRumble.UIFrameworkSettings;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class Stranger : MonoBehaviour
{
    public Transform sun;                 // the Sun
    public float satelliteAngle = -30f;   // the point on the satellite's orbit the Stranger sits in line with
    public float sideOffsetAngle = 4f;    // degrees sideways from the satellite line (negative = other side)
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

    
    public void ApplySettings()
    {
        gameObject.SetActive(OwSystemSettings.StrangerEnabled.Value);

        modelScale = OwSystemSettings.StrangerScale.Value;
        distance = OwSystemSettings.StrangerDistance.Value;
        satelliteAngle = OwSystemSettings.StrangerSatelliteAngle.Value;
        sideOffsetAngle = OwSystemSettings.StrangerSideOffsetAngle.Value;
        centerSpinSpeed = OwSystemSettings.StrangerCenterSpinSpeed.Value;
        leaveStartSeconds = OwSystemSettings.StrangerLeaveStartSeconds.Value;
        leaveDurationSeconds = OwSystemSettings.StrangerLeaveDurationSeconds.Value;
        cloakingFieldEnabled = OwSystemSettings.StrangerCloakingField.Value;

        transform.localScale = Vector3.one * (BaseScale * modelScale);

        EnsureRefs();
        ApplyCloak();
    }

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

    void LateUpdate()
    {
        
        Transform root = Main.solarSystem.Root.transform;
        float systemScale = OwSystemSettings.SolarSystemScale.Value;

        if (TryGetSatelliteDirection(out Vector3 dir))
            _lastDir = dir;
        else
            dir = _lastDir;

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
    /// Get the satellite direction and calculate X degrees to put at
    /// </summary>
    private bool TryGetSatelliteDirection(out Vector3 dir)
    {
        dir = Vector3.forward;

        GameObject sat = Main.solarSystem.HearthianMapSatelite;

        Orbiter orb = sat.GetComponent<Orbiter>();

        Vector3 offset = orb.GetPositionAtAngle(satelliteAngle) - sun.position;
        if (offset.sqrMagnitude < 1e-6f) return false;

        Transform root = Main.solarSystem.Root.transform;
        dir = Quaternion.AngleAxis(sideOffsetAngle, root.up) * offset.normalized;
        return true;
    }

    private float GetLeaveTargetDistance(float startDist)
    {
        GameObject sat = Main.solarSystem.HearthianMapSatelite;

        float satDist = Vector3.Distance(sat.transform.position, sun.position);
        return Mathf.Max(startDist, satDist * leaveDistanceMargin);
    }
}