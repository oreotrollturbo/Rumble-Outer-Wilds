using System;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using RumbleModdingAPI.RMAPI;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using Il2CppInterop.Runtime.InteropTypes;
using OuterWildsRumble.UIFrameworkSettings;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class StarBackground : MonoBehaviour
{
    // Sit just inside the far clip plane so floating-point rounding never
    // tips a vertex over the edge and gets it culled/clipped for a frame.
    const float FarPlaneSafetyMargin = 0.97f;

    public StarBackground(IntPtr ptr) : base(ptr) { }

    Transform headset;
    float meshBoundsRadius; // radius of the mesh's bounds at localScale = 1

    void Start()
    {
        // Measure the mesh instead of assuming a magic ratio for it.
        meshBoundsRadius = GetComponent<MeshFilter>().mesh.bounds.extents.magnitude;

        RescaleToFarClip();

        transform.position = Vector3.zero;
        DontDestroyOnLoad(gameObject);
        Actions.onMapInitialized += SceneLoaded;

        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        GetComponent<Renderer>().material.renderQueue = 2900;
    }

    public void RescaleToFarClip()
    {
        float farClip = OwSystemSettings.ViewDistance.Value; // whatever the game actually uses
        float targetRadius = farClip * FarPlaneSafetyMargin;
        transform.localScale = Vector3.one * (targetRadius / meshBoundsRadius);
    }

    private void SceneLoaded(string mapName)
    {
        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        RescaleToFarClip(); // in case the far clip plane can differ per-scene
    }
}