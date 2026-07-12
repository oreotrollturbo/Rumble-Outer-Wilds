using System.Collections;
using MelonLoader;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class HearthianMapSatelite : MonoBehaviour
{
    public HearthianMapSatelite(IntPtr ptr) : base(ptr) {}

    // ── Pulse timing ──────────────────────────────────────────────────────────
    public float offDuration     = 3f;
    public float fadeInDuration  = 0.7f;
    public float onDuration      = 1.8f;
    public float fadeOutDuration = 0.7f;

    private GameObject halo;
    private Material   haloMaterial;

    void Start()
    {
        halo = transform.GetChild(0).gameObject;

        transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
        transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(false);
        transform.GetChild(1).GetChild(0).GetChild(0).GetChild(3).gameObject.SetActive(false);
        
        Renderer haloRenderer = halo.GetComponentInChildren<Renderer>(true);
        if (haloRenderer != null)
        {
            haloMaterial = haloRenderer.material; // instance copy, safe to mutate per-object
            haloMaterial.SetFloat("_Alpha", 0f);
        }
        else
        {
            MelonLogger.Msg("[HearthianMapSatelite] No renderer found on halo — pulse will not run.");
        }

        MelonCoroutines.Start(PulseLoop());
    }

    // ── Pulse cycle: off → fade in → on → fade out → repeat ────────────────────
    private IEnumerator PulseLoop()
    {
        if (haloMaterial == null) yield break;

        while (true)
        {
            // Off
            SetHaloAlpha(0f);
            yield return new WaitForSeconds(offDuration);
            if (this == null) yield break;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                SetHaloAlpha(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeInDuration)));
                yield return null;
                if (this == null) yield break;
            }
            SetHaloAlpha(1f);

            // On
            yield return new WaitForSeconds(onDuration);
            if (this == null) yield break;

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                SetHaloAlpha(Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(elapsed / fadeOutDuration)));
                yield return null;
                if (this == null) yield break;
            }
            SetHaloAlpha(0f);
        }
    }

    private void SetHaloAlpha(float alpha)
    {
        if (haloMaterial == null) return;
        haloMaterial.SetFloat("_Alpha", alpha);
    }
}