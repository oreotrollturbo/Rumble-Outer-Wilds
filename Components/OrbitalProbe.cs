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

    public float probeSpeed = 30f;

    private Quaternion rotOffset = Quaternion.Euler(0, 0, 90f);

    // Offset from the tip transform in its local space (tweak to sit inside barrel)
    private Vector3 middleLocalOffset = new Vector3(-1.67f, 0, 0);

    private Transform probeHalo;

    private Transform midTransform;
    private bool isLaunched = false;
    private Vector3 launchDirection;
    
    private Transform cannonTransform;

    void Start()
    {
        probeHalo = transform.GetChild(0);
        probeHalo.gameObject.SetActive(false);
        // Grab the middle from the cannon's hierarchy:
        // SolarSystem root -> OrbitalProbeCannon -> probeCannonRoot (child 0) -> tip (child 2)
        Transform cannonRoot = Main.solarSystem.OrbitalProbeCannon.transform;
        Transform probeCannonRoot = cannonRoot.GetChild(0);
        midTransform = probeCannonRoot.GetChild(1);
        transform.rotation = midTransform.rotation * rotOffset;
        
        cannonTransform = cannonRoot;
    }

    void FixedUpdate()
    {
        if (!isLaunched)
        {
            // Dock: follow tip transform with offset
            transform.position = midTransform.TransformPoint(middleLocalOffset);
            transform.rotation = midTransform.rotation * rotOffset;
        }
        else
        {
            // Fly forever in the launched direction
            transform.position += launchDirection * probeSpeed * Time.fixedDeltaTime;
        }
    }

    public void StartLaunch()
    {
        probeHalo.gameObject.SetActive(true);
        launchDirection = cannonTransform.right; //NOT .forward
        isLaunched = true;
    }

    public void Reinitialise()
    {
        isLaunched = false;
        probeHalo.gameObject.SetActive(true);
    }
}