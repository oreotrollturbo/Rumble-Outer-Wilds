using System;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class BrittleHollow: MonoBehaviour
{
    public BrittleHollow(IntPtr ptr) : base(ptr) {}

    List<Transform> destructibleParts = new List<Transform>();

    void Start()
    {
        var bhRoot = transform.GetChild(0);

        for (int i = 0; i < bhRoot.childCount; i++)
        {
            Transform child = bhRoot.GetChild(i);

            if (!child.gameObject.name.Contains("Unbreakable"))
            {
                destructibleParts.Add(child);
            }
        }
        
        //TODO break off random pieces over time by decreasing their Z until it reaches the black hole, then unparent them so they can be parented to the white hole where they will stay?
    }


    public void SolarSystemRestart()
    {
        //TODO get all the pieces back in their original positon, maybe copy and remake brittle hollow or keep track of all original piece positions?
    }
    
}