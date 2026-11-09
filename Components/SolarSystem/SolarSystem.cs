using MelonLoader;
using OuterWildsRumble.Components.Utils;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.Rendering;
using Object = Il2CppSystem.Object;

namespace OuterWildsRumble.Components;

/// <summary>
/// The core solar system component: places the system in each map and (re)starts it.
/// Networking lives in <see cref="SolarSystemNetworking"/>, realistic mode in <see cref="SolarSystemRealisticMode"/>.
/// All three sit on the same root GameObject.
/// </summary>
[RegisterTypeInIl2Cpp]
public class SolarSystem : MonoBehaviour
{
    private SolarSystemRealisticMode _realistic;
    private SolarSystemNetworking _networking;
    
    public static Shader replacementShader;

    public SolarSystemRealisticMode Realistic
    {
        get
        {
            if (_realistic == null) _realistic = GetComponent<SolarSystemRealisticMode>();
            return _realistic;
        }
    }

    public void Start()
    {
        Actions.onMapInitialized += SceneLoaded;

        transform.position = new Vector3(
            OwSystemSettings.SolarSystemGymX.Value,
            OwSystemSettings.SolarSystemGymY.Value,
            OwSystemSettings.SolarSystemGymZ.Value);
        if (OwSystemSettings.RealisticMode.Value)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }
    }

    public void SceneLoaded(string mapName)
    {
        if (OwSystemSettings.ShaderReplacement.Value)
        {
            ReplaceAllShaders();
        }
        
        if (OwSystemSettings.OnlySunLighting.Value)
        {
            HandleOnlySunLighting(mapName);
        }
        
        if (!OwSystemSettings.RealisticMode.Value)
        {
            switch (mapName)
            {
                case "Gym":
                    transform.position = new Vector3(
                        OwSystemSettings.SolarSystemGymX.Value,
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
        else if (Realistic != null)
        {
            Realistic.ApplyScene(mapName);
        }
        else
        {
            MelonLogger.Warning("[SolarSystem] Realistic mode is on but SolarSystemRealisticMode is missing.");
        }
    }

    public void StartSolarSystem()
    {
        if (Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().hasFiredBefore)
            Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().Restart();

        Main.solarSystem.BrittleHollow.GetComponent<BrittleHollow>().SolarSystemRestart();
        Main.solarSystem.HollowsLantern.GetComponent<HollowsLantern>().SolarSystemRestart();
        Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().StartFiringSequence();

        // Restart the Stranger's departure timer and the satellite's climb
        if (Main.solarSystem.Stranger != null)
        {
            var st = Main.solarSystem.Stranger.GetComponent<Stranger>();
            if (st != null) st.ResetLoop();
        }

        var satOrbit = Main.solarSystem.HearthianMapSatelite?.GetComponent<Orbiter>();
        if (satOrbit != null) satOrbit._currentOrbitAngle = 0f;

        if (OwSystemSettings.RealisticMode.Value && Realistic != null)
        {
            Realistic.SetRelativeTo(Main.solarSystem.TimberHearth);
        }
    }
    
    public void ReplaceAllShaders()
    {
        MelonLogger.Msg("Starting shader replacement");

        if (replacementShader == null)
        {
            MelonLogger.Error("Replacement Shader variable is not assigned!");
            return;
        }

        foreach (Renderer item in FindObjectsOfType<Renderer>())
        {
            Material sharedMaterial = item.sharedMaterial;
            if (sharedMaterial == null)
            {
                continue;
            }
            if ((sharedMaterial.shader).name == "Shader Graphs/MobileEnvironmentUV0")
            {
                Shader val = replacementShader;
                if (item.sharedMaterial.shader != (Object)val)
                {
                    Material val2 = new Material(val);
                    Texture texture = sharedMaterial.GetTexture("_TEXTURE");
                    val2.SetTexture("Texture2D_2058E65A", texture);
                    val2.SetTexture("Texture2D_3812B1EC", texture);
                    val2.SetColor("Color_D943764B", Color.white);
                    item.sharedMaterial = val2;
                }
            }
            item.lightmapIndex = -1;
            item.lightProbeUsage = 0;
            item.reflectionProbeUsage = 0;
        }
    }
    
    void HandleOnlySunLighting(string mapName)
    {
        if (mapName.Contains("Map"))
        {
            GameObject.Find("Lighting & Effects").transform.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            GameObject.Find("LIGHTING").SetActive(false);
        }
    }

    void Scale(float scale)
    {
        SolarSystemScaler.Apply(scale);
    }
}