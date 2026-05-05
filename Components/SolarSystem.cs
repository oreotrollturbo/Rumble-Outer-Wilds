using System;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SolarSystem : MonoBehaviour
{
    public SolarSystem(IntPtr ptr) : base(ptr) {}
    public void Start()
    {
        Actions.onMapInitialized += SceneLoaded;
        
        transform.position = new Vector3(OwSystemSettings.SolarSystemGymX.Value,
            OwSystemSettings.SolarSystemGymY.Value,
            OwSystemSettings.SolarSystemGymZ.Value);
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void SceneLoaded(string mapName)
    {
        switch (mapName)
        {
            case "Gym":
                transform.position = new Vector3(OwSystemSettings.SolarSystemGymX.Value,
                    OwSystemSettings.SolarSystemGymY.Value,
                    OwSystemSettings.SolarSystemGymZ.Value);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                    
                break;
                
            case "Map0":
                transform.position = new Vector3(
                    OwSystemSettings.SolarSystemRingX.Value,
                    OwSystemSettings.SolarSystemRingY.Value,
                    OwSystemSettings.SolarSystemRingZ.Value);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                
                break;
                
            case "Map1":
                transform.position = new Vector3(
                    OwSystemSettings.SolarSystemPitX.Value,
                    OwSystemSettings.SolarSystemPitY.Value,
                    OwSystemSettings.SolarSystemPitZ.Value);
                transform.rotation = Quaternion.Euler(0, 0, 0);
                    
                break;
                
            case "Park":
                transform.position = new Vector3(
                    OwSystemSettings.SolarSystemParkX.Value,
                    OwSystemSettings.SolarSystemParkY.Value,
                    OwSystemSettings.SolarSystemParkZ.Value);
                transform.rotation = Quaternion.Euler(0, 124.1311f, 0);
                    
                break;
        }
    }

    public void StartSolarSystem()
    {
        if (Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().hasFiredBefore)
        {
            Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().Restart(); //TODO rework logic silly !
        }
        Main.solarSystem.BrittleHollow.GetComponent<BrittleHollow>().SolarSystemRestart();
        Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().StartFiringSequence();
    }

    public void Scale(float scale)
    {
        GameObject Root = transform.gameObject;
        // Scale the root
        Root.transform.localScale *= scale;
            
        // Scale orbiters
        foreach (var orb in Root.GetComponentsInChildren<Orbiter>())
        {
            orb.orbitDistance *= scale;
        }
            
        // Scale elliptical orbiters
        foreach (var ell in Root.GetComponentsInChildren<EllipticalOrbiter>())
        {
            ell.semiMinorAxis *= scale;
            ell.meltStartDistance *= scale;
            ell.meltCompleteDistance *= scale;
        }
        
        
        var quantumOrbiter = Main.solarSystem.QuantumMoon.GetComponent<QuantumOrbiter>();
        var keys = new List<Transform>(quantumOrbiter.orbitParents.Keys);
        foreach (var key in keys)
        {
            quantumOrbiter.orbitParents[key] *= scale;
        }
        
        Main.solarSystem.Sun.GetComponent<SupernovaSun>().interloperSwallowDistance  *= scale;
    }
}