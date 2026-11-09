using System.Collections;
using Il2CppInterop.Runtime.Injection;
using Il2CppPhoton.Pun;
using Il2CppRUMBLE.Interactions.InteractionBase;
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
        public const string ModVersion = "2.0.0";
        public const string Description = "For my fellow hatchlings";
        public const string Author = "oreotrollturbo";
        public const string Company = "Rumble.LLC";
    }

    public class Main : Utilities.RumbleMod
    {
        public static SolarSystemData solarSystem;
        
        public static Camera playerCam;

        public static bool isInMatch;
        
        const string outerWildsBundlePath = "OuterWildsRumble.Assetbundles.outerwilds";
        const string eventHorizonBundlePath = "OuterWildsRumble.Assetbundles.eventhorizon";
        
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
            
            Actions.onMapInitialized += (string val) =>
            {
                isInMatch = val == "Map0" || val == "Map1";
            };
            Actions.onMatchStarted += () => isInMatch = true;
            Actions.onMatchEnded += () => isInMatch = false;
            
            
            ClassInjector.RegisterTypeInIl2Cpp<Orbiter>();
            ClassInjector.RegisterTypeInIl2Cpp<EllipticalOrbiter>();
            ClassInjector.RegisterTypeInIl2Cpp<QuantumObject>();
            ClassInjector.RegisterTypeInIl2Cpp<HourGlassTwins>();
            ClassInjector.RegisterTypeInIl2Cpp<SolarSystem>();
            ClassInjector.RegisterTypeInIl2Cpp<SignalScope>();
            ClassInjector.RegisterTypeInIl2Cpp<MarshmallowStick>();
            
            SolarSystem.replacementShader = AssetBundles.LoadAssetFromStream<Shader>(this, outerWildsBundlePath, "ReplacementShader");
            SolarSystem.replacementShader.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Object.DontDestroyOnLoad(SolarSystem.replacementShader);

            if (SolarSystem.replacementShader == null)
            {
                MelonLogger.Error("SHADER IS NULL NOOOOOOOOO");
            }
        }
        

        private void SceneLoaded(string mapName)
        {
            Calls.Players.GetLocalPlayer().Controller.gameObject.transform.GetChild(2).GetChild(0).GetChild(0).gameObject.GetComponent<Camera>().farClipPlane
                = OwSystemSettings.ViewDistance.Value;
            
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
                solarSystem.SignalScope = GameObject.Instantiate(SignalScope.prefabSignalScope);
                solarSystem.SignalScope.AddComponent<SignalScope>();
                solarSystem.SignalScope.transform.localScale = new Vector3(2, 2, 2);
            }
            
            if (OwSystemSettings.MarshmallowStickIsEnabled.Value)
            {
                solarSystem.MarshmallowStick = MarshmallowStick.BuildMarshmallowStick();
                if (solarSystem.MarshmallowStick != null)
                    solarSystem.MarshmallowStick.AddComponent<MarshmallowStick>();
                else
                    MelonLogger.Error("Marshmallow assets missing, stick not spawned");
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

        void SetupSolarSystem()
        {
            solarSystem.Root = new GameObject("OuterWilds_System");
            var pv = solarSystem.Root.AddComponent<PhotonView>();
            pv.ViewID = 6767;
            solarSystem.Root.AddComponent<SolarSystem>();
            solarSystem.Root.AddComponent<SolarSystemNetworking>();
            solarSystem.Root.AddComponent<SolarSystemRealisticMode>();
            
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
            
            if (solarSystem.HearthianMapSatelite != null) solarSystem.HearthianMapSatelite.transform.SetParent(solarSystem.Root.transform, true);
            
            if (solarSystem.Stranger != null) solarSystem.Stranger.transform.SetParent(solarSystem.Root.transform, true);
            
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

            StarBackground.starShader = outerWildsBundle.LoadAsset<Shader>("StarGridColor");
            StarBackground.starShader.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Object.DontDestroyOnLoad(StarBackground.starShader);

            StarBackground.starTexture = outerWildsBundle.LoadAsset<Texture>("effects_spa_star_d");
            if (StarBackground.starTexture != null)
            {
                StarBackground.starTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
                Object.DontDestroyOnLoad(StarBackground.starTexture);
            }
            
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
            solarSystem.HearthianMapSatelite         = LoadAndSpawn("HearthianMapSatellite");
            solarSystem.Stranger           = LoadAndSpawn("StrangerPrefab");

            solarSystem.PlayerShip         = LoadAndSpawn("HearthianSpaceShip");
            solarSystem.TapeRecorder         = LoadAndSpawn("ow_recorderGO");
            solarSystem.StarBackground         = LoadAndSpawn("StarsOWPrefab");

            // Prefab cache: instances die with the player belt, so re-instantiate in SceneLoaded
            SignalScope.prefabSignalScope = outerWildsBundle.LoadAsset<GameObject>("SignalscopeGO");
            SignalScope.prefabSignalScope.hideFlags = HideFlags.DontUnloadUnusedAsset;
            MelonLogger.Msg("Loaded Signalscope");
            
            MarshmallowStick.marshmallowStickAsset = outerWildsBundle.LoadAsset<GameObject>("RoastingStick");
            MarshmallowStick.marshmallowStickAsset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            MarshmallowStick.marshmallowAsset = outerWildsBundle.LoadAsset<GameObject>("Marshmallow");
            MarshmallowStick.marshmallowAsset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            
            MelonLogger.Msg("Loaded MarshmallowStick");

            solarSystem.WhiteHoleMaterial  = GetMaterial("WhiteHoleMaterial");
            solarSystem.BlackHoleMaterial  = GetMaterial("BlackholeMaterial");

            outerWildsBundle.Unload(false);
            eventHorizonBundle.Unload(false);
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

        void SetupOrbitals() //TODO remove anything that's settings driven
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
                SetupHeaSatelite();
                SetupStranger();
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
                
                //MusicEmitter emitter7 = solarSystem.HearthianMapSatelite.transform.GetChild(1).GetChild(1).gameObject.AddComponent<MusicEmitter>();
                MusicEmitter emitter7 = solarSystem.HearthianMapSatelite.AddComponent<MusicEmitter>();
                emitter7.musicFileName = "OW_PR_ProbeInAirSound.wav";
                emitter7.detectionAngle *= 0.9f;
                
                SupernovaSun sunScript = solarSystem.Sun.GetComponent<SupernovaSun>();
                if (sunScript != null)
                {
                    var swallow = new List<Transform>
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
                    };
                    if (solarSystem.Stranger != null) swallow.Add(solarSystem.Stranger.transform);
                    sunScript.SetBodiesToSwallow(swallow);
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
            //TODO legacy stuff need to remove from everywhere since its settings driven now  
            hourGlassTwins.orbitDistance = 3.88f;         
            hourGlassTwins.orbitSpeed = 2.27f;          
            hourGlassTwins.spinSpeed = 12.5f;
            hourGlassTwins.orbitAxis = Vector3.up;
            
            if (OwSystemSettings.RealisticMode.Value)
            {
                hourGlassTwins._currentOrbitAngle = 310;
                HourGlassTwins.randomSandStage = false;
            }

            HourGlassTwins sandComponent = solarSystem.HourGlassTwins.AddComponent<HourGlassTwins>();
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
                
                if (OwSystemSettings.RealisticMode.Value)
                {
                    heartOrbit._currentOrbitAngle = 255.4f;
                    heartOrbit._currentSpinAngle = 316.4f;
                    
                    rockOrbit._currentOrbitAngle = 30.6f;
                    rockOrbit._currentSpinAngle = 30.6f;
                }
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

            if (OwSystemSettings.RealisticMode.Value)
            {
                brittleHollowOrbit._currentOrbitAngle = 94;
            }

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
            
            Orbiter giantsDeepOrbit = solarSystem.GiantsDeep.AddComponent<Orbiter>();
            
            giantsDeepOrbit.orbitParent = solarSystem.Sun.transform;  
            giantsDeepOrbit.orbitDistance = 10.6f;           
            giantsDeepOrbit.orbitSpeed = 0.6f;             
            giantsDeepOrbit.spinSpeed = 0.2f;       
            giantsDeepOrbit.orbitAxis = Vector3.up;

            if (OwSystemSettings.RealisticMode.Value)
            {
                giantsDeepOrbit.randomisePos = false;
                giantsDeepOrbit._currentOrbitAngle = 184;
            }
            
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

                if (OwSystemSettings.RealisticMode.Value)
                {
                    cannonOrbit._currentOrbitAngle = 320;
                }
                
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
            
            Orbiter darkBrambleOrbit = solarSystem.DarkBramble.AddComponent<Orbiter>();
            
            darkBrambleOrbit.orbitParent = solarSystem.Sun.transform;  
            darkBrambleOrbit.orbitDistance = 14.6f;           
            darkBrambleOrbit.orbitSpeed = 0.38f;             
            darkBrambleOrbit.spinSpeed = 0f;       
            darkBrambleOrbit.orbitAxis = Vector3.up;

            if (OwSystemSettings.RealisticMode.Value)
            {
                darkBrambleOrbit._currentOrbitAngle = 140;
            }
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

        void SetupHeaSatelite()
        {
            solarSystem.HearthianMapSatelite.transform.rotation = Quaternion.Euler(0, 180f, 0);
            var orbit = solarSystem.HearthianMapSatelite.AddComponent<Orbiter>();
            orbit.randomisePos = false;
            
            solarSystem.HearthianMapSatelite.transform.localScale = Vector3.one * 0.0073f;
            
            orbit.orbitParent = solarSystem.Sun.transform;
            orbit.orbitAxis = new Vector3(1f, 0, 0);
            orbit.spinAxis = new Vector3(1f, 0, 0);
            orbit.orbitDistance = 700f; //Dictated by the scaler and not this :(
            
            orbit.orbitSpeed = -0.4f;
            orbit.spinSpeed = -0.4f;

            var satelite = solarSystem.HearthianMapSatelite.AddComponent<HearthianMapSatelite>();
        }

        void SetupStranger()
        {
            if (solarSystem.Stranger == null) return;

            var stranger = solarSystem.Stranger.AddComponent<Stranger>();
            stranger.sun = solarSystem.Sun.transform;
            solarSystem.Stranger.transform.localScale = Vector3.one * 0.0004f;
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
            solarSystem.StarBackground.transform.SetParent(solarSystem.Root.transform,true);
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

        public GameObject HearthianMapSatelite;

        public GameObject Stranger;

        public GameObject PlayerShip;
        public GameObject SignalScope;
        public GameObject MarshmallowStick;
        public GameObject TapeRecorder;
        public GameObject StarBackground;

        public Material BlackHoleMaterial;
        public Material WhiteHoleMaterial;
    }
}