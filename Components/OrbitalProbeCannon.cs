using System;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class OrbitalProbeCannon : MonoBehaviour
{
    public OrbitalProbeCannon(IntPtr ptr) : base(ptr) { }

    private Transform baseTransform;
    private Transform middleTransform;
    private Transform tipTransform;
    private Light explosionLight;

    private Transform explosionTransform;

    private float timeToAim = 10f;
    private float explosionTime = 6f;
    private float timeBetweenLaunchAndExplosion = 0.5f;

    private Vector3 baseTagetPosition = new Vector3(1.919f, 0.00507f, -0.01671066f);
    private Quaternion baseTagetRotation = Quaternion.Euler(354.7151f, 225.9565f, 0);

    private Vector3 middleTagetPosition = new Vector3(0f, 0f, 0f);
    private Quaternion middleTagetRotation = Quaternion.Euler(-0f, 0f, 69.0547f);

    private Vector3 tipTagetPosition = new Vector3(3.7276f, 0.006f, -0.006f);
    private Quaternion tipTagetRotation = Quaternion.Euler(-0, 0, 312.6561f);

    private Vector3 explosionPeakScale = new Vector3(3f, 3f, 3f);
    private float expandFraction = 0.7f;
    private float explosionPeakLightIntensity = 150f;

    private bool isAimed = false;
    private GameObject aimExclusionTarget;
    private float exclusionAngle = 60f;

    private Quaternion targetRotation;
    private Orbiter orbiter;

    void Start()
    {
        aimExclusionTarget = Main.solarSystem.GiantsDeep;
        Transform probeCannonRoot = gameObject.transform.GetChild(0);
        baseTransform = probeCannonRoot.GetChild(0);
        middleTransform = probeCannonRoot.GetChild(1);
        tipTransform = probeCannonRoot.GetChild(2);
        
        explosionLight = transform.GetChild(0).GetChild(0).GetComponent<Light>(); //brightens peak is at 150

        orbiter = GetComponent<Orbiter>();

        explosionTransform = gameObject.transform.GetChild(1);
        explosionTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        explosionTransform.gameObject.SetActive(false);
    }

    private Quaternion GenerateSafeRotation(Vector3 fromPosition)
    {
        Quaternion candidate;
        int attempts = 0;
        do
        {
            float randomY = Random.Range(0f, 360f);
            float randomZ = Random.Range(0f, 360f);
            candidate = Quaternion.Euler(0f, randomY, randomZ);
            attempts++;
        }
        while (attempts < 100 && aimExclusionTarget != null && IsWithinExclusionAngle(candidate, fromPosition));

        return candidate;
    }

    private bool IsWithinExclusionAngle(Quaternion candidate, Vector3 fromPosition)
    {
        Vector3 candidateDir = candidate * Vector3.forward;
        Vector3 toTarget = (aimExclusionTarget.transform.position - fromPosition).normalized;
        return Vector3.Angle(candidateDir, toTarget) < exclusionAngle;
    }

    private void FixedUpdate()
    {
        if (isAimed) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            360f / timeToAim * Time.fixedDeltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.rotation = targetRotation;
            isAimed = true;
        }
    }

    public void StartFiringSequence()
    {
        MelonLogger.Msg("Starting firing sequence");
        MelonCoroutines.Start(FiringSequence());
    }

    private IEnumerator FiringSequence()
    {
        orbiter.spinEnabled = false;
        Vector3 predictedPosition = orbiter.GetPositionAtAngle(orbiter.GetOrbitAngleAfter(timeToAim));
            
        targetRotation = GenerateSafeRotation(predictedPosition);
        isAimed = false;

        while (!isAimed)
            yield return null;

        Main.solarSystem.OrbitalProbe.GetComponent<OrbitalProbe>().StartLaunch();

        yield return new WaitForSeconds(timeBetweenLaunchAndExplosion);

        MelonCoroutines.Start(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        Renderer explosionHalo = explosionTransform.GetChild(0).GetComponent<Renderer>();
        Renderer explosionCore = explosionTransform.GetChild(1).GetComponent<Renderer>();
        Material haloMat = explosionHalo.material;
        Material coreMat = explosionCore.material;

        explosionTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        haloMat.SetFloat("_Alpha", 1f);
        coreMat.SetFloat("_Alpha", 1f);
        explosionTransform.gameObject.SetActive(true);
        if (explosionLight != null) explosionLight.intensity = 0f;

        float expandDuration = explosionTime * expandFraction;
        float shrinkDuration = explosionTime - expandDuration;

        // Phase 1: expand
        float elapsed = 0f;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandDuration);
            explosionTransform.localScale = Vector3.LerpUnclamped(
                new Vector3(0.01f, 0.01f, 0.01f),
                explosionPeakScale,
                t
            );
            if (explosionLight != null) explosionLight.intensity = Mathf.Lerp(0f, explosionPeakLightIntensity, t); // <-- add this
            yield return null;
        }
        explosionTransform.localScale = explosionPeakScale;

        // Snapshot piece starting transforms
        Vector3 baseStartPos = baseTransform.localPosition;
        Quaternion baseStartRot = baseTransform.localRotation;

        Vector3 middleStartPos = middleTransform.localPosition;
        Quaternion middleStartRot = middleTransform.localRotation;

        Vector3 tipStartPos = tipTransform.localPosition;
        Quaternion tipStartRot = tipTransform.localRotation;

        float startHaloAlpha = haloMat.GetFloat("_Alpha");
        float startCoreAlpha = coreMat.GetFloat("_Alpha");

        // Phase 2: shrink + fade + slow breakup
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration); // linear, no SmoothStep

            haloMat.SetFloat("_Alpha", Mathf.Lerp(startHaloAlpha, 0f, t));
            coreMat.SetFloat("_Alpha", Mathf.Lerp(startCoreAlpha, 0f, t));
            if (explosionLight != null) explosionLight.intensity = Mathf.Lerp(explosionPeakLightIntensity, 0f, t);

            // Gentle ease-in shrink (starts slow, accelerates)
            float scaleEased = Mathf.Pow(t, 1.5f);
            explosionTransform.localScale = Vector3.LerpUnclamped(
                explosionPeakScale,
                new Vector3(0.01f, 0.01f, 0.01f),
                scaleEased
            );

            // Pieces drift apart slowly and linearly — no easing
            baseTransform.localPosition = Vector3.Lerp(baseStartPos, baseTagetPosition, t);
            baseTransform.localRotation = Quaternion.Slerp(baseStartRot, baseTagetRotation, t);

            middleTransform.localPosition = Vector3.Lerp(middleStartPos, middleTagetPosition, t);
            middleTransform.localRotation = Quaternion.Slerp(middleStartRot, middleTagetRotation, t);

            tipTransform.localPosition = Vector3.Lerp(tipStartPos, tipTagetPosition, t);
            tipTransform.localRotation = Quaternion.Slerp(tipStartRot, tipTagetRotation, t);

            yield return null;
        }

        // Cleanup
        haloMat.SetFloat("_Alpha", 0f);
        coreMat.SetFloat("_Alpha", 0f);
        explosionTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        explosionTransform.gameObject.SetActive(false);
        if (explosionLight != null) explosionLight.intensity = 0f;
        

        baseTransform.localPosition = baseTagetPosition;
        baseTransform.localRotation = baseTagetRotation;
        middleTransform.localPosition = middleTagetPosition;
        middleTransform.localRotation = middleTagetRotation;
        tipTransform.localPosition = tipTagetPosition;
        tipTransform.localRotation = tipTagetRotation;
        
        if (orbiter != null) orbiter.spinEnabled = true;
    }
}