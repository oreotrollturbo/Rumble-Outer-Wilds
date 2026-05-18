using System;
using MelonLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class Orbiter : MonoBehaviour
{
    public Transform orbitParent;
    public float orbitDistance = 5f;

    public Vector3 orbitAngles = Vector3.zero;

    public Vector3 orbitAxis = Vector3.up;
    public float orbitSpeed = 30f;

    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 30f;

    public bool randomisePos = true;
    public bool spinEnabled = true;
    
    public bool disableOrbit = false;
    
    

    public float _currentOrbitAngle = 0;
    public float _currentSpinAngle = 0;

    public Orbiter(IntPtr ptr) : base(ptr) { }

    void Start()
    {
        if (randomisePos)
        {
            _currentOrbitAngle = GetRandomAngle();
            _currentSpinAngle = GetRandomAngle();
        }
    }

    public Quaternion? customRotation = null; // when set, overrides spin rotation

    void FixedUpdate()
    {
        if (disableOrbit) return;
        if (orbitParent)
        {
            float dt = Time.fixedDeltaTime;

            _currentOrbitAngle += orbitSpeed * dt;
            if (spinEnabled)
                _currentSpinAngle += spinSpeed * dt;

            _currentOrbitAngle %= 360f;
            _currentSpinAngle %= 360f;

            Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
            Quaternion orbitRot = Quaternion.AngleAxis(_currentOrbitAngle, orbitAxis);
            Vector3 localOffsetDirection = orbitRot * baseDir;

            Vector3 worldOffset = orbitParent.rotation * localOffsetDirection * orbitDistance;
            transform.position = orbitParent.position + worldOffset;

            if (spinEnabled)
            {
                if (customRotation.HasValue)
                {
                    transform.rotation = customRotation.Value;
                }
                else
                {
                    Quaternion tiltRot = Quaternion.Euler(orbitAngles);
                    Quaternion spinRot = Quaternion.AngleAxis(_currentSpinAngle, spinAxis);
                    transform.rotation = orbitParent.rotation * tiltRot * spinRot;
                }
            }
        }
    }

    // Returns the predicted world position at a given orbit angle
    public Vector3 GetPositionAtAngle(float orbitAngle)
    {
        Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
        Quaternion orbitRot = Quaternion.AngleAxis(orbitAngle, orbitAxis);
        Vector3 localOffsetDirection = orbitRot * baseDir;
        Vector3 worldOffset = orbitParent.rotation * localOffsetDirection * orbitDistance;
        return orbitParent.position + worldOffset;
    }

    // Returns what _currentOrbitAngle will be after a given number of seconds
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