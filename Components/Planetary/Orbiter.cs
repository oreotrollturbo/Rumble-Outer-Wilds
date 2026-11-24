using System;
using MelonLoader;
using OuterWildsRumble.UIFrameworkSettings;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class Orbiter : MonoBehaviour
{
    public Transform orbitParent;
    public float orbitDistance = 5f;

    private Vector3 orbitAngles = Vector3.zero;

    public Vector3 orbitAxis = Vector3.up;
    public float orbitSpeed = 30f;

    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 30f;

    public bool randomisePos = !OwSystemSettings.RealisticMode.Value;
    public bool spinEnabled = true;

    public bool disableOrbit = false;

    public float _currentOrbitAngle = 0;
    public float _currentSpinAngle = 0;

    private float _initialOrbitAngle;
    private float _initialSpinAngle;
    private Quaternion _initialLocalRotation = Quaternion.identity;
    private bool _hasInitialState;

    public bool HasInitialState => _hasInitialState;

    public Quaternion? customRotation = null;

    public Orbiter(IntPtr ptr) : base(ptr) { }

    void Start()
    {
        if (randomisePos)
        {
            _currentOrbitAngle = GetRandomAngle();
            _currentSpinAngle = GetRandomAngle();
        }

        _initialOrbitAngle = _currentOrbitAngle;
        _initialSpinAngle = _currentSpinAngle;
        _initialLocalRotation = transform.localRotation;
        _hasInitialState = true;
    }

    public void ResetLoop()
    {
        if (!_hasInitialState) return;

        _currentOrbitAngle = _initialOrbitAngle;
        _currentSpinAngle = _initialSpinAngle;
        customRotation = null;
        transform.localRotation = _initialLocalRotation;

        if (orbitParent)
            transform.position = GetPositionAtAngle(_currentOrbitAngle);
    }

    public Quaternion GetSystemSpinRotation()
    {
        if (!_hasInitialState) return transform.localRotation;

        return _initialLocalRotation *
               Quaternion.AngleAxis(_currentSpinAngle - _initialSpinAngle, spinAxis.normalized);
    }

    private static Quaternion GetSystemFrame()
    {
        var root = Main.solarSystem.Root;
        return root != null ? root.transform.rotation : Quaternion.identity;
    }

    void FixedUpdate()
    {
        if (disableOrbit) return;
        if (orbitParent)
        {
            float dt = Time.fixedDeltaTime;

            _currentOrbitAngle += orbitSpeed * dt;
            _currentOrbitAngle %= 360f;

            Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
            Quaternion orbitRot = Quaternion.AngleAxis(_currentOrbitAngle, orbitAxis);
            Vector3 localOffsetDirection = orbitRot * baseDir;

            Vector3 worldOffset = GetSystemFrame() * localOffsetDirection * orbitDistance;
            transform.position = orbitParent.position + worldOffset;

            if (spinEnabled)
            {
                float deltaAngle = spinSpeed * dt;
                _currentSpinAngle = (_currentSpinAngle + deltaAngle) % 360f;

                if (customRotation.HasValue)
                {
                    transform.rotation = customRotation.Value;
                }
                else
                {
                    Quaternion frameSpin = Quaternion.AngleAxis(deltaAngle, spinAxis);
                    transform.rotation = transform.rotation * frameSpin;
                }
            }
        }
    }

    public Vector3 GetPositionAtAngle(float orbitAngle)
    {
        Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
        Quaternion orbitRot = Quaternion.AngleAxis(orbitAngle, orbitAxis);
        Vector3 localOffsetDirection = orbitRot * baseDir;
        Vector3 worldOffset = GetSystemFrame() * localOffsetDirection * orbitDistance;
        return orbitParent.position + worldOffset;
    }

    public float GetOrbitAngleAfter(float seconds)
    {
        return (_currentOrbitAngle + orbitSpeed * seconds) % 360f;
    }

    public void SetCurrentAngle(float angle)
    {
        _currentOrbitAngle = angle;
    }

    public static float GetRandomAngle()
    {
        return Random.Range(0f, 360f);
    }
}