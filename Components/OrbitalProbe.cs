using System;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class OrbitalProbe : MonoBehaviour
{
    public OrbitalProbe(IntPtr ptr) : base(ptr) {}

    private float probeSpeed = 1f;

    // Offset from the tip transform in its local space (tweak to sit inside barrel)
    private Vector3 middleLocalOffset = new Vector3(-0.2f, 0f, 0f);

    private Transform midTransform;
    private bool isLaunched = false;
    private Vector3 launchDirection;

    void Start()
    {
        // Grab the tip from the cannon's hierarchy:
        // SolarSystem root -> OrbitalProbeCannon -> probeCannonRoot (child 0) -> tip (child 2)
        Transform cannonRoot = Main.solarSystem.OrbitalProbeCannon.transform;
        Transform probeCannonRoot = cannonRoot.GetChild(0);
        midTransform = probeCannonRoot.GetChild(1);
    }

    void FixedUpdate()
    {
        if (!isLaunched)
        {
            // Dock: follow tip transform with offset
            transform.position = midTransform.TransformPoint(middleLocalOffset);
            transform.rotation = midTransform.rotation;
        }
        else
        {
            // Fly forever in the launched direction
            transform.position += launchDirection * probeSpeed * Time.fixedDeltaTime;
        }
    }

    public void StartLaunch()
    {
        // Snapshot the forward direction of the tip at launch time
        launchDirection = midTransform.forward;
        isLaunched = true;
    }

    public void Reinitialise()
    {
        isLaunched = false;
        // FixedUpdate will snap it back to the tip on the next frame
    }
}