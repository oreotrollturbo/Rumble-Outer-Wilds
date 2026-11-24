using System.Collections.Generic;
using MelonLoader;
using OuterWildsRumble.Components;
using OuterWildsRumble.UIFrameworkSettings;
using UnityEngine;

namespace OuterWildsRumble;

public static class SolarSystemScaler
{
    // Base values I tested
    private const double BaseInterloperSwallowDist = 3.2978 / 30;
    private const double BaseSupernovaExtraDist = 25 / 30d;
    private const float BaseSpitSpeed = 0.1f / 30f;
    private const float BaseSpitYRange = 0.9f / 30f;
    private const float BaseSuckStopDistance = 0.05f / 30f;
    
    public static void Apply(float scale)
    {
        var sys = Main.solarSystem;
        if (sys.Root == null) return;
        
        sys.Root.transform.localScale = Vector3.one * scale;

        //Orbiter distances
        SetOrbiterDist(sys.SunStation, OwSystemSettings.SunStationOrbitDistance.Value, scale);
        SetOrbiterDist(sys.HourGlassTwins, OwSystemSettings.HourGlassTwinsOrbitDistance.Value, scale);
        SetOrbiterDist(sys.TimberHearth, OwSystemSettings.TimberHearthOrbitDistance.Value, scale);
        SetOrbiterDist(sys.Attlerock, OwSystemSettings.AttlerockOrbitDistance.Value, scale);
        SetOrbiterDist(sys.BrittleHollow, OwSystemSettings.BrittleHollowOrbitDistance.Value, scale);
        SetOrbiterDist(sys.HollowsLantern, OwSystemSettings.HollowsLanternOrbitDistance.Value, scale);
        SetOrbiterDist(sys.GiantsDeep, OwSystemSettings.GiantsDeepOrbitDistance.Value, scale);
        SetOrbiterDist(sys.OrbitalProbeCannon, OwSystemSettings.OrbitalProbeCannonOrbitDistance.Value, scale);
        SetOrbiterDist(sys.QuantumMoon, OwSystemSettings.QuantumMoonOrbitDistance.Value, scale);
        SetOrbiterDist(sys.DarkBramble, OwSystemSettings.DarkBrambleOrbitDistance.Value, scale);
        SetOrbiterDist(sys.HearthianMapSatelite, OwSystemSettings.SatelliteOrbitDistance.Value, scale);

        // Interlopeur
        var ell = sys.Interloper?.GetComponent<EllipticalOrbiter>();
        if (ell != null)
            ell.semiMinorAxis = OwSystemSettings.InterloperSemiMinorAxis.Value * scale;

        // Quoom
        var qo = sys.QuantumMoon?.GetComponent<QuantumOrbiter>();
        if (qo != null && sys.HourGlassTwins != null)
        {
            qo.orbitParents[sys.HourGlassTwins.transform.GetChild(2)] = 0.59f * scale;
            qo.orbitParents[sys.HourGlassTwins.transform.GetChild(0)] = 0.59f * scale;
            qo.orbitParents[sys.TimberHearth.transform] = 0.96f * scale;
            qo.orbitParents[sys.BrittleHollow.transform] = 0.87f * scale;
            qo.orbitParents[sys.GiantsDeep.transform] = 1.70f * scale;
            qo.orbitParents[sys.DarkBramble.transform] = 1.70f * scale;
        }
        
        //Sun stuff
        var sn = sys.Sun?.GetComponent<SupernovaSun>();
        if (sn != null)
        {
            sn.interloperSwallowDistance = BaseInterloperSwallowDist * scale;
            sn.extraDistance = (float)BaseSupernovaExtraDist * scale;
        }

        //Brittle hollow
        var bh = sys.BrittleHollow?.GetComponent<BrittleHollow>();
        if (bh != null)
        {
            bh.suckSpeed *= scale;
            bh.driftSpeed *= scale;
            bh.spitSpeed = BaseSpitSpeed * scale;
            bh.spitYRange = BaseSpitYRange * scale;
            bh.suckStopDistance = BaseSuckStopDistance * scale;
        }
        
        var hl = sys.HollowsLantern?.GetComponent<HollowsLantern>();
        if (hl != null)
        {
            hl.meteorDestroyRadius *= scale;
        }
    }
    
    private static void SetOrbiterDist(GameObject go, float baseDist, float scale)
    {
        var orb = go?.GetComponent<Orbiter>();
        if (orb != null)
            orb.orbitDistance = baseDist * scale;
    }
}