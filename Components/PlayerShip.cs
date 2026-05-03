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
public class PlayerShip : MonoBehaviour
{
    public PlayerShip(IntPtr ptr) : base(ptr)
    {
    }

    void Start()
    { 
        DontDestroyOnLoad(gameObject);
        transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        transform.position = new Vector3(-35.4736f, 10.75f, -15.9372f);
        transform.rotation = Quaternion.Euler(-0, 163f, 0);
        Actions.onMapInitialized += SceneLoaded;
    }

    private void SceneLoaded(string mapName)
    {
        switch (mapName)
        {
            case "Gym":
                transform.position = new Vector3(-35.4736f, 10.75f, -15.9372f);
                transform.rotation = Quaternion.Euler(-0, 163f, 0);
                break;

            case "Map0":

                Vector3 pos = new Vector3(15.7553f, 5.7286f, 29.0445f);
                Quaternion rot = Quaternion.Euler(349.3421f, 295.3221f, 358.0536f);
                if (!Calls.Players.IsHost())
                {
                    pos = new Vector3(22.8553f, 23.9273f, -28.9091f);
                    rot = Quaternion.Euler(6f, 53.4914f, 359.1993f);
                }

                transform.position = pos;
                transform.rotation = rot;

                break;

            case "Map1":
                transform.position = new Vector3(-34.4782f, 19.8146f, -14.1971f);
                transform.rotation = Quaternion.Euler(359.8188f, 143.5267f, 6.4373f);
                break;

            case "Park":
                transform.position = new Vector3(-17.3906f, 5.0094f, -24.2655f);
                transform.rotation = Quaternion.Euler(0f, 88.8748f, 356.2899f);
                break;
        }
    }
}