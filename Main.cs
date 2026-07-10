using Il2CppInterop.Runtime.Injection;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using OuterWildsRumble.Components;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;
using BuildInfo = OuterWildsRumble.BuildInfo;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(OuterWildsRumble.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonAdditionalDependencies("UIFramework")]

namespace OuterWildsRumble
{
    public static class BuildInfo
    {
        public const string ModName = "OuterWildsRumble";
        public const string ModVersion = "1.8.0";
        public const string Description = "For my fellow hatchlings";
        public const string Author = "oreotrollturbo";
        public const string Company = "Rumble.LLC";
    }

    public class Main : MelonMod
    {
        public static SolarSystemData solarSystem;
        
        public static Camera playerCam;

        public static bool isInMatch;
        
        const string outerWildsBundlePath = "OuterWildsRumble.OuterWildsStuff.outerwilds";
        const string eventHorizonBundlePath = "OuterWildsRumble.OuterWildsStuff.eventhorizon";
        
        public static Shader atmosphereShader;
        public static Material copyDepthMaterial;
        public static AtmosphereProfile defaultAtmosphereProfile;
        public static ComputeShader opticalDepth;
        public static ScriptableRendererData activeRenderData;

        public static Shader replacementShader;

        public static GameObject prefabSignalScope;
        
        public static string folderPath = MelonEnvironment.UserDataDirectory + @"\OuterWildsRumble";
        
        public override void OnLateInitializeMelon()
        {
            if (!Directory.Exists(folderPath))
            {
                MelonLogger.Warning("File at " + folderPath + " does not exist");
                Directory.CreateDirectory(folderPath);
            }

            OwSystemSettings.Setup(this);
            
            Actions.onMapInitialized += SceneLoaded;
            
            Actions.onMapInitialized += (string val) => isInMatch = false;
            Actions.onMatchStarted += () => isInMatch = true;
            Actions.onMatchEnded += () => isInMatch = false;
            
            
            ClassInjector.RegisterTypeInIl2Cpp<Orbiter>();
            ClassInjector.RegisterTypeInIl2Cpp<EllipticalOrbiter>();
            ClassInjector.RegisterTypeInIl2Cpp<QuantumObject>();
            ClassInjector.RegisterTypeInIl2Cpp<HourGlassTwins>();
            ClassInjector.RegisterTypeInIl2Cpp<SolarSystem>();
            ClassInjector.RegisterTypeInIl2Cpp<SignalScope>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereEffect>();
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereProfile>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereRendererFeatureTest>();
            //ClassInjector.RegisterTypeInIl2Cpp<DepthStackRenderFeature>();
            //ClassInjector.RegisterTypeInIl2Cpp<TestRenderFeature>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereRenderPass>();
            //ClassInjector.RegisterTypeInIl2Cpp<BlitEndRenderPass>();
            //ClassInjector.RegisterTypeInIl2Cpp<BlitStartRenderPass>();
            //ClassInjector.RegisterTypeInIl2Cpp<DepthStackRenderPass>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereRendererFeatureTest>();

            //atmosphereShader = AssetBundles.LoadAssetFromStream<Shader>(this, outerWildsBundlePath, "AtmosphereNew");
            //copyDepthMaterial = AssetBundles.LoadAssetFromStream<Material>(this, outerWildsBundlePath, "CopyDepth");
            //opticalDepth = AssetBundles.LoadAssetFromStream<ComputeShader>(this, outerWildsBundlePath, "OpticalDepth");
            //defaultAtmosphereProfile =  AtmosphereProfileFactory.CreateDefaultAtmosphereProfile(opticalDepth);
            
            //Object.DontDestroyOnLoad(atmosphereShader);
            //Object.DontDestroyOnLoad(defaultAtmosphereProfile);
            //Object.DontDestroyOnLoad(opticalDepth);
            //atmosphereShader.hideFlags = HideFlags.DontUnloadUnusedAsset;
            //defaultAtmosphereProfile.hideFlags = HideFlags.DontUnloadUnusedAsset;
            //opticalDepth.hideFlags = HideFlags.DontUnloadUnusedAsset;
            
            //ScriptableObject.CreateInstance<TestRenderFeature>().hideFlags = HideFlags.DontUnloadUnusedAsset;
            
            //AtmosphereSetup.InjectAtmosphereFeatureAtRuntime(atmosphereShader);
            //AtmosphereSetup.AddAtmosphereFeatureAtRuntime();
            //AtmosphereSetup.Test();
            //AtmospherePassManager.Init(atmosphereShader);
            
            replacementShader = AssetBundles.LoadAssetFromStream<Shader>(this, outerWildsBundlePath, "ReplacementShader");
            replacementShader.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Object.DontDestroyOnLoad(replacementShader);

            if (replacementShader == null)
            {
                MelonLogger.Error("SHADER IS NULL NOOOOOOOOO"); //yikes
            }
        }
        

        private void SceneLoaded(string mapName)
        {
            if (OwSystemSettings.ShaderReplacement.Value)
            {
                ReplaceAllShaders();
            }

            if (OwSystemSettings.OnlySunLighting.Value)
            {
                HandleOnlySunLighting(mapName);
            }
            
            RenderSettings.fog = false; 
            playerCam = Camera.main;

            if (mapName == "Gym")
            {
                if (solarSystem.Root == null)
                {
                    SetupSolarSystem();
                }
                GameObject.Find("Player Controller(Clone)").transform.GetChild(2).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
            }

            if (OwSystemSettings.SignalScopeEnabled.Value)
            {
                solarSystem.SignalScope = GameObject.Instantiate(prefabSignalScope);
                solarSystem.SignalScope.AddComponent<SignalScope>();
                solarSystem.SignalScope.transform.localScale = new Vector3(2, 2, 2);
            }

            if (solarSystem.Root != null)
            {
                MelonCoroutines.Start(solarSystem.Sun.GetComponent<SupernovaSun>().FindPlayerAndSetup());
            }

            if (solarSystem.Sun.GetComponent<SupernovaSun>().currentPhase == SupernovaSun.Phase.Done && !OwSystemSettings.SunResetAfterSupernovaEnd.Value)
            {
                solarSystem.Sun.GetComponent<SupernovaSun>().ResetAfterExplosion();
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

            foreach (Renderer item in Object.FindObjectsOfType<Renderer>())
            {
                Material sharedMaterial = item.sharedMaterial;
                if ((Object)(object)sharedMaterial == (Object)null)
                {
                    continue;
                }
                if (((Object)sharedMaterial.shader).name == "Shader Graphs/MobileEnvironmentUV0")
                {
                    Shader val = replacementShader;
                    if ((Object)(object)item.sharedMaterial.shader != (Object)(object)val)
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
                item.lightProbeUsage = (LightProbeUsage)0;
                item.reflectionProbeUsage = (ReflectionProbeUsage)0;
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


        void SetupSolarSystem()
        {
            solarSystem.Root = new GameObject("OuterWilds_System");
            solarSystem.Root.AddComponent<SolarSystem>();
            
            GameObject.DontDestroyOnLoad(solarSystem.Root);
            
            MelonLogger.Msg("Loading assets");
            LoadAssets();
            
            MelonLogger.Msg("Creating solar system");
            CreateSun();
            CreateWhiteHole();
            SetupOrbitals();
            SetupPlayerShip();
            SetupTapeRecorder();
            SetupStarBackground();
            
            if (solarSystem.Sun != null) solarSystem.Sun.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.SunStation != null) solarSystem.SunStation.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.HourGlassTwins != null) solarSystem.HourGlassTwins.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.TimberHearth != null) solarSystem.TimberHearth.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.Attlerock != null) solarSystem.Attlerock.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.BrittleHollow != null) solarSystem.BrittleHollow.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.HollowsLantern != null) solarSystem.HollowsLantern.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.GiantsDeep != null) solarSystem.GiantsDeep.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.OrbitalProbeCannon != null) solarSystem.OrbitalProbeCannon.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.OrbitalProbe != null) solarSystem.OrbitalProbe.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.QuantumMoon != null) solarSystem.QuantumMoon.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.DarkBramble != null) solarSystem.DarkBramble.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.WhiteHole != null) solarSystem.WhiteHole.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.WhiteHoleStation != null) solarSystem.WhiteHoleStation.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.Interloper != null) solarSystem.Interloper.transform.SetParent(solarSystem.Root.transform, true);
            
            //if (solarSystem.PlayerShip != null) solarSystem.PlayerShip.transform.SetParent(solarSystem.Root.transform, true);
            
            OwSystemSettings.ApplyToSolarSystem();
            solarSystem.Root.transform.position += new Vector3(0, 260f, 0);
            
            solarSystem.Root.GetComponent<SolarSystem>().StartSolarSystem();
            MelonLogger.Msg($"Finished setup!");
        }
        
        

        public void LoadAssets()
        {
            // Helper to load, instantiate, and log errors in one go
            // If only there was a place you could check how to do it... (https://supercopia.github.io/RMAPI-Reference/06-Asset-Bundles/#from-embedded-resource-stream) (Joke.)

            var outerWildsBundle   = AssetBundles.LoadAssetBundleFromStream(this, outerWildsBundlePath);
            var eventHorizonBundle = AssetBundles.LoadAssetBundleFromStream(this, eventHorizonBundlePath);

            GameObject LoadAndSpawn(string assetName)
            {
                var asset = outerWildsBundle.LoadAsset<GameObject>(assetName);
                if (asset != null)
                {
                    MelonLogger.Msg($"Loaded: {assetName}");
                    return GameObject.Instantiate(asset);
                }
                MelonLogger.Error($"Failed to load asset: {assetName}");
                return null;
            }

            Material GetMaterial(string assetName)
            {
                var material = eventHorizonBundle.LoadAsset<Material>(assetName);
                if (material != null)
                {
                    MelonLogger.Msg($"Loaded: {assetName}");
                    return material;
                }

                MelonLogger.Error($"Failed to load material: {assetName}");
                return null;
            }

            solarSystem.Sun = LoadAndSpawn("SunV3");
            solarSystem.SunStation         = LoadAndSpawn("SunStation");
            solarSystem.HourGlassTwins = LoadAndSpawn("HourGlassTwinsGO");
            solarSystem.TimberHearth   = LoadAndSpawn("TimberHearth");
            solarSystem.Attlerock      = LoadAndSpawn("Attlerock");
            solarSystem.BrittleHollow      = LoadAndSpawn("BrittleHollowFullGO1");
            solarSystem.HollowsLantern     = LoadAndSpawn("HollowsLanternGO");
            solarSystem.LanternMeteor     = LoadAndSpawn("HotMeteorGO");
            solarSystem.GiantsDeep     = LoadAndSpawn("GiantsDeep");
            solarSystem.OrbitalProbeCannon = LoadAndSpawn("OrbitalProbeCannonGO");
            solarSystem.OrbitalProbe = LoadAndSpawn("NomaiProbe");
            solarSystem.QuantumMoon        = LoadAndSpawn("QuantumMoon");
            solarSystem.DarkBramble        = LoadAndSpawn("DarkBramble");
            solarSystem.WhiteHoleStation   = LoadAndSpawn("WhiteHoleStation");
            solarSystem.Interloper         = LoadAndSpawn("InterloperGameObject");

            solarSystem.PlayerShip         = LoadAndSpawn("HearthianSpaceShip");
            solarSystem.TapeRecorder         = LoadAndSpawn("ow_recorderGO");
            solarSystem.StarBackground         = LoadAndSpawn("StarsOW");

            // Prefab cache: instances die with the player belt, so re-instantiate in SceneLoaded
            prefabSignalScope = outerWildsBundle.LoadAsset<GameObject>("SignalscopeGO");
            prefabSignalScope.hideFlags = HideFlags.DontUnloadUnusedAsset;
            MelonLogger.Msg("Loaded Signalscope");

            solarSystem.WhiteHoleMaterial  = GetMaterial("WhiteHoleMaterial");
            solarSystem.BlackHoleMaterial  = GetMaterial("BlackholeMaterial");

            // Free the bundle data; instantiated GameObjects and Material references stay alive.
            outerWildsBundle.Unload(false);
            eventHorizonBundle.Unload(false);

            //Test stuff
            //LoadAndSpawn("AtmospherePlanet");
        }

        void CreateSun()
        {
            solarSystem.Sun.transform.position = solarSystem.Root.transform.position;
            solarSystem.Sun.transform.localScale = Vector3.one * 1.65f;
            solarSystem.Sun.name = "Sun";
    
            // let there be light
            Light sunLight = solarSystem.Sun.AddComponent<Light>();
            sunLight.type = LightType.Point;       
            sunLight.range = 19000f;     
            sunLight.intensity = 18000f;               
            sunLight.color = new Color(1f, 0.8392f, 0.7098f, 1f);
            sunLight.shadows = LightShadows.Soft;

            SupernovaSun supernovaSun = solarSystem.Sun.AddComponent<SupernovaSun>();
            supernovaSun.sunLight = sunLight;
        }

        void CreateWhiteHole()
        {
            solarSystem.WhiteHole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            solarSystem.WhiteHole.transform.position = solarSystem.Root.transform.position + new Vector3(0,0, 18.2f);
            solarSystem.WhiteHole.transform.localScale = Vector3.one * 0.14f;
            solarSystem.WhiteHole.name = "WhiteHole";

            Renderer r = solarSystem.WhiteHole.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = solarSystem.WhiteHoleMaterial;
            }
            
            Light whiteHoleLight = solarSystem.WhiteHole.AddComponent<Light>();
            whiteHoleLight.type = LightType.Point;       
            whiteHoleLight.range = 300f;
            whiteHoleLight.intensity = 130f;
            whiteHoleLight.color = new Color(1f, 1f, 1f, 1f);
            whiteHoleLight.shadows = LightShadows.Soft;  
        }

        void SetupOrbitals()
        {
            //Planets from sun 4.5 7.9 10.85 15.35 18.6 21.5
            //2.28 4.0 5.49 7.77 9.42 10.89
            
            //Moons from planets 0.85 0.97 1.5
            //0.430 0.491 0.759
            
            if (solarSystem.Sun != null)
            {
                SetupSunStation();
                SetupHourGlassTwins();
                SetupTimberHearth();
                SetupBrittleHollow();
                SetupGiantsDeep();
                SetupDarkBramble();
                SetupWhiteHoleStation();
                SetupInterloper();
                // must be called after "parent" planets
                SetupQuantumMoon();
                
                MusicEmitter emitter = solarSystem.HourGlassTwins.AddComponent<MusicEmitter>();
                emitter.musicFileName = "OW_TravelerTheme_drums.wav"; 
                
                MusicEmitter emitter1 = solarSystem.Attlerock.AddComponent<MusicEmitter>();
                emitter1.musicFileName = "OW_TravelerTheme_whistling.wav";
                
                MusicEmitter emitter2 = solarSystem.BrittleHollow.AddComponent<MusicEmitter>();
                emitter2.musicFileName = "OW_TravelerTheme_banjo.wav";
                
                MusicEmitter emitter3 = solarSystem.GiantsDeep.AddComponent<MusicEmitter>();
                emitter3.musicFileName = "OW_TravelerTheme_flute.wav"; 
                
                MusicEmitter emitter4 = solarSystem.DarkBramble.AddComponent<MusicEmitter>();
                emitter4.musicFileName = "OW_TravelerTheme_harmonica.wav";
                
                MusicEmitter emitter6 = solarSystem.TimberHearth.AddComponent<MusicEmitter>();
                emitter6.musicFileName = "OW_TravelerTheme_harmonica.wav";
                emitter6.detectionAngle *= 0.6f;
                
                MusicEmitter emitter5 = solarSystem.QuantumMoon.AddComponent<MusicEmitter>();
                emitter5.musicFileName = "OW_TravelerTheme_piano.wav";
                
                
                SupernovaSun sunScript = solarSystem.Sun.GetComponent<SupernovaSun>();
                if (sunScript != null)
                {
                    sunScript.SetBodiesToSwallow(new List<Transform>
                    {
                        solarSystem.HourGlassTwins.transform,
                        solarSystem.TimberHearth.transform,
                        solarSystem.Attlerock.transform,
                        solarSystem.BrittleHollow.transform,
                        solarSystem.HollowsLantern.transform,
                        solarSystem.GiantsDeep.transform,
                        solarSystem.OrbitalProbeCannon.transform,
                        solarSystem.DarkBramble.transform,
                        solarSystem.QuantumMoon.transform,
                        solarSystem.WhiteHole.transform,
                        solarSystem.Interloper.transform,
                        solarSystem.SunStation.transform,
                        solarSystem.WhiteHoleStation.transform,
                    });
                }
            }
        }

        void SetupSunStation()
        {
            GameObject sunStationPivot = new GameObject("SunStationPivot");
            
            solarSystem.SunStation.transform.SetParent(sunStationPivot.transform, false);
            solarSystem.SunStation.transform.localScale = Vector3.one * 0.047f;
            solarSystem.SunStation.transform.localPosition = Vector3.zero; 
            solarSystem.SunStation.transform.Rotate(0, 180f, 0);
            
            Orbiter sunStation = sunStationPivot.AddComponent<Orbiter>();

            sunStation.randomisePos = false;
            sunStation.orbitParent = solarSystem.Sun.transform;  
            sunStation.orbitDistance = 23/30f;
            sunStation.orbitSpeed = 16f;     
            sunStation.spinSpeed = 16f;
            sunStation.orbitAxis = Vector3.up;
            
            solarSystem.SunStation = sunStationPivot;
        }

        void SetupHourGlassTwins()
        {
            solarSystem.HourGlassTwins.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter hourGlassTwins = solarSystem.HourGlassTwins.AddComponent<Orbiter>();
            
            hourGlassTwins.orbitParent = solarSystem.Sun.transform;  
            hourGlassTwins.orbitDistance = 3.88f;           
            hourGlassTwins.orbitSpeed = 2.27f;          
            hourGlassTwins.spinSpeed = 20.5f;
            hourGlassTwins.orbitAxis = Vector3.up;

            HourGlassTwins sandComponent = solarSystem.HourGlassTwins.AddComponent<HourGlassTwins>();
            //Keeping default settings
        }

        void SetupTimberHearth()
        {
            solarSystem.TimberHearth.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter heartOrbit = solarSystem.TimberHearth.AddComponent<Orbiter>();
            
            heartOrbit.orbitParent = solarSystem.Sun.transform;  
            heartOrbit.orbitDistance = 5.6f;           
            heartOrbit.orbitSpeed = 1f;             
            heartOrbit.spinSpeed = 7.5f;       
            heartOrbit.orbitAxis = Vector3.up;
            
            //AtmosphereEffect atmosphere = solarSystem.TimberHearth.AddComponent<AtmosphereEffect>();
            //atmosphere.sun = solarSystem.Sun.transform;
            //atmosphere.directional = false;
            //atmosphere.cutoffDepth = 0.5f;
            //atmosphere.planetRadius = 90f;
            //atmosphere.profile = defaultAtmosphereProfile;
            //TODO harmonica
            
            
            if (solarSystem.Attlerock != null)
            {
                solarSystem.Attlerock.transform.localScale = Vector3.one * 0.05f;
                solarSystem.Attlerock.transform.rotation = Quaternion.Euler(3.4551f, 6.3135f, 350.4707f);

                Orbiter rockOrbit = solarSystem.Attlerock.AddComponent<Orbiter>();
                
                rockOrbit.orbitParent = solarSystem.TimberHearth.transform; 
                rockOrbit.orbitDistance = 0.8f;                
                rockOrbit.orbitSpeed = 15f;                 
                rockOrbit.spinSpeed = 15f;
                rockOrbit.orbitAxis = new Vector3(0.1f, 1f, 0f).normalized;
                rockOrbit.randomisePos = false;
            }
        }
        
        void SetupBrittleHollow()
        {
            solarSystem.BrittleHollow.transform.localScale = Vector3.one * 0.1f;
            
            Orbiter brittleHollowOrbit = solarSystem.BrittleHollow.AddComponent<Orbiter>();
    
            brittleHollowOrbit.orbitParent = solarSystem.Sun.transform;  
            brittleHollowOrbit.orbitDistance = 7.8f;          
            brittleHollowOrbit.orbitSpeed = 0.8f;            
            brittleHollowOrbit.spinSpeed = 7f;     
            brittleHollowOrbit.orbitAxis = Vector3.up;

            solarSystem.BrittleHollow.AddComponent<BrittleHollow>();

            if (solarSystem.BlackHoleMaterial)
            {
                GameObject blackHole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blackHole.name = "BlackHole";

                Renderer r = blackHole.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = solarSystem.BlackHoleMaterial;
                }
                
                blackHole.transform.SetParent(solarSystem.BrittleHollow.transform,false);
                blackHole.transform.localPosition = new Vector3(0, 0, 0);
                blackHole.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            }
    
            if (solarSystem.HollowsLantern != null)
            {
                solarSystem.HollowsLantern.transform.localScale = Vector3.one * 0.05f;
                solarSystem.HollowsLantern.transform.rotation = Quaternion.Euler(3.4551f, 6.3135f, 350.4707f);

                Orbiter lanternOrbit = solarSystem.HollowsLantern.AddComponent<Orbiter>();
        
                lanternOrbit.orbitParent = solarSystem.BrittleHollow.transform; 
                lanternOrbit.orbitDistance = 0.66f;             
                lanternOrbit.orbitSpeed = 20f;              
                lanternOrbit.spinSpeed = 30f;            
                lanternOrbit.orbitAxis = new Vector3(0.1f, 1f, 0f).normalized;
                
                HollowsLantern hollowsLantern = solarSystem.HollowsLantern.AddComponent<HollowsLantern>();
                solarSystem.LanternMeteor.transform.localScale = Vector3.one * 0.018f;
                
                Light lanternLight = solarSystem.HollowsLantern.transform.GetChild(0).gameObject.AddComponent<Light>();
                lanternLight.type = LightType.Point;       
                lanternLight.range = 300f;                 
                lanternLight.intensity = 100f;               
                lanternLight.color = new Color(0.851f, 0.404f, 0.059f, 1f);
                lanternLight.shadows = LightShadows.Soft;  
            }
        }

        void SetupGiantsDeep()
        {
            solarSystem.GiantsDeep.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter giantsDeepOrbit = solarSystem.GiantsDeep.AddComponent<Orbiter>();
            
            giantsDeepOrbit.orbitParent = solarSystem.Sun.transform;  
            giantsDeepOrbit.orbitDistance = 10.6f;           
            giantsDeepOrbit.orbitSpeed = 0.6f;             
            giantsDeepOrbit.spinSpeed = 0.2f;       
            giantsDeepOrbit.orbitAxis = Vector3.up;       
            
            if (solarSystem.OrbitalProbeCannon != null)
            {
                solarSystem.OrbitalProbeCannon.transform.localScale = Vector3.one * 0.001f;
                solarSystem.OrbitalProbeCannon.transform.rotation = Quaternion.Euler(3.4551f, 6.3135f, 350.4707f);

                Orbiter cannonOrbit = solarSystem.OrbitalProbeCannon.AddComponent<Orbiter>();
                
                cannonOrbit.orbitParent = solarSystem.GiantsDeep.transform; 
                cannonOrbit.orbitDistance = 1.3f;                
                cannonOrbit.orbitSpeed = 10f;                 
                cannonOrbit.spinSpeed = 10f;
                cannonOrbit.orbitAxis = Vector3.up;
                cannonOrbit.randomisePos = false;
                
                var probeCannon = solarSystem.OrbitalProbeCannon.AddComponent<OrbitalProbeCannon>();
                probeCannon.orbiter = cannonOrbit;

                if (solarSystem.OrbitalProbe != null)
                {
                    solarSystem.OrbitalProbe.transform.localScale = Vector3.one * (0.00003333333f * OwSystemSettings.SolarSystemScale.Value);
                    solarSystem.OrbitalProbe.transform.SetParent(solarSystem.Sun.transform, true);
                    solarSystem.OrbitalProbe.AddComponent<OrbitalProbe>();
                }
            }
        }

        void SetupDarkBramble()
        {
            solarSystem.DarkBramble.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter darkBrambleOrbit = solarSystem.DarkBramble.AddComponent<Orbiter>();
            
            darkBrambleOrbit.orbitParent = solarSystem.Sun.transform;  
            darkBrambleOrbit.orbitDistance = 14.6f;           
            darkBrambleOrbit.orbitSpeed = 0.38f;             
            darkBrambleOrbit.spinSpeed = 0f;       
            darkBrambleOrbit.orbitAxis = Vector3.up;
        }

        void SetupWhiteHoleStation()
        {
            solarSystem.WhiteHoleStation.transform.localScale = Vector3.one * 0.1f;
            
            Vector3 targetPosition = solarSystem.WhiteHole.transform.position;
            targetPosition.z -= 0.8f;
            solarSystem.WhiteHoleStation.transform.position = targetPosition;
            solarSystem.WhiteHoleStation.transform.rotation = Quaternion.Euler(0, 90f, 0);
        }
        
        void SetupInterloper()
        {
            GameObject interloper = solarSystem.Interloper;
            interloper.transform.localScale = Vector3.one * 0.1f;
    
            EllipticalOrbiter interloperOrbiter = interloper.AddComponent<EllipticalOrbiter>();

            Transform ice = interloper.transform.GetChild(0).GetChild(0);
            ice.localScale = new Vector3(1.2f,1f,1f);
            interloperOrbiter.iceTransform = ice;
            
            interloperOrbiter.focusA = solarSystem.Sun.transform;
            interloperOrbiter.focusB = solarSystem.WhiteHole.transform; 
            
            // semiMinorAxis determines how "fat" or "skinny" the ellipse is
            interloperOrbiter.semiMinorAxis = 5.66f; 
            
            interloperOrbiter.orbitSpeed = 11f;
            interloperOrbiter.speedIntensity = 1.1f;
            interloperOrbiter.spinAxis = Vector3.up;
        }
        
        
        void SetupQuantumMoon()
        {
            solarSystem.QuantumMoon.transform.localScale = Vector3.one * 10f;
            solarSystem.QuantumMoon.transform.rotation = Quaternion.identity;

            Orbiter quantumMoonOrbit = solarSystem.QuantumMoon.AddComponent<Orbiter>();
            
            quantumMoonOrbit.orbitDistance = 1.7f;                
            quantumMoonOrbit.orbitSpeed = 2f;                 
            quantumMoonOrbit.spinSpeed = 4f;    
            quantumMoonOrbit.orbitAxis = new Vector3(1, 1, 0);
            
            QuantumOrbiter quantumObject = solarSystem.QuantumMoon.AddComponent<QuantumOrbiter>();

            quantumObject.orbitParents = new Dictionary<Transform, float> 
            { 
                { solarSystem.HourGlassTwins.transform.GetChild(2), 0.59f },
                { solarSystem.HourGlassTwins.transform.GetChild(0), 0.59f },
                { solarSystem.TimberHearth.transform,   0.96f },
                { solarSystem.BrittleHollow.transform,  0.87f },
                { solarSystem.GiantsDeep.transform,     1.7f },
                { solarSystem.DarkBramble.transform,    1.7f }
            };
        }

        void SetupPlayerShip()
        {
            solarSystem.PlayerShip.AddComponent<PlayerShip>();
        }
        
        void SetupTapeRecorder()
        {
            solarSystem.TapeRecorder.AddComponent<QuantumTapeRecorder>();

            solarSystem.TapeRecorder.SetActive(OwSystemSettings.TapeRecorderToggle.Value);
        }
        
        void SetupStarBackground()
        {
            solarSystem.StarBackground.AddComponent<StarBackground>();
        }
    }
    
    
    public struct SolarSystemData
    {
        public GameObject Root;
        
        public GameObject Sun;
        public GameObject SunStation;
        
        public GameObject HourGlassTwins;
        
        public GameObject TimberHearth;
        public GameObject Attlerock;
        
        public GameObject BrittleHollow;
        public GameObject HollowsLantern;
        public GameObject LanternMeteor;
        
        public GameObject GiantsDeep;
        public GameObject OrbitalProbeCannon;
        public GameObject OrbitalProbe;
        public GameObject QuantumMoon;
        
        public GameObject DarkBramble;
        
        public GameObject WhiteHole;
        public GameObject WhiteHoleStation;
        
        public GameObject Interloper;

        public GameObject PlayerShip;
        public GameObject SignalScope;
        public GameObject TapeRecorder;
        public GameObject StarBackground;

        public Material BlackHoleMaterial;
        public Material WhiteHoleMaterial;
    }
    
    
    public class ButtonWithLabel
    {
        public GameObject button;
        public GameObject label;

        public ButtonWithLabel(Vector3 localPosition, string labelText, string objectName, Transform parent)
        {
            // Create button at origin with identity rotation
            button = Create.NewButton(
                Vector3.zero, 
                Quaternion.identity
            );
    
            button.name = objectName;
            if (parent != null)
            {
                button.transform.SetParent(parent, false); // Set parent while maintaining local space
            }

            // Set local transforms
            button.transform.localPosition = localPosition;
            button.transform.localRotation = Quaternion.Euler(90, 180, 0);
    
            // Create label as child of button
            label = Create.NewText(labelText, 0.5f, Color.white, Vector3.zero, Quaternion.identity);
            label.name = objectName + " label";
            label.transform.SetParent(button.transform, false);
            label.transform.localPosition = new Vector3(0, 0.1f, 0f); // Position below button   0f, 0f, 0.12f
            label.transform.localRotation = Quaternion.Euler(90, 180, 0);
            
            TextMeshPro tmp = label.GetComponent<TextMeshPro>();
            tmp.text = labelText;
            tmp.enableWordWrapping = false;       // Prevents line breaks
            tmp.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely
        }

        public ButtonWithLabel(Vector3 localPosition, string labelText, string objectName, GameObject parent)
            : this(localPosition, labelText, objectName, parent.transform)
        {
        }

        public void ChangeLabelText(string newLabel)
        {
            label.GetComponent<TextMeshPro>().text = newLabel;
        }

        public void Destroy()
        {
            GameObject.Destroy(button);
            GameObject.Destroy(label);
        }
    }
}