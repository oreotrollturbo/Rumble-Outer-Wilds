using System.IO;
using MelonLoader;
using MelonLoader.Preferences;
using OuterWildsRumble.Components;
using UIFramework;

namespace OuterWildsRumble.UIFrameworkSettings
{
    public static class OwSystemSettings
    {
        // ── Paths ─────────────────────────────────────────────────────────────────
        private const string UserDataPath = "UserData/OuterWildsRumble/";
        private const string ConfigFile = "settings.cfg";
        
        public const float SolarSystemScale = 30f;
        
        private static MelonPreferences_Category globalCat;
        private static MelonPreferences_Category sunCat;
        private static MelonPreferences_Category sunStationCat;
        private static MelonPreferences_Category hourglassCat;
        private static MelonPreferences_Category timberHearthCat;
        private static MelonPreferences_Category attlerockCat;
        private static MelonPreferences_Category brittleHollowCat;
        private static MelonPreferences_Category hollowsLanternCat;
        private static MelonPreferences_Category giantsDeepCat;
        private static MelonPreferences_Category probeCannonCat;
        private static MelonPreferences_Category orbitalProbeCat;
        private static MelonPreferences_Category quantumMoonCat;
        private static MelonPreferences_Category darkBrambleCat;
        private static MelonPreferences_Category whiteHoleCat;
        private static MelonPreferences_Category whiteHoleStationCat;
        private static MelonPreferences_Category interloperCat;
        private static MelonPreferences_Category playerShipCat;

        // =========================================================================
        // GLOBAL
        // =========================================================================
        public static MelonPreferences_Entry<bool> EnabledEntry;

        // =========================================================================
        // SUN
        // =========================================================================
        public static MelonPreferences_Entry<bool>  SunEnabled;
        public static MelonPreferences_Entry<bool>  SunDoTimeLoop;
        public static MelonPreferences_Entry<int>   SunSecondsToFullRed;
        public static MelonPreferences_Entry<float> SunWaitAfterRed;
        public static MelonPreferences_Entry<float> SunExpansionSpeed;
        public static MelonPreferences_Entry<float> SunCollapseDuration;
        public static MelonPreferences_Entry<float> SunExplosionDuration;

        // =========================================================================
        // SUN STATION
        // =========================================================================
        public static MelonPreferences_Entry<bool>  SunStationEnabled;
        public static MelonPreferences_Entry<float> SunStationOrbitDistance;
        public static MelonPreferences_Entry<float> SunStationOrbitSpeed;
        public static MelonPreferences_Entry<float> SunStationSpinSpeed;

        // =========================================================================
        // HOURGLASS TWINS
        // =========================================================================
        public static MelonPreferences_Entry<bool>  HourGlassTwinsEnabled;
        public static MelonPreferences_Entry<float> HourGlassTwinsOrbitDistance;
        public static MelonPreferences_Entry<float> HourGlassTwinsOrbitSpeed;
        public static MelonPreferences_Entry<float> HourGlassTwinsSpinSpeed;
        public static MelonPreferences_Entry<float> HourGlassTwinsTransferDuration;
        public static MelonPreferences_Entry<float> HourGlassTwinsWaitDuration;
        public static MelonPreferences_Entry<bool>  HourGlassTwinsRandomSandStage;

        // =========================================================================
        // TIMBER HEARTH
        // =========================================================================
        public static MelonPreferences_Entry<bool>  TimberHearthEnabled;
        public static MelonPreferences_Entry<float> TimberHearthOrbitDistance;
        public static MelonPreferences_Entry<float> TimberHearthOrbitSpeed;
        public static MelonPreferences_Entry<float> TimberHearthSpinSpeed;

        // =========================================================================
        // ATTLEROCK
        // =========================================================================
        public static MelonPreferences_Entry<bool>  AttlerockEnabled;
        public static MelonPreferences_Entry<float> AttlerockOrbitDistance;
        public static MelonPreferences_Entry<float> AttlerockOrbitSpeed;
        public static MelonPreferences_Entry<float> AttlerockSpinSpeed;

        // =========================================================================
        // BRITTLE HOLLOW
        // =========================================================================
        public static MelonPreferences_Entry<bool>  BrittleHollowEnabled;
        public static MelonPreferences_Entry<float> BrittleHollowOrbitDistance;
        public static MelonPreferences_Entry<float> BrittleHollowOrbitSpeed;
        public static MelonPreferences_Entry<float> BrittleHollowSpinSpeed;
        public static MelonPreferences_Entry<bool> BrittleHollowBreakAppart;
        public static MelonPreferences_Entry<float> BrittleHollowBreakIntervalMin;
        public static MelonPreferences_Entry<float> BrittleHollowBreakIntervalMax;
        public static MelonPreferences_Entry<float> BrittleHollowSuckSpeed;
        public static MelonPreferences_Entry<float> BrittleHollowDriftSpeed;
        public static MelonPreferences_Entry<float> BrittleHollowDriftMaxRadius;

        // =========================================================================
        // HOLLOW'S LANTERN
        // =========================================================================
        public static MelonPreferences_Entry<bool>  HollowsLanternEnabled;
        public static MelonPreferences_Entry<float> HollowsLanternOrbitDistance;
        public static MelonPreferences_Entry<float> HollowsLanternOrbitSpeed;
        public static MelonPreferences_Entry<float> HollowsLanternSpinSpeed;

        // =========================================================================
        // GIANT'S DEEP
        // =========================================================================
        public static MelonPreferences_Entry<bool>  GiantsDeepEnabled;
        public static MelonPreferences_Entry<float> GiantsDeepOrbitDistance;
        public static MelonPreferences_Entry<float> GiantsDeepOrbitSpeed;
        public static MelonPreferences_Entry<float> GiantsDeepSpinSpeed;

        // =========================================================================
        // ORBITAL PROBE CANNON
        // =========================================================================
        public static MelonPreferences_Entry<bool>  OrbitalProbeCannonEnabled;
        public static MelonPreferences_Entry<float> OrbitalProbeCannonOrbitDistance;
        public static MelonPreferences_Entry<float> OrbitalProbeCannonOrbitSpeed;
        public static MelonPreferences_Entry<float> OrbitalProbeCannonSpinSpeed;
        public static MelonPreferences_Entry<bool> OrbitalProbeCannonFire;
        public static MelonPreferences_Entry<float> OrbitalProbeCannonTimeToAim;
        public static MelonPreferences_Entry<float> OrbitalProbeCannonExplosionTime;
        public static MelonPreferences_Entry<float> OrbitalProbeCannonBreakupDuration;

        // =========================================================================
        // ORBITAL PROBE
        // =========================================================================
        public static MelonPreferences_Entry<bool>  OrbitalProbeEnabled;
        public static MelonPreferences_Entry<float> OrbitalProbeSpeed;

        // =========================================================================
        // QUANTUM MOON
        // =========================================================================
        public static MelonPreferences_Entry<bool>  QuantumMoonEnabled;
        public static MelonPreferences_Entry<float> QuantumMoonOrbitDistance;
        public static MelonPreferences_Entry<float> QuantumMoonOrbitSpeed;
        public static MelonPreferences_Entry<float> QuantumMoonSpinSpeed;
        public static MelonPreferences_Entry<bool> QuantumMoonPlayMusic;

        // =========================================================================
        // DARK BRAMBLE
        // =========================================================================
        public static MelonPreferences_Entry<bool>  DarkBrambleEnabled;
        public static MelonPreferences_Entry<float> DarkBrambleOrbitDistance;
        public static MelonPreferences_Entry<float> DarkBrambleOrbitSpeed;

        // =========================================================================
        // WHITE HOLE
        // =========================================================================
        public static MelonPreferences_Entry<bool> WhiteHoleEnabled;

        // =========================================================================
        // WHITE HOLE STATION
        // =========================================================================
        public static MelonPreferences_Entry<bool> WhiteHoleStationEnabled;

        // =========================================================================
        // INTERLOPER
        // =========================================================================
        public static MelonPreferences_Entry<bool>  InterloperEnabled;
        public static MelonPreferences_Entry<float> InterloperSemiMinorAxis;
        public static MelonPreferences_Entry<float> InterloperOrbitSpeed;
        public static MelonPreferences_Entry<float> InterloperSpeedIntensity;

        // =========================================================================
        // PLAYER SHIP
        // =========================================================================
        public static MelonPreferences_Entry<bool> PlayerShipEnabled;

        // =========================================================================
        // SETUP
        // =========================================================================
        public static void Setup(Main modInstance)
        {
            if (!Directory.Exists(UserDataPath))
                Directory.CreateDirectory(UserDataPath);

            string configPath = Path.Combine(UserDataPath, ConfigFile);

            // ── Create categories ────────────────────────────────────────────────
            globalCat          = MelonPreferences.CreateCategory("OWR_General",        "General");
            sunCat             = MelonPreferences.CreateCategory("OWR_Sun",           "Sun");
            sunStationCat      = MelonPreferences.CreateCategory("OWR_SunStation",    "Sun Station");
            hourglassCat       = MelonPreferences.CreateCategory("OWR_Hourglass",     "Hourglass Twins");
            timberHearthCat    = MelonPreferences.CreateCategory("OWR_TimberHearth",  "Timber Hearth");
            attlerockCat       = MelonPreferences.CreateCategory("OWR_Attlerock",     "Attlerock");
            brittleHollowCat   = MelonPreferences.CreateCategory("OWR_BrittleHollow", "Brittle Hollow");
            hollowsLanternCat  = MelonPreferences.CreateCategory("OWR_HollowsLantern","Hollow's Lantern");
            giantsDeepCat      = MelonPreferences.CreateCategory("OWR_GiantsDeep",    "Giant's Deep");
            probeCannonCat     = MelonPreferences.CreateCategory("OWR_ProbeCannon",   "Probe Cannon");
            orbitalProbeCat    = MelonPreferences.CreateCategory("OWR_OrbitalProbe",  "Orbital Probe");
            quantumMoonCat     = MelonPreferences.CreateCategory("OWR_QuantumMoon",   "Quantum Moon");
            darkBrambleCat     = MelonPreferences.CreateCategory("OWR_DarkBramble",   "Dark Bramble");
            whiteHoleCat       = MelonPreferences.CreateCategory("OWR_WhiteHole",     "White Hole");
            whiteHoleStationCat= MelonPreferences.CreateCategory("OWR_WhiteHoleStation","White Hole Station");
            interloperCat      = MelonPreferences.CreateCategory("OWR_Interloper",    "Interloper");
            playerShipCat      = MelonPreferences.CreateCategory("OWR_PlayerShip",    "Player Ship");

            // Make all categories write to the same config file
            var allCategories = new[] {
                globalCat, sunCat, sunStationCat, hourglassCat, timberHearthCat,
                attlerockCat, brittleHollowCat, hollowsLanternCat, giantsDeepCat,
                probeCannonCat, orbitalProbeCat, quantumMoonCat, darkBrambleCat,
                whiteHoleCat, whiteHoleStationCat, interloperCat, playerShipCat
            };
            foreach (var cat in allCategories)
                cat.SetFilePath(configPath);

            // ── Populate entries in their categories ─────────────────────────────

            // General
            EnabledEntry = globalCat.CreateEntry(
                "Enabled", true,
                "Enable Mod", "Master toggle for the mod");

            // Sun
            SunEnabled = sunCat.CreateEntry(
                "Sun_Enabled", true,
                "Enabled", "Toggle the Sun");
            SunDoTimeLoop = sunCat.CreateEntry(
                "Sun_TimeLoop", true,
                "Do sun time loop", "Let the sun change stages and eventually go kaboom :3");
            SunSecondsToFullRed = sunCat.CreateEntry(
                "Sun_SecondsToFullRed", 60 * 22,
                "Seconds to Full Red",
                "How many seconds the sun takes to turn fully red (~22 min default)");
            SunWaitAfterRed = sunCat.CreateEntry(
                "Sun_WaitAfterRed", 92f,
                "Wait After Red (s)",
                "Seconds the sun holds at full red before collapsing");
            SunExpansionSpeed = sunCat.CreateEntry(
                "Sun_WallExpansionSpeed", 24f,
                "Wall Expansion Speed",
                "World units per second the supernova wall expands");
            SunCollapseDuration = sunCat.CreateEntry(
                "Sun_CollapseDuration", 9.5f,
                "Collapse Duration (s)",
                "How long the collapse animation takes");
            SunExplosionDuration = sunCat.CreateEntry(
                "Sun_ExplosionDuration", 3.7f,
                "Explosion Duration (s)",
                "How long the initial explosion flash takes");

            // Sun Station
            SunStationEnabled = sunStationCat.CreateEntry(
                "SunStation_Enabled", true,
                "Enabled", "Toggle the Sun Station");
            SunStationOrbitDistance = sunStationCat.CreateEntry(
                "SunStation_OrbitDistance", 23f / 30f,
                "Orbit Distance",
                "Pre-scale orbit distance from the Sun (multiplied by 30 at runtime)");
            SunStationOrbitSpeed = sunStationCat.CreateEntry(
                "SunStation_OrbitSpeed", 16f,
                "Orbit Speed", "Degrees per second around the Sun");
            SunStationSpinSpeed = sunStationCat.CreateEntry(
                "SunStation_SpinSpeed", 16f,
                "Spin Speed", "Self-rotation degrees per second");

            // Hourglass Twins
            HourGlassTwinsEnabled = hourglassCat.CreateEntry(
                "HourGlassTwins_Enabled", true,
                "Enabled", "Toggle the Hourglass Twins");
            HourGlassTwinsOrbitDistance = hourglassCat.CreateEntry(
                "HourGlassTwins_OrbitDistance", 3.88f,
                "Orbit Distance",
                "Pre-scale orbit distance from the Sun");
            HourGlassTwinsOrbitSpeed = hourglassCat.CreateEntry(
                "HourGlassTwins_OrbitSpeed", 2.27f,
                "Orbit Speed", "Degrees per second around the Sun");
            HourGlassTwinsSpinSpeed = hourglassCat.CreateEntry(
                "HourGlassTwins_SpinSpeed", 20.5f,
                "Spin Speed", "Self-rotation degrees per second");
            HourGlassTwinsTransferDuration = hourglassCat.CreateEntry(
                "HourGlassTwins_TransferDuration", 3.4f,
                "Transfer Duration (revs)",
                "Number of orbits for the sand to fully transfer");
            HourGlassTwinsWaitDuration = hourglassCat.CreateEntry(
                "HourGlassTwins_WaitDuration", 0.4f,
                "Wait Duration (revs)",
                "Number of orbits to wait between sand transfers");
            HourGlassTwinsRandomSandStage = hourglassCat.CreateEntry(
                "HourGlassTwins_RandomSandStage", true,
                "Random Start Stage",
                "Start the sand transfer at a random point in the cycle");

            // Timber Hearth
            TimberHearthEnabled = timberHearthCat.CreateEntry(
                "TimberHearth_Enabled", true,
                "Enabled", "Toggle Timber Hearth");
            TimberHearthOrbitDistance = timberHearthCat.CreateEntry(
                "TimberHearth_OrbitDistance", 5.6f,
                "Orbit Distance",
                "Pre-scale orbit distance from the Sun");
            TimberHearthOrbitSpeed = timberHearthCat.CreateEntry(
                "TimberHearth_OrbitSpeed", 1.0f,
                "Orbit Speed", "Degrees per second around the Sun");
            TimberHearthSpinSpeed = timberHearthCat.CreateEntry(
                "TimberHearth_SpinSpeed", 7.5f,
                "Spin Speed", "Self-rotation degrees per second");

            // Attlerock
            AttlerockEnabled = attlerockCat.CreateEntry(
                "Attlerock_Enabled", true,
                "Enabled", "Toggle Attlerock (Timber Hearth's moon)");
            AttlerockOrbitDistance = attlerockCat.CreateEntry(
                "Attlerock_OrbitDistance", 0.8f,
                "Orbit Distance",
                "Pre-scale orbit distance from Timber Hearth");
            AttlerockOrbitSpeed = attlerockCat.CreateEntry(
                "Attlerock_OrbitSpeed", 15f,
                "Orbit Speed", "Degrees per second around Timber Hearth");
            AttlerockSpinSpeed = attlerockCat.CreateEntry(
                "Attlerock_SpinSpeed", 15f,
                "Spin Speed", "Self-rotation degrees per second");

            // Brittle Hollow
            BrittleHollowEnabled = brittleHollowCat.CreateEntry(
                "BrittleHollow_Enabled", true,
                "Enabled", "Toggle Brittle Hollow");
            BrittleHollowOrbitDistance = brittleHollowCat.CreateEntry(
                "BrittleHollow_OrbitDistance", 7.8f,
                "Orbit Distance",
                "Pre-scale orbit distance from the Sun");
            BrittleHollowOrbitSpeed = brittleHollowCat.CreateEntry(
                "BrittleHollow_OrbitSpeed", 0.8f,
                "Orbit Speed", "Degrees per second around the Sun");
            BrittleHollowSpinSpeed = brittleHollowCat.CreateEntry(
                "BrittleHollow_SpinSpeed", 7.0f,
                "Spin Speed", "Self-rotation degrees per second");
            BrittleHollowBreakAppart = brittleHollowCat.CreateEntry(
                "BrittleHollow_BreakApart", true,
                "Break apart", "Weather chunks of brittle hollow will break apart and warp to the white hole");
            BrittleHollowBreakIntervalMin = brittleHollowCat.CreateEntry(
                "BrittleHollow_BreakIntervalMin", 28f,
                "Break Interval Min (s)",
                "Minimum seconds between chunk break events");
            BrittleHollowBreakIntervalMax = brittleHollowCat.CreateEntry(
                "BrittleHollow_BreakIntervalMax", 43f,
                "Break Interval Max (s)",
                "Maximum seconds between chunk break events");
            BrittleHollowSuckSpeed = brittleHollowCat.CreateEntry(
                "BrittleHollow_SuckSpeed", 1f,
                "Suck Speed",
                "Units per second chunks move toward the black hole");
            BrittleHollowDriftSpeed = brittleHollowCat.CreateEntry(
                "BrittleHollow_DriftSpeed", 0.09f,
                "Drift Speed",
                "Units per second chunks drift near the white hole");
            BrittleHollowDriftMaxRadius = brittleHollowCat.CreateEntry(
                "BrittleHollow_DriftMaxRadius", 9f,
                "Drift Max Radius",
                "How far chunks can drift from the white hole before being pulled back");

            // Hollow's Lantern
            HollowsLanternEnabled = hollowsLanternCat.CreateEntry(
                "HollowsLantern_Enabled", true,
                "Enabled", "Toggle Hollow's Lantern");
            HollowsLanternOrbitDistance = hollowsLanternCat.CreateEntry(
                "HollowsLantern_OrbitDistance", 0.66f,
                "Orbit Distance",
                "Pre-scale orbit distance from Brittle Hollow");
            HollowsLanternOrbitSpeed = hollowsLanternCat.CreateEntry(
                "HollowsLantern_OrbitSpeed", 20f,
                "Orbit Speed", "Degrees per second around Brittle Hollow");
            HollowsLanternSpinSpeed = hollowsLanternCat.CreateEntry(
                "HollowsLantern_SpinSpeed", 30f,
                "Spin Speed", "Self-rotation degrees per second");

            // Giant's Deep
            GiantsDeepEnabled = giantsDeepCat.CreateEntry(
                "GiantsDeep_Enabled", true,
                "Enabled", "Toggle Giant's Deep");
            GiantsDeepOrbitDistance = giantsDeepCat.CreateEntry(
                "GiantsDeep_OrbitDistance", 10.6f,
                "Orbit Distance",
                "Pre-scale orbit distance from the Sun");
            GiantsDeepOrbitSpeed = giantsDeepCat.CreateEntry(
                "GiantsDeep_OrbitSpeed", 0.6f,
                "Orbit Speed", "Degrees per second around the Sun");
            GiantsDeepSpinSpeed = giantsDeepCat.CreateEntry(
                "GiantsDeep_SpinSpeed", 0.2f,
                "Spin Speed", "Self-rotation degrees per second");

            // Orbital Probe Cannon
            OrbitalProbeCannonEnabled = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_Enabled", true,
                "Enabled", "Toggle the Orbital Probe Cannon");
            OrbitalProbeCannonOrbitDistance = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_OrbitDistance", 1.3f,
                "Orbit Distance",
                "Pre-scale orbit distance from Giant's Deep");
            OrbitalProbeCannonOrbitSpeed = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_OrbitSpeed", 10f,
                "Orbit Speed", "Degrees per second around Giant's Deep");
            OrbitalProbeCannonSpinSpeed = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_SpinSpeed", 10f,
                "Spin Speed", "Self-rotation degrees per second");
            OrbitalProbeCannonFire = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_Fire", true,
                "Fire probe", "Weather the orbital probe cannon will fire the probe (and then break)");
            OrbitalProbeCannonTimeToAim = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_TimeToAim", 10f,
                "Time to Aim (s)",
                "Seconds the cannon spends rotating to its aim direction");
            OrbitalProbeCannonExplosionTime = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_ExplosionTime", 6f,
                "Explosion Hold Time (s)",
                "Seconds the explosion light stays at full intensity");
            OrbitalProbeCannonBreakupDuration = probeCannonCat.CreateEntry(
                "OrbitalProbeCannon_BreakupDuration", 18f,
                "Breakup Duration (s)",
                "Seconds it takes the cannon to physically break apart");

            // Orbital Probe
            OrbitalProbeEnabled = orbitalProbeCat.CreateEntry(
                "OrbitalProbe_Enabled", true,
                "Enabled", "Toggle the Orbital Probe");
            OrbitalProbeSpeed = orbitalProbeCat.CreateEntry(
                "OrbitalProbe_Speed", 40f,
                "Launch Speed",
                "How fast the probe will go once fired");

            // Quantum Moon
            QuantumMoonEnabled = quantumMoonCat.CreateEntry(
                "QuantumMoon_Enabled", true,
                "Enabled", "Toggle the Quantum Moon");
            QuantumMoonOrbitDistance = quantumMoonCat.CreateEntry(
                "QuantumMoon_OrbitDistance", 1.7f,
                "Orbit Distance",
                "Pre-scale orbit distance from its current parent");
            QuantumMoonOrbitSpeed = quantumMoonCat.CreateEntry(
                "QuantumMoon_OrbitSpeed", 2f,
                "Orbit Speed", "Degrees per second around parent");
            QuantumMoonSpinSpeed = quantumMoonCat.CreateEntry(
                "QuantumMoon_SpinSpeed", 4f,
                "Spin Speed", "Self-rotation degrees per second");
            QuantumMoonPlayMusic = quantumMoonCat.CreateEntry(
                "QuantumMoon_PlayMusic", false,
                "Play Music", "Toggles the mysterious music coming from the quantum moon. Who could possibly be on it?");

            // Dark Bramble
            DarkBrambleEnabled = darkBrambleCat.CreateEntry(
                "DarkBramble_Enabled", true,
                "Enabled", "Toggle Dark Bramble");
            DarkBrambleOrbitDistance = darkBrambleCat.CreateEntry(
                "DarkBramble_OrbitDistance", 14.6f,
                "Orbit Distance",
                "Pre-scale orbit distance from the Sun");
            DarkBrambleOrbitSpeed = darkBrambleCat.CreateEntry(
                "DarkBramble_OrbitSpeed", 0.38f,
                "Orbit Speed", "Degrees per second around the Sun");

            // White Hole
            WhiteHoleEnabled = whiteHoleCat.CreateEntry(
                "WhiteHole_Enabled", true,
                "Enabled", "Toggle the White Hole");

            // White Hole Station
            WhiteHoleStationEnabled = whiteHoleStationCat.CreateEntry(
                "WhiteHoleStation_Enabled", true,
                "Enabled", "Toggle the White Hole Station");

            // Interloper
            InterloperEnabled = interloperCat.CreateEntry(
                "Interloper_Enabled", true,
                "Enabled", "Toggle the Interloper");
            InterloperSemiMinorAxis = interloperCat.CreateEntry(
                "Interloper_SemiMinorAxis", 5.66f,
                "Semi-Minor Axis",
                "Pre-scale width of the elliptical orbit");
            InterloperOrbitSpeed = interloperCat.CreateEntry(
                "Interloper_OrbitSpeed", 11f,
                "Orbit Speed",
                "Base angular speed along the ellipse");
            InterloperSpeedIntensity = interloperCat.CreateEntry(
                "Interloper_SpeedIntensity", 1.1f,
                "Speed Intensity",
                "Higher values = faster near perihelion");

            // Player Ship
            PlayerShipEnabled = playerShipCat.CreateEntry(
                "PlayerShip_Enabled", true,
                "Enabled", "Toggle the Hearthian spaceship model");

            // ── Register all categories with UIFramework ─────────────────────────
            var uiHandle = UI.Register((MelonBase)modInstance,
                globalCat, sunCat, sunStationCat, hourglassCat,
                timberHearthCat, attlerockCat, brittleHollowCat,
                hollowsLanternCat, giantsDeepCat, probeCannonCat,
                orbitalProbeCat, quantumMoonCat, darkBrambleCat,
                whiteHoleCat, whiteHoleStationCat, interloperCat,
                playerShipCat);

            uiHandle.OnModSaved += OnSettingsSaved;
        }

        // =========================================================================
        // SAVE CALLBACK
        // =========================================================================
        private static void OnSettingsSaved()
        {
            Melon<Main>.Logger.Msg("[OwSystemSettings] Settings saved — applying to live solar system.");
            ApplyToSolarSystem();
        }

        // =========================================================================
        // APPLY (unchanged — accesses the same static entry fields)
        // =========================================================================
        public static void ApplyToSolarSystem()
        {
            var sys = Main.solarSystem;

            if (sys.Sun != null)
            {
                sys.Sun.SetActive(SunEnabled.Value);
                var sn = sys.Sun.GetComponent<SupernovaSun>();
                if (sn != null)
                {
                    sn.secondsToFullRed              = SunSecondsToFullRed.Value;
                    sn.waitAfterRed                  = SunWaitAfterRed.Value;
                    sn.expansionSpeedWorldUnitsPerSec = SunExpansionSpeed.Value;
                    sn.collapseDuration              = SunCollapseDuration.Value;
                    sn.explosionDuration             = SunExplosionDuration.Value;
                    sn.DoTimeLoop                    = SunDoTimeLoop.Value;
                }
            }

            if (sys.SunStation != null)
            {
                sys.SunStation.SetActive(SunStationEnabled.Value);
                var orb = sys.SunStation.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = SunStationOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = SunStationOrbitSpeed.Value;
                    orb.spinSpeed     = SunStationSpinSpeed.Value;
                }
            }

            if (sys.HourGlassTwins != null)
            {
                sys.HourGlassTwins.SetActive(HourGlassTwinsEnabled.Value);
                var orb = sys.HourGlassTwins.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = HourGlassTwinsOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = HourGlassTwinsOrbitSpeed.Value;
                    orb.spinSpeed     = HourGlassTwinsSpinSpeed.Value;
                }
                var hgt = sys.HourGlassTwins.GetComponent<HourGlassTwins>();
                if (hgt != null)
                {
                    hgt.transferDurationRevs = HourGlassTwinsTransferDuration.Value;
                    hgt.waitDurationRevs     = HourGlassTwinsWaitDuration.Value;
                    hgt.randomSandStage      = HourGlassTwinsRandomSandStage.Value;
                }
            }

            if (sys.TimberHearth != null)
            {
                sys.TimberHearth.SetActive(TimberHearthEnabled.Value);
                var orb = sys.TimberHearth.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = TimberHearthOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = TimberHearthOrbitSpeed.Value;
                    orb.spinSpeed     = TimberHearthSpinSpeed.Value;
                }
            }

            if (sys.Attlerock != null)
            {
                sys.Attlerock.SetActive(AttlerockEnabled.Value);
                var orb = sys.Attlerock.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = AttlerockOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = AttlerockOrbitSpeed.Value;
                    orb.spinSpeed     = AttlerockSpinSpeed.Value;
                }
            }

            if (sys.BrittleHollow != null)
            {
                sys.BrittleHollow.SetActive(BrittleHollowEnabled.Value);
                var orb = sys.BrittleHollow.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = BrittleHollowOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = BrittleHollowOrbitSpeed.Value;
                    orb.spinSpeed     = BrittleHollowSpinSpeed.Value;
                }
                var bh = sys.BrittleHollow.GetComponent<BrittleHollow>();
                if (bh != null)
                {
                    bh.breakApart       = BrittleHollowBreakAppart.Value;
                    bh.breakIntervalMin = BrittleHollowBreakIntervalMin.Value;
                    bh.breakIntervalMax = BrittleHollowBreakIntervalMax.Value;
                    bh.suckSpeed        = BrittleHollowSuckSpeed.Value;
                    bh.driftSpeed       = BrittleHollowDriftSpeed.Value;
                    bh.driftMaxRadius   = BrittleHollowDriftMaxRadius.Value;
                }
            }

            if (sys.HollowsLantern != null)
            {
                sys.HollowsLantern.SetActive(HollowsLanternEnabled.Value);
                var orb = sys.HollowsLantern.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = HollowsLanternOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = HollowsLanternOrbitSpeed.Value;
                    orb.spinSpeed     = HollowsLanternSpinSpeed.Value;
                }
            }

            if (sys.GiantsDeep != null)
            {
                sys.GiantsDeep.SetActive(GiantsDeepEnabled.Value);
                var orb = sys.GiantsDeep.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = GiantsDeepOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = GiantsDeepOrbitSpeed.Value;
                    orb.spinSpeed     = GiantsDeepSpinSpeed.Value;
                }
            }

            if (sys.OrbitalProbeCannon != null)
            {
                sys.OrbitalProbeCannon.SetActive(OrbitalProbeCannonEnabled.Value);
                var orb = sys.OrbitalProbeCannon.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = OrbitalProbeCannonOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = OrbitalProbeCannonOrbitSpeed.Value;
                    orb.spinSpeed     = OrbitalProbeCannonSpinSpeed.Value;
                }
                var cannon = sys.OrbitalProbeCannon.GetComponent<OrbitalProbeCannon>();
                if (cannon != null)
                {
                    cannon.fireProbe       = OrbitalProbeCannonFire.Value;
                    cannon.timeToAim       = OrbitalProbeCannonTimeToAim.Value;
                    cannon.explosionTime   = OrbitalProbeCannonExplosionTime.Value;
                    cannon.breakupDuration = OrbitalProbeCannonBreakupDuration.Value;
                }
            }

            if (sys.OrbitalProbe != null)
            {
                sys.OrbitalProbe.SetActive(OrbitalProbeEnabled.Value);
                var probe = sys.OrbitalProbe.GetComponent<OrbitalProbe>();
                if (probe != null)
                    probe.probeSpeed = OrbitalProbeSpeed.Value;
            }

            if (sys.QuantumMoon != null)
            {
                sys.QuantumMoon.SetActive(QuantumMoonEnabled.Value);
                var orb = sys.QuantumMoon.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = QuantumMoonOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = QuantumMoonOrbitSpeed.Value;
                    orb.spinSpeed     = QuantumMoonSpinSpeed.Value;
                }
                var mus = sys.QuantumMoon.GetComponent<MusicEmitter>();
                if (mus != null)
                {
                    mus.isEnabled = false;
                }
            }

            if (sys.DarkBramble != null)
            {
                sys.DarkBramble.SetActive(DarkBrambleEnabled.Value);
                var orb = sys.DarkBramble.GetComponent<Orbiter>();
                if (orb != null)
                {
                    orb.orbitDistance = DarkBrambleOrbitDistance.Value * SolarSystemScale;
                    orb.orbitSpeed    = DarkBrambleOrbitSpeed.Value;
                }
            }

            if (sys.WhiteHole != null)
                sys.WhiteHole.SetActive(WhiteHoleEnabled.Value);

            if (sys.WhiteHoleStation != null)
                sys.WhiteHoleStation.SetActive(WhiteHoleStationEnabled.Value);

            if (sys.Interloper != null)
            {
                sys.Interloper.SetActive(InterloperEnabled.Value);
                var ell = sys.Interloper.GetComponent<EllipticalOrbiter>();
                if (ell != null)
                {
                    ell.semiMinorAxis  = InterloperSemiMinorAxis.Value * SolarSystemScale;
                    ell.orbitSpeed     = InterloperOrbitSpeed.Value;
                    ell.speedIntensity = InterloperSpeedIntensity.Value;
                }
            }

            if (sys.PlayerShip != null)
                sys.PlayerShip.SetActive(PlayerShipEnabled.Value);
        }
    }
}