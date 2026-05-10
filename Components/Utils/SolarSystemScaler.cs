using System.Collections.Generic;
using OuterWildsRumble.Components;
using OuterWildsRumble.UIFrameworkSettings;
using UnityEngine;

namespace OuterWildsRumble
{
    /// <summary>
    /// Single source of truth for every scale-dependent value in the solar system.
    ///
    /// Rules:
    ///   - Call Apply() exactly once at the end of ApplyToSolarSystem().
    ///   - All assignments use  =  (direct), never  *=  (compound).
    ///     This means Apply() is fully idempotent – safe to call any number of times.
    ///   - ApplyToSolarSystem() must NOT multiply orbit distances by scale itself;
    ///     that job belongs here and only here.
    /// </summary>
    public static class SolarSystemScaler
    {
        // ── Base values for fields that are NOT exposed in settings ───────────
        // These must match the default field values in their component classes.
        // If a component default changes, update the constant here too.
        private const float BaseMusicDetectionAngle   = 8/30f;
        private const double BaseInterloperSwallowDist = 3.2978 / 30;
        private const double BaseSupernovaExtraDist    = 25/30d;
        private const float BaseSpitSpeed             = 0.1f /30f;
        private const float BaseSpitYRange            = 0.9f /30f;
        private const float BaseSuckStopDistance      = 0.05f/30f;

        // ── Quantum moon orbit distances per possible parent (base, pre-scale) ─
        // Mirrors the dictionary built in Main.SetupQuantumMoon().
        private static readonly (float childIndex, float dist)[] AshTwinChildren =
        {
            (2, 0.59f),   // AshTwin  child index 2
            (0, 0.59f),   // CaveTwin child index 0
        };

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Apply all scale-dependent values from base * scale.</summary>
        public static void Apply(float scale)
        {
            var sys = Main.solarSystem;
            if (sys.Root == null) return;

            // Root transform – direct set, never compounds
            sys.Root.transform.localScale = Vector3.one * scale;

            // ── Orbiter distances ─────────────────────────────────────────────
            SetOrbiterDist(sys.SunStation,        OwSystemSettings.SunStationOrbitDistance.Value,         scale);
            SetOrbiterDist(sys.HourGlassTwins,    OwSystemSettings.HourGlassTwinsOrbitDistance.Value,     scale);
            SetOrbiterDist(sys.TimberHearth,       OwSystemSettings.TimberHearthOrbitDistance.Value,       scale);
            SetOrbiterDist(sys.Attlerock,          OwSystemSettings.AttlerockOrbitDistance.Value,          scale);
            SetOrbiterDist(sys.BrittleHollow,      OwSystemSettings.BrittleHollowOrbitDistance.Value,     scale);
            SetOrbiterDist(sys.HollowsLantern,     OwSystemSettings.HollowsLanternOrbitDistance.Value,    scale);
            SetOrbiterDist(sys.GiantsDeep,         OwSystemSettings.GiantsDeepOrbitDistance.Value,        scale);
            SetOrbiterDist(sys.OrbitalProbeCannon, OwSystemSettings.OrbitalProbeCannonOrbitDistance.Value, scale);
            SetOrbiterDist(sys.QuantumMoon,        OwSystemSettings.QuantumMoonOrbitDistance.Value,       scale);
            SetOrbiterDist(sys.DarkBramble,        OwSystemSettings.DarkBrambleOrbitDistance.Value,       scale);

            // ── Elliptical orbiter (Interloper) ───────────────────────────────
            var ell = sys.Interloper?.GetComponent<EllipticalOrbiter>();
            if (ell != null)
                ell.semiMinorAxis = OwSystemSettings.InterloperSemiMinorAxis.Value * scale;

            // ── Quantum moon per-parent distances ─────────────────────────────
            var qo = sys.QuantumMoon?.GetComponent<QuantumOrbiter>();
            if (qo != null && sys.HourGlassTwins != null)
            {
                qo.orbitParents[sys.HourGlassTwins.transform.GetChild(2)] = 0.59f * scale;
                qo.orbitParents[sys.HourGlassTwins.transform.GetChild(0)] = 0.59f * scale;
                qo.orbitParents[sys.TimberHearth.transform]               = 0.96f * scale;
                qo.orbitParents[sys.BrittleHollow.transform]              = 0.87f * scale;
                qo.orbitParents[sys.GiantsDeep.transform]                 = 1.70f * scale;
                qo.orbitParents[sys.DarkBramble.transform]                = 1.70f * scale;
            }

            // ── Supernova distances ───────────────────────────────────────────
            var sn = sys.Sun?.GetComponent<SupernovaSun>();
            if (sn != null)
            {
                sn.interloperSwallowDistance = BaseInterloperSwallowDist * scale;
                sn.extraDistance             = BaseSupernovaExtraDist    * scale;
            }

            // ── Brittle Hollow physics ────────────────────────────────────────
            var bh = sys.BrittleHollow?.GetComponent<BrittleHollow>();
            if (bh != null)
            {
                // Settings-backed fields
                bh.suckSpeed      = OwSystemSettings.BrittleHollowSuckSpeed.Value      * scale;
                bh.driftSpeed     = OwSystemSettings.BrittleHollowDriftSpeed.Value     * scale;
                // Component-default fields
                bh.spitSpeed        = BaseSpitSpeed        * scale;
                bh.spitYRange       = BaseSpitYRange       * scale;
                bh.suckStopDistance = BaseSuckStopDistance * scale;
            }

            // ── Orbital probe speed ───────────────────────────────────────────
            var probe = sys.OrbitalProbe?.GetComponent<OrbitalProbe>();
            if (probe != null)
                probe.probeSpeed = OwSystemSettings.OrbitalProbeSpeed.Value * scale;

            // ── Music emitter detection radius ────────────────────────────────
            foreach (var mus in sys.Root.GetComponentsInChildren<MusicEmitter>())
                mus.detectionAngle = BaseMusicDetectionAngle * scale;
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static void SetOrbiterDist(GameObject go, float baseDist, float scale)
        {
            var orb = go?.GetComponent<Orbiter>();
            if (orb != null)
                orb.orbitDistance = baseDist * scale;
        }
    }
}
