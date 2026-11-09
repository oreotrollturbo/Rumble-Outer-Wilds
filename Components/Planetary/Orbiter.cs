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
    public float orbitSpeed = 30f; //degs per sec

    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 30f; //degs per sec

    public bool randomisePos = !OwSystemSettings.RealisticMode.Value;
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

    public Quaternion? customRotation = null; // when set, overrides spin rotation (the body is pinned in world space)

    // Orbit offsets are expressed in the solar system's frame, NOT the parent's rotation.
    // Using orbitParent.rotation made moons inherit their parent's spin (orbit speed = moon + parent spin).
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

            //Positioning
            Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
            Quaternion orbitRot = Quaternion.AngleAxis(_currentOrbitAngle, orbitAxis);
            Vector3 localOffsetDirection = orbitRot * baseDir;

            Vector3 worldOffset = GetSystemFrame() * localOffsetDirection * orbitDistance;
            transform.position = orbitParent.position + worldOffset;

            // Rotation
            if (spinEnabled)
            {
                // The spin angle must keep advancing even while the body is pinned (anchored),
                // because SolarSystem uses it to counter-rotate the sky.
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

    // what _currentOrbitAngle will be after a given number of seconds
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