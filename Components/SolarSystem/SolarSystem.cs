using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using OuterWildsRumble.Components.Utils;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.Rendering;

namespace OuterWildsRumble.Components;


[RegisterTypeInIl2Cpp]
public class SolarSystem : MonoBehaviour
{
    private RealisticMode _realistic;
    private SolarSystemNetworking _networking;

    public static Shader replacementShader;

    private readonly Dictionary<int, Material> _replacementCache = new();
    private readonly Dictionary<int, string> _shaderNames = new();
    private const string ReplacedPrefix = "OWR_";
    private static readonly string[] TexProps = { "_TEXTURE", "Texture2D_2058E65A", "_MainTex", "_BaseMap" };
    private float _loadTime;

    public RealisticMode Realistic
    {
        get
        {
            if (_realistic == null) _realistic = GetComponent<RealisticMode>();
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
        _loadTime = Time.realtimeSinceStartup;

        if (OwSystemSettings.ShaderReplacement.Value)
        {
            MelonCoroutines.Start(ReplaceShadersWatch());
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
            MelonLogger.Warning("[SolarSystem] Realistic mode is on but RealisticMode is missing.");
        }
    }


    public void ResetLoop()
    {
        if (OwSystemSettings.RealisticMode.Value && Realistic != null)
            Realistic.RestoreBaseFrame();

        var orbiters = GetComponentsInChildren<Orbiter>(true)
            .Where(o => o != null)
            .OrderBy(OrbiterDepth)
            .ToList();
        foreach (var orbiter in orbiters)
            orbiter.ResetLoop();

        foreach (var twins in GetComponentsInChildren<HourGlassTwins>(true))
            twins.ResetLoop();

        if (OwSystemSettings.RealisticMode.Value && Realistic != null)
            Realistic.ReanchorAfterReset();
    }

    private static int OrbiterDepth(Orbiter orbiter)
    {
        if (orbiter == null) return 0;
        int depth = 0;
        var parent = orbiter.orbitParent;
        while (parent != null && depth < 16)
        {
            var parentOrbiter = parent.GetComponent<Orbiter>();
            if (parentOrbiter == null) break;
            depth++;
            parent = parentOrbiter.orbitParent;
        }
        return depth;
    }

    public void StartSolarSystem()
    {
        ResetLoop();

        if (Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().hasFiredBefore)
            Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().Restart();

        Main.solarSystem.BrittleHollow.GetComponent<BrittleHollow>().SolarSystemRestart();
        Main.solarSystem.HollowsLantern.GetComponent<HollowsLantern>().SolarSystemRestart();
        Main.solarSystem.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>().StartFiringSequence();

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

    private IEnumerator ReplaceShadersWatch()
    {
        for (int i = 0; i < 10; i++) yield return null;

        float end = Time.realtimeSinceStartup + 8f;
        float next = 0f;
        while (Time.realtimeSinceStartup < end)
        {
            if (Time.realtimeSinceStartup >= next)
            {
                ReplaceAllShaders();
                next = Time.realtimeSinceStartup + 0.75f;
            }
            yield return null;
        }
    }

    private string ShaderName(Shader s)
    {
        int id = s.GetInstanceID();
        if (!_shaderNames.TryGetValue(id, out var n))
            _shaderNames[id] = n = s.name;
        return n;
    }

    private bool IsMoveToon(Material m) =>
        ShaderName(m.shader) == "Shader Graphs/RUMBLE_Move_Toon"
        || m.name.Contains("RUMBLE_Move_Toon");

    private static Texture FindTexture(Material m)
    {
        foreach (var p in TexProps)
        {
            if (m.HasProperty(p))
            {
                var t = m.GetTexture(p);
                if (t != null) return t;
            }
        }
        return null;
    }

    public void ReplaceAllShaders(bool onlyMoveToon = true)
    {
        if (replacementShader == null)
        {
            MelonLogger.Error("Replacement Shader variable is not assigned!");
            return;
        }
        
        int replaced = 0, ours = 0, noTex = 0;
        bool dumped = false;

        foreach (Renderer item in FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                     .Where(comp => comp.gameObject.scene.name != "DontDestroyOnLoad").ToArray())
        {
            Material mat = item.sharedMaterial;
            if (mat == null) continue;

            if (mat.name.StartsWith(ReplacedPrefix) || mat.shader == replacementShader)
            {
                ours++;
                continue;
            }

            bool match = IsMoveToon(mat);
            if (!match && !onlyMoveToon)
            {
                match = ShaderName(mat.shader) == "Shader Graphs/MobileEnvironmentUV0"
                        || item.gameObject.name.StartsWith("GYM_Patch");
            }
            if (!match) continue;

            if (!dumped)
            {
                dumped = true;
                MelonLogger.Msg($"[Shaders] t+{Time.realtimeSinceStartup - _loadTime:F2}s sample '{item.gameObject.name}': " +
                    $"shader='{mat.shader.name}' mat='{mat.name}' " +
                    $"texProps=[{string.Join(",", mat.GetTexturePropertyNames())}]");
            }

            int id = mat.GetInstanceID();
            if (!_replacementCache.TryGetValue(id, out var repl) || repl == null)
            {
                repl = new Material(replacementShader) { name = ReplacedPrefix + mat.name };
                Texture tex = FindTexture(mat);
                if (tex == null) noTex++;
                repl.SetTexture("Texture2D_2058E65A", tex);
                repl.SetTexture("Texture2D_3812B1EC", tex);
                repl.SetColor("Color_D943764B", Color.white);
                _replacementCache[id] = repl;
            }

            item.sharedMaterial = repl;
            item.lightmapIndex = -1;
            item.lightProbeUsage = 0;
            item.reflectionProbeUsage = 0;
            replaced++;
        }

        if (replaced > 0)
            MelonLogger.Msg($"[Shaders] t+{Time.realtimeSinceStartup - _loadTime:F2}s replaced={replaced} " +
                            $"noTexture={noTex} skipped(ours)={ours} onlyMoveToon={onlyMoveToon}");
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