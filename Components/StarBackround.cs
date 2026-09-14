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
    const float FarPlaneSafetyMargin = 0.97f;

    public StarBackground(IntPtr ptr) : base(ptr) { }

    Transform headset;
    float meshBoundsRadius; // radius of the mesh hopefully

    void Start()
    {
        // Measure the mesh instead of assuming a magic ratio for it, (seems to work)
        meshBoundsRadius = GetComponent<MeshFilter>().mesh.bounds.extents.magnitude;

        RescaleToFarClip();

        DontDestroyOnLoad(gameObject);
        Actions.onMapInitialized += SceneLoaded;

        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        GetComponent<Renderer>().material.renderQueue = 2900;
    }

    public void RescaleToFarClip()
    {
        if (Main.solarSystem.Root != null && transform.parent != Main.solarSystem.Root.transform)
        {
            transform.SetParent(Main.solarSystem.Root.transform, true);
        }

        float farClip = OwSystemSettings.ViewDistance.Value;
        float targetRadius = farClip * FarPlaneSafetyMargin;
        float desiredWorldScale = targetRadius / meshBoundsRadius;

       
        float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;

        transform.localScale = Vector3.one * (desiredWorldScale / parentScale);
        transform.position = Vector3.zero;
    }

    private void SceneLoaded(string mapName)
    {
        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        RescaleToFarClip();
    }
}