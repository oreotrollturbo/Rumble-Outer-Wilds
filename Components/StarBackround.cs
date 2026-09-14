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

        DontDestroyOnLoad(gameObject);
        Actions.onMapInitialized += SceneLoaded;

        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        GetComponent<Renderer>().material.renderQueue = 2900;
    }

    public void RescaleToFarClip()
    {
        // Make sure we're parented to the solar system root before doing any
        // scale/position math relative to it.
        if (Main.solarSystem.Root != null && transform.parent != Main.solarSystem.Root.transform)
        {
            transform.SetParent(Main.solarSystem.Root.transform, true);
        }

        float farClip = OwSystemSettings.ViewDistance.Value; // whatever the game actually uses
        float targetRadius = farClip * FarPlaneSafetyMargin;
        float desiredWorldScale = targetRadius / meshBoundsRadius;

        // Local values are only correct in world space if we correct for the
        // parent's scale - important since this sits under Root, which is
        // scaled down to build the solar system at model scale.
        float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;

        transform.localScale = Vector3.one * (desiredWorldScale / parentScale);
        transform.localPosition = Vector3.zero; // centered on parent, not world origin
    }

    private void SceneLoaded(string mapName)
    {
        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        RescaleToFarClip(); // in case the far clip plane can differ per-scene
    }
}