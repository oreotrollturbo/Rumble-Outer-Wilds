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
    public const float ScaleToCamDistance = 6.25f; ////6.25 scale per 1 far clip plane unit 

    public StarBackground(IntPtr ptr) : base(ptr)
    {
    }

    Transform headset;

    void Start()
    {
        transform.localScale = Vector3.one * (OwSystemSettings.ViewDistance.Value * ScaleToCamDistance);
        transform.position = Vector3.zero;
        DontDestroyOnLoad(gameObject);
        Actions.onMapInitialized += SceneLoaded;
        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
    }

    void Update()
    {
        // try
        // {
        //     transform.position = headset.position;
        // }
        // catch (NullReferenceException e)
        // {
        // }
    }


    private void SceneLoaded(string mapName)
    {
        headset = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
    }
}