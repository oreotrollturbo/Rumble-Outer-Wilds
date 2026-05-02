using System;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using RumbleModdingAPI.RMAPI;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class QuantumTapeRecorder : MonoBehaviour
{
    public QuantumTapeRecorder(IntPtr ptr) : base(ptr)
    {
    }

    void Start()
    {
        transform.position = new Vector3(-35.4736f, 10.75f, -15.9372f);
        transform.rotation = Quaternion.Euler(-0, 163f, 0);
        Actions.onMapInitialized += SceneLoaded;
    }

    private void SceneLoaded(string mapName)
    {
        gameObject.SetActive(mapName == "Gym");
    }
}