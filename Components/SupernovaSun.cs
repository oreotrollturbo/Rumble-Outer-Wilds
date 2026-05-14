using System.Collections;
using System.Collections.Generic;
using System.IO;
using AudioSchtuff;
using MelonLoader;
using OuterWildsRumble.Components.SupernovaUtils;
using OuterWildsRumble.UIFrameworkSettings;
using RumbleModdingAPI.RMAPI;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SupernovaSun : MonoBehaviour
{
    // ── Sound names ───────────────────────────────────────────────────────────
    private const string endTimesSoundName           = "OW_EndTimes.wav";
    private const string supernovaCollapseSoundName  = "Sun_supernova_collapse.wav";
    private const string supernovaExplosionSoundName = "Sun_supernova_explosion.wav";
    private const string supernovaWallSoundName      = "Sun_supernova_wall.wav";

    // ── Settings ──────────────────────────────────────────────────────────────
    public bool DoTimeLoop = true;

    private float wallRampRange = 0f;
    private AudioManager.ClipData wallClip;

    public double extraDistance          = 25f;
    private float originalLightIntensity;

    // ── Player & expansion ────────────────────────────────────────────────────
    public Transform playerTransform;
    private bool hasReachedPlayer = false;
    private bool isFadingOut      = false;
    public float expansionSpeedWorldUnitsPerSec = 24f;

    // ── Required target ───────────────────────────────────────────────────────
    private struct RequiredTarget
    {
        public Transform transform;
        public float     radius;
        public bool      engulfed;
    }
    private RequiredTarget requiredTarget;

    // ── Timing & scale ────────────────────────────────────────────────────────
    public int   secondsToFullRed           = 60 * 22;
    public float waitAfterRed               = 60 + 32f;
    public float collapseDuration           = 9.5f;
    public float explosionDuration          = 3.7f;
    public Vector3 collapseScale            = new(0.08f, 0.08f, 0.08f);
    public Vector3 explosionTargetScale     = new(2.5f,  2.5f,  2.5f);
    public Vector3 redGrowthScale           = new(0.05f, 0.05f, 0.05f);
    public double interloperSwallowDistance  = 3.2978 / 30;

    // ── Light & colour ────────────────────────────────────────────────────────
    public Light sunLight;
    public Color sunlightOriginal;
    public Color sunlightRed   = new(0.73f, 0.116f, 0);
    public Color sunlightWhite = Color.white;
    public Color sunlightBlue  = Color.cyan;

    // ── Phase state ───────────────────────────────────────────────────────────
    public enum Phase { Red, RedFullWait, Collapse, Explosion, Wall, Done }
    public Phase currentPhase = Phase.Red;
    private float phaseTimer  = 0f;
    
    private GameObject opaqueSunGO;
    private GameObject transparentSunGO;

    private Renderer opaqueSunRenderer;
    private Renderer transSunRenderer;

    private Material opaqueSunMaterial;
    private Material opaqueHaloMaterial;
    private Material transSunMaterial;
    private Material transHaloMaterial;

    // Active convenience pointers — redirected from opaque → transparent at
    // the start of the Explosion phase.  All phase-update methods use these.
    private Material sunMaterial;
    private Material haloMaterial;

    // ── Shader setting snapshots ──────────────────────────────────────────────
    private SunShaderUtils.SunCoreSettings startCore, redCore, whiteCore, superCore;
    private SunShaderUtils.SunHaloSettings startHalo, redHalo, whiteHalo, superHalo;

    private Vector3 initialScale;

    // ── Swallowing ────────────────────────────────────────────────────────────
    private struct BodyToSwallow { public Transform transform; public float radius; }
    private List<BodyToSwallow> bodiesToSwallow = new();

    private float sunBaseRadius;
    private float sunRadiusPerUnitScale;

    // The corona object that shrinks away during Collapse (child 1 of opaque sun).
    private Transform opaqueCollapseObject;

    // ─────────────────────────────────────────────────────────────────────────

    public SupernovaSun(IntPtr ptr) : base(ptr) { }

    // ── Public API ────────────────────────────────────────────────────────────
    public void SetBodiesToSwallow(List<Transform> transforms)
    {
        bodiesToSwallow.Clear();
        foreach (Transform t in transforms)
        {
            if (t == null) continue;
            bodiesToSwallow.Add(new BodyToSwallow { transform = t, radius = CalculateWorldRadius(t) });
        }
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Start()
    {
        // Locate both sun GameObjects from the root.
        transparentSunGO = transform.GetChild(0).gameObject;  // hidden until Explosion
        opaqueSunGO      = transform.GetChild(1).gameObject;  // visible during Red…Collapse

        // Main renderers.
        opaqueSunRenderer = opaqueSunGO.GetComponent<Renderer>();
        transSunRenderer  = transparentSunGO.GetComponent<Renderer>();

        if (opaqueSunRenderer == null || transSunRenderer == null) return;

        opaqueSunMaterial = opaqueSunRenderer.material;
        transSunMaterial  = transSunRenderer.material;

        // Halo: GetComponentsInChildren returns renderers depth-first, so [0] is always the
        // sun body itself and [1] is the halo — regardless of how deep the halo sits.
        // includeInactive:true ensures the transparent sun (currently hidden) is fully queried.
        Renderer[] opaqueRenderers = opaqueSunGO.GetComponentsInChildren<Renderer>(true);
        Renderer[] transRenderers  = transparentSunGO.GetComponentsInChildren<Renderer>(true);

        opaqueHaloMaterial = opaqueRenderers.Length > 1 ? opaqueRenderers[1].material : null;
        transHaloMaterial  = transRenderers.Length  > 1 ? transRenderers[1].material  : null;

        MelonLogger.Msg($"[SupernovaSun] opaque renderers={opaqueRenderers.Length}  trans renderers={transRenderers.Length}");
        MelonLogger.Msg($"[SupernovaSun] opaqueHaloMaterial={opaqueHaloMaterial != null}  transHaloMaterial={transHaloMaterial != null}");

        // Corona object (the ring that shrinks during Collapse): try named lookup first,
        // fall back to child index 1 of the opaque sun.
        opaqueCollapseObject = opaqueSunGO.transform.Find("Corona");
        if (opaqueCollapseObject == null && opaqueSunGO.transform.childCount > 1)
            opaqueCollapseObject = opaqueSunGO.transform.GetChild(1);

        // Boot state: opaque visible, transparent hidden.
        opaqueSunGO.SetActive(true);
        transparentSunGO.SetActive(false);

        // Active refs start on the opaque materials.
        sunMaterial  = opaqueSunMaterial;
        haloMaterial = opaqueHaloMaterial;

        initialScale = transform.localScale;

        if (sunLight != null)
        {
            sunlightOriginal     = sunLight.color;
            originalLightIntensity = sunLight.intensity;
        }

        // Compute radius from the opaque sun's renderer (it's visible at Start).
        sunBaseRadius = Mathf.Max(
            opaqueSunRenderer.bounds.extents.x,
            opaqueSunRenderer.bounds.extents.y,
            opaqueSunRenderer.bounds.extents.z);
        sunRadiusPerUnitScale = sunBaseRadius / initialScale.x;

        // Required engulf target.
        requiredTarget = new RequiredTarget
        {
            transform = Main.solarSystem.WhiteHole.transform,
            radius    = CalculateWorldRadius(Main.solarSystem.WhiteHole.transform),
            engulfed  = false,
        };

        MelonCoroutines.Start(FindPlayerAndSetup());
        MelonCoroutines.Start(InitializeAfterFrame());
    }

    public IEnumerator FindPlayerAndSetup()
    {
        while (Calls.Players.GetLocalPlayer() == null ||
               Calls.Players.GetLocalPlayer().Controller == null ||
               Calls.Players.GetLocalPlayer().Controller.PlayerVisuals == null)
        {
            if (this == null) yield break;
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1f);
        if (this == null) yield break;
        playerTransform = Calls.Players.GetLocalPlayer().Controller.PlayerVisuals.transform.GetChild(1);
    }

    // ── Fixed update ──────────────────────────────────────────────────────────
    void FixedUpdate()
    {
        if (DoTimeLoop)
        {
            if ((OwSystemSettings.SunDoTimeLoopInMatches.Value && !Main.isInMatch) ||
                !OwSystemSettings.SunDoTimeLoopInMatches.Value)
            {
                phaseTimer += Time.deltaTime;
            }
        }
        else
        {
            phaseTimer = 0f;
            if (OwSystemSettings.SunStayRed.Value)
            {
                currentPhase = Phase.RedFullWait;
                transparentSunGO.SetActive(false);
            }
                
        }

        switch (currentPhase)
        {
            case Phase.Red:         UpdateRedPhase();         break;
            case Phase.RedFullWait: UpdateRedFullWaitPhase(); break;
            case Phase.Collapse:    UpdateCollapsePhase();    break;
            case Phase.Explosion:   UpdateExplosionPhase();   break;
            case Phase.Wall:        UpdateWallPhase();        break;
        }
    }

    // ── Phase updates ─────────────────────────────────────────────────────────
    private void UpdateRedPhase()
    {
        float t      = Mathf.Clamp01(phaseTimer / secondsToFullRed);
        float sizeT  = Mathf.SmoothStep(0f, 1f, t);
        float colorT = Mathf.Pow(t, 3f);  // ease-in: stays orange longer

        transform.localScale = Vector3.Lerp(initialScale, initialScale + redGrowthScale, sizeT);
        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(startCore, redCore, colorT));
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(startHalo, redHalo, colorT));
        sunLight.color = Color.Lerp(sunlightOriginal, sunlightRed, colorT);

        if (t >= 1f) OnRedFull();
    }

    private void UpdateRedFullWaitPhase()
    {
        SunShaderUtils.ApplyCore(sunMaterial, redCore);
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, redHalo);
        sunLight.color       = sunlightRed;
        transform.localScale = initialScale + redGrowthScale;

        GameObject interloper = Main.solarSystem.Interloper;
        if (Vector3.Distance(interloper.transform.position, transform.position) < interloperSwallowDistance)
            interloper.SetActive(false);
    }

    private void UpdateCollapsePhase()
    {
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseTimer / collapseDuration));
        transform.localScale = Vector3.Lerp(initialScale + redGrowthScale, collapseScale, t);
        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(redCore, whiteCore, t));
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(redHalo, whiteHalo, t));
        sunLight.color = Color.Lerp(sunlightRed, sunlightWhite, t);
    }

    private void UpdateExplosionPhase()
    {
        float t = Mathf.Clamp01(phaseTimer / explosionDuration);
        transform.localScale = Vector3.Lerp(collapseScale, explosionTargetScale, t);
        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(whiteCore, superCore, t));
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(whiteHalo, superHalo, t));
        sunLight.color = Color.Lerp(sunlightWhite, sunlightBlue, t);
        UpdateWallClipVolume();
    }

    private void UpdateWallPhase()
    {
        SunShaderUtils.ApplyCore(sunMaterial, superCore);
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, superHalo);
        sunLight.color = sunlightBlue;

        float currentRadius = sunRadiusPerUnitScale * transform.localScale.x;
        Vector3 sunPos      = transform.position;

        // Swallow bodies.
        for (int i = bodiesToSwallow.Count - 1; i >= 0; i--)
        {
            BodyToSwallow body = bodiesToSwallow[i];
            Transform     tr   = body.transform;
            if (!tr.gameObject.activeSelf) continue;
            if (currentRadius >= Vector3.Distance(tr.position, sunPos) + body.radius + extraDistance)
            {
                tr.gameObject.SetActive(false);
                Main.solarSystem.SignalScope.GetComponent<SignalScope>().StopMusicEmitter(tr.gameObject);
            }
        }

        if (isFadingOut) return;

        double farthestRequired = 0d;

        if (requiredTarget.engulfed || !requiredTarget.transform.gameObject.activeSelf)
        {
            requiredTarget.engulfed = true;
        }
        else
        {
            float dist   = Vector3.Distance(sunPos, requiredTarget.transform.position);
            double needed = dist + requiredTarget.radius + extraDistance;
            if (currentRadius >= needed)
                requiredTarget.engulfed = true;
            else
                farthestRequired = needed;
        }

        float distToPlayer = Vector3.Distance(sunPos, playerTransform.position);
        if (!hasReachedPlayer)
        {
            if (currentRadius >= distToPlayer + extraDistance)
                hasReachedPlayer = true;
            else
                farthestRequired = Mathf.Max((float)farthestRequired, distToPlayer + (float)extraDistance);
        }
        

        UpdateWallClipVolume();

        if (requiredTarget.engulfed)
        {
            if (Main.solarSystem.DarkBramble.activeSelf)
                Main.solarSystem.DarkBramble.SetActive(false);
            MelonCoroutines.Start(FadeOutAndDisable());
        }
        else
        {
            float newWorldRadius = Mathf.Min(
                currentRadius + expansionSpeedWorldUnitsPerSec * Time.deltaTime,
                (float)farthestRequired);
            float newScaleX = newWorldRadius / sunRadiusPerUnitScale;
            transform.localScale = new Vector3(newScaleX, newScaleX, newScaleX);
        }
    }

    // ── Volume helper ─────────────────────────────────────────────────────────
    private void UpdateWallClipVolume()
    {
        if (wallClip == null) return;

        float currentRadius   = sunRadiusPerUnitScale * transform.localScale.x;
        float distToPlayer    = Vector3.Distance(transform.position, playerTransform.position);
        float surfaceDistance = distToPlayer - currentRadius;

        float volume = surfaceDistance <= 0f
            ? 1f
            : Mathf.Lerp(OwSystemSettings.SunSupernovaWallVolume.Value, 1f,
                         1f - Mathf.Clamp01(surfaceDistance / wallRampRange));

        wallClip.Reader.Volume = volume;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────
    private IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        // Read shader defaults from the opaque sun — it is always available.
        if (opaqueSunMaterial != null)
        {
            startCore = SunShaderUtils.ReadCore(opaqueSunMaterial);

            redCore           = startCore;
            redCore.Color1    = new Color(0.65f, 0.189f, 0);
            redCore.Color2    = new Color(0.7f,  0.156f, 0f);
            redCore.SunBright = 1.8f;

            whiteCore           = startCore;
            whiteCore.Color1    = Color.white;
            whiteCore.Color2    = Color.white;
            whiteCore.Color3    = Color.white;
            whiteCore.Color4    = Color.white;
            whiteCore.SunBright = 2.5f;

            superCore           = startCore;
            superCore.SunBright = 1.8f;
            superCore.Color1    = Color.cornflowerBlue;
            superCore.Color2    = Color.white;
            superCore.Color3    = Color.mediumBlue;
            superCore.Color4    = Color.deepSkyBlue;

            // Pre-load start state into the transparent sun so it's ready when we swap.
            SunShaderUtils.ApplyCore(transSunMaterial, startCore);
        }

        if (opaqueHaloMaterial != null)
        {
            startHalo = SunShaderUtils.ReadHalo(opaqueHaloMaterial);

            redHalo                  = startHalo;
            redHalo.HaloRing1Color   = new Color(0.8f,   0.2f,   0.01f);
            redHalo.HaloRing2Color   = new Color(0.749f, 0.112f, 0);

            whiteHalo                  = startHalo;
            whiteHalo.HaloRing1Color   = Color.white;
            whiteHalo.HaloRing2Color   = Color.white;

            superHalo                  = startHalo;
            superHalo.HaloRing1Color   = Color.darkCyan;
            superHalo.HaloRing2Color   = Color.cyan;

            // Pre-load start state into the transparent sun's halo.
            if (transHaloMaterial != null) SunShaderUtils.ApplyHalo(transHaloMaterial, startHalo);
        }
    }

    private void OnRedFull()
    {
        if (currentPhase != Phase.Red) return;
        Main.solarSystem.SunStation.gameObject.SetActive(false);
        AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, endTimesSoundName),
            OwSystemSettings.SunEndTimesMusicVolume.Value);
        currentPhase = Phase.RedFullWait;
        phaseTimer   = 0f;
        MelonCoroutines.Start(HandlePostRedSequence());
    }

    private IEnumerator HandlePostRedSequence()
    {
        yield return new WaitForSeconds(waitAfterRed);

        // ── Collapse — opaque sun shrinks its corona ──────────────────────────
        currentPhase = Phase.Collapse;
        phaseTimer   = 0f;
        AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, supernovaCollapseSoundName),
            OwSystemSettings.SunCollapseVolume.Value);

        if (opaqueCollapseObject != null)
        {
            Vector3 originalCollapseScale = opaqueCollapseObject.localScale;
            float elapsed = 0f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                opaqueCollapseObject.localScale =
                    Vector3.Lerp(originalCollapseScale, Vector3.zero, elapsed / collapseDuration);
                yield return null;
            }
            opaqueCollapseObject.gameObject.SetActive(false);
            opaqueCollapseObject.localScale = originalCollapseScale; // preserve for reset
        }
        else
        {
            yield return new WaitForSeconds(collapseDuration);
        }

        yield return new WaitForSeconds(0.2f);

        // ── Swap to transparent sun ───────────────────────────────────────────
        // Seed the transparent sun with the final white-collapse state so there
        // is no visual pop on the first Explosion frame.
        SunShaderUtils.ApplyCore(transSunMaterial, whiteCore);
        if (transHaloMaterial != null) SunShaderUtils.ApplyHalo(transHaloMaterial, whiteHalo);
        transSunMaterial.SetFloat("_Alpha", 1f);
        if (transHaloMaterial != null) transHaloMaterial.SetFloat("_Alpha", 1f);

        opaqueSunGO.SetActive(false);
        transparentSunGO.SetActive(true);

        // Redirect active material refs to the transparent sun.
        sunMaterial  = transSunMaterial;
        haloMaterial = transHaloMaterial;   // null is safe — all ApplyHalo calls are guarded

        // ── Explosion ─────────────────────────────────────────────────────────
        currentPhase = Phase.Explosion;
        phaseTimer   = 0f;
        AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, supernovaExplosionSoundName),
            OwSystemSettings.SunExplodeVolume.Value, false);
        wallClip = AudioManager.PlaySoundIfFileExists(
            Path.Combine(Main.folderPath, supernovaWallSoundName),
            OwSystemSettings.SunSupernovaWallVolume.Value, true);

        yield return new WaitForSeconds(explosionDuration);

        // Force the final explosion state to avoid any flash at the Wall boundary.
        SunShaderUtils.ApplyCore(sunMaterial, superCore);
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, superHalo);
        sunLight.color       = sunlightBlue;
        transform.localScale = explosionTargetScale;

        // ── Wall ──────────────────────────────────────────────────────────────
        currentPhase = Phase.Wall;
        phaseTimer   = 0f;
        float currentRadius = sunRadiusPerUnitScale * transform.localScale.x;
        wallRampRange = Mathf.Max(
            Vector3.Distance(transform.position, playerTransform.position) - currentRadius, 1f);
    }

    private IEnumerator FadeOutAndDisable()
    {
        isFadingOut = true;

        float fadeDuration = 4f;
        if (wallClip != null)
            AudioManager.FadeOut(wallClip, fadeDuration, 0, wallClip.Reader.Volume, true);

        // At this point sunMaterial points to the transparent sun — _Alpha is valid.
        float startCoreAlpha      = sunMaterial.GetFloat("_Alpha");
        float startHaloAlpha      = haloMaterial != null ? haloMaterial.GetFloat("_Alpha") : 1f;
        float startLightIntensity = sunLight.intensity;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
            sunMaterial.SetFloat("_Alpha", Mathf.Lerp(startCoreAlpha, 0f, t));
            if (haloMaterial != null) haloMaterial.SetFloat("_Alpha", Mathf.Lerp(startHaloAlpha, 0f, t));
            sunLight.intensity = Mathf.Lerp(startLightIntensity, 0f, t);
            yield return null;
        }

        sunMaterial.SetFloat("_Alpha", 0f);
        if (haloMaterial != null) haloMaterial.SetFloat("_Alpha", 0f);
        sunLight.intensity = 0f;

        gameObject.SetActive(false);
        currentPhase = Phase.Done;

        if (OwSystemSettings.SunResetAfterSupernovaEnd.Value)
            ResetAfterExplosion();
    }

    // ── Reset ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Full reset back to Phase.Red. Only valid when currentPhase == Done.
    /// </summary>
    public void ResetAfterExplosion()
    {
        if (currentPhase != Phase.Done) return;

        // Stop wall sound.
        if (wallClip != null)
        {
            AudioManager.FadeOut(wallClip, 0f, 0f, 0f, true);
            wallClip = null;
        }

        // Re-enable required target and swallowed bodies.
        requiredTarget.transform.gameObject.SetActive(true);
        foreach (var body in bodiesToSwallow)
        {
            if (body.transform != null && !body.transform.gameObject.activeSelf)
                body.transform.gameObject.SetActive(true);
        }

        // Restore opaque sun; hide transparent.
        transparentSunGO.SetActive(false);
        opaqueSunGO.SetActive(true);

        // Restore the corona object on the opaque sun.
        if (opaqueCollapseObject != null)
        {
            opaqueCollapseObject.localScale = Vector3.one;
            opaqueCollapseObject.gameObject.SetActive(true);
        }

        // Reset transparent sun's _Alpha so it is ready for the next cycle.
        transSunMaterial.SetFloat("_Alpha", 1f);
        if (transHaloMaterial != null) transHaloMaterial.SetFloat("_Alpha", 1f);

        // Redirect active refs back to the opaque materials.
        sunMaterial  = opaqueSunMaterial;
        haloMaterial = opaqueHaloMaterial;

        // Reset state flags.
        currentPhase     = Phase.Red;
        phaseTimer       = 0f;
        hasReachedPlayer = false;
        isFadingOut      = false;
        requiredTarget.engulfed = false;

        transform.localScale = initialScale;

        SunShaderUtils.ApplyCore(sunMaterial, startCore);
        if (haloMaterial != null) SunShaderUtils.ApplyHalo(haloMaterial, startHalo);

        if (sunLight != null)
        {
            sunLight.color     = sunlightOriginal;
            sunLight.intensity = originalLightIntensity;
        }

        Main.solarSystem.SignalScope.GetComponent<SignalScope>().TurnOffAllMusic();
        gameObject.SetActive(true);
        Main.solarSystem.Root.GetComponent<SolarSystem>().StartSolarSystem();

        MelonCoroutines.Start(InitializeAfterFrame());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private float CalculateWorldRadius(Transform obj)
    {
        Renderer[] allRenderers = obj.GetComponentsInChildren<Renderer>();
        Vector3    pivot        = obj.position;
        float      maxDist      = 0f;
        foreach (Renderer r in allRenderers)
        {
            string name = r.gameObject.name;
            if (name.Contains("Proxy") || name.Contains("Sand")) continue;
            float dist = (r.bounds.center - pivot).magnitude + r.bounds.extents.magnitude;
            if (dist > maxDist) maxDist = dist;
        }
        return maxDist > 0f ? maxDist : 0.25f;
    }

    private SunShaderUtils.SunCoreSettings LerpCore(
        SunShaderUtils.SunCoreSettings a, SunShaderUtils.SunCoreSettings b, float t)
        => new()
        {
            SunBright = Mathf.Lerp(a.SunBright, b.SunBright, t),
            SunSpeed  = a.SunSpeed,
            Color1    = Color.Lerp(a.Color1, b.Color1, t),
            Color2    = Color.Lerp(a.Color2, b.Color2, t),
            Color3    = Color.Lerp(a.Color3, b.Color3, t),
            Color4    = Color.Lerp(a.Color4, b.Color4, t),
        };

    private SunShaderUtils.SunHaloSettings LerpHalo(
        SunShaderUtils.SunHaloSettings a, SunShaderUtils.SunHaloSettings b, float t)
        => new()
        {
            HaloRing1          = a.HaloRing1,
            HaloRing1Color     = Color.Lerp(a.HaloRing1Color, b.HaloRing1Color, t),
            HaloRing1Size      = a.HaloRing1Size,
            HaloRing1Intensity = a.HaloRing1Intensity,
            HaloRing1Strength  = a.HaloRing1Strength,
            HaloRing2          = a.HaloRing2,
            HaloRing2Str       = a.HaloRing2Str,
            HaloRing2Thickness = a.HaloRing2Thickness,
            HaloRing2Color     = Color.Lerp(a.HaloRing2Color, b.HaloRing2Color, t),
            HaloRing2Size      = a.HaloRing2Size,
            HaloRing2Intensity = a.HaloRing2Intensity,
            HaloRing2Width     = a.HaloRing2Width,
        };
}