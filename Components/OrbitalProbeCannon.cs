using System;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class OrbitalProbeCannon : MonoBehaviour
{
    public OrbitalProbeCannon(IntPtr ptr) : base(ptr)
    {
    }

    private Transform baseTransform;
    private Transform middleTransform;
    private Transform tipTransform;
    private Light explosionLight;

    private Transform explosionTransform;

    private float fadeInDuration = 1f;
    private float fadeOutDuration = 4f;
    private float breakupDelay = 3.5f; // seconds after explosion starts before pieces move
    private float breakupDuration = 18f; // how long pieces take to fully reach target transforms

    private float timeToAim = 10f;
    private float explosionTime = 6f;
    private float timeBetweenLaunchAndExplosion = 0.5f;

    private Vector3 baseTagetPosition = new Vector3(0, 0, 0);
    private Quaternion baseTagetRotation = Quaternion.Euler(354.7151f, 225.9565f, 0);

    private Vector3 middleTagetPosition = new Vector3(1.919f, 0.00507f, -0.01671066f);
    private Quaternion middleTagetRotation = Quaternion.Euler(-0f, 0f, 69.0547f);

    private Vector3 tipTagetPosition = new Vector3(3.7276f, 0.006f, -0.006f);
    private Quaternion tipTagetRotation = Quaternion.Euler(-0, 0, 312.6561f);

    private float expandFraction = 0.75f;
    private float explosionPeakLightIntensity = 150f;

    // Explosion is pinned here in local space — 2 units offset on X, otherwise at origin
    private Vector3 explosionLocalPosition = new Vector3(0, 0.5636f, 1.3854f);

    private bool isAimed = false;
    private GameObject aimExclusionTarget;
    private float exclusionAngle = 60f;

    private Quaternion targetRotation;
    public Orbiter orbiter;

    void Start()
    {
        aimExclusionTarget = Main.solarSystem.GiantsDeep;
        Transform probeCannonRoot = gameObject.transform.GetChild(0);
        baseTransform = probeCannonRoot.GetChild(0);
        middleTransform = probeCannonRoot.GetChild(1);
        tipTransform = probeCannonRoot.GetChild(2);

        orbiter = GetComponent<Orbiter>();

        explosionTransform = middleTransform.GetChild(0);
        explosionLight = explosionTransform.GetChild(2).GetComponent<Light>(); //brightens peak is at 150
        explosionTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        explosionTransform.localPosition = explosionLocalPosition;
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
        } while (attempts < 100 && aimExclusionTarget != null && IsWithinExclusionAngle(candidate, fromPosition));

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
        yield return new WaitForSeconds(8f);
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

        explosionTransform.localPosition = explosionLocalPosition;
        haloMat.SetFloat("_Alpha", 0f);
        coreMat.SetFloat("_Alpha", 0f);
        explosionTransform.gameObject.SetActive(true);
        if (explosionLight != null) explosionLight.intensity = 0f;

        // Phase 1: fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            haloMat.SetFloat("_Alpha", Mathf.Lerp(0f, 1f, t));
            coreMat.SetFloat("_Alpha", Mathf.Lerp(0f, 1f, t));
            if (explosionLight != null) explosionLight.intensity = Mathf.Lerp(0f, explosionPeakLightIntensity, t);
            yield return null;
        }

        // Phase 2: fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            haloMat.SetFloat("_Alpha", Mathf.Lerp(1f, 0f, t));
            coreMat.SetFloat("_Alpha", Mathf.Lerp(1f, 0f, t));
            if (explosionLight != null) explosionLight.intensity = Mathf.Lerp(explosionPeakLightIntensity, 0f, t);
            yield return null;
        }

        haloMat.SetFloat("_Alpha", 0f);
        coreMat.SetFloat("_Alpha", 0f);
        explosionTransform.gameObject.SetActive(false);
        if (explosionLight != null) explosionLight.intensity = 0f;

        // Breakup delay — starts counting from when the explosion began, so subtract time already spent
        float timeAlreadyElapsed = fadeInDuration + fadeOutDuration;
        float remainingDelay = breakupDelay - timeAlreadyElapsed;
        if (remainingDelay > 0f)
            yield return new WaitForSeconds(remainingDelay);

        // Snapshot piece starting transforms right before they begin moving
        Vector3 baseStartPos = baseTransform.localPosition;
        Quaternion baseStartRot = baseTransform.localRotation;

        Vector3 middleStartPos = middleTransform.localPosition;
        Quaternion middleStartRot = middleTransform.localRotation;

        Vector3 tipStartPos = tipTransform.localPosition;
        Quaternion tipStartRot = tipTransform.localRotation;

        // Phase 3: breakup
        elapsed = 0f;
        while (elapsed < breakupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / breakupDuration);

            baseTransform.localPosition = Vector3.Lerp(baseStartPos, baseTagetPosition, t);
            baseTransform.localRotation = Quaternion.Slerp(baseStartRot, baseTagetRotation, t);

            middleTransform.localPosition = Vector3.Lerp(middleStartPos, middleTagetPosition, t);
            middleTransform.localRotation = Quaternion.Slerp(middleStartRot, middleTagetRotation, t);

            tipTransform.localPosition = Vector3.Lerp(tipStartPos, tipTagetPosition, t);
            tipTransform.localRotation = Quaternion.Slerp(tipStartRot, tipTagetRotation, t);

            yield return null;
        }

        // Cleanup — snap to final transforms
        baseTransform.localPosition = baseTagetPosition;
        baseTransform.localRotation = baseTagetRotation;
        middleTransform.localPosition = middleTagetPosition;
        middleTransform.localRotation = middleTagetRotation;
        tipTransform.localPosition = tipTagetPosition;
        tipTransform.localRotation = tipTagetRotation;

        if (orbiter != null)
        {
            orbiter.customRotation = transform.rotation; // lock to current broken orientation
            orbiter.spinEnabled = true;
        }
    }
}