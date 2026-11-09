using System;
using MelonLoader;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class HourGlassTwins : MonoBehaviour
{
    // --- Configs ---
    public float transferDurationSeconds = 17 * 60f; // How long a sand transfer takes
    public float waitDurationSeconds = 5 * 60f;      // wait time between transfers

    public static bool randomSandStage = false;
    public float ashSpinSpeed = 4.0111f;
    public float emberSpinSpeed = 2.8649f;

    private readonly Vector3 ashEmptyScale = new Vector3(66, 66, 66);
    private readonly Vector3 ashFullScale = new Vector3(330, 330, 330);

    private readonly Vector3 emberEmptyScale = new Vector3(30, 30, 30);
    private readonly Vector3 emberFullScale = new Vector3(320, 320, 320);

    private readonly Vector3 activeScale = new Vector3(1, 1, 1);
    private readonly Vector3 funnelInactiveScale = Vector3.zero;

    private Quaternion defaultFunnelRotation;

    // --- References ---
    private GameObject sandFunnel;
    private GameObject ashTwinSand;
    private GameObject emberTwinSand;
    private GameObject ashTwinPlanet;
    private GameObject emberTwinPlanet;

    public enum State
    {
        WaitingForTransfer,
        Transferring
    }

    public State currentState = State.WaitingForTransfer;
    public bool flowingToAsh = false;
    public float stateTimer = 0f; // Seconds spent in the current state

    public HourGlassTwins(IntPtr ptr) : base(ptr) { }

    private void Start()
    {
        emberTwinPlanet = transform.GetChild(0).gameObject;
        emberTwinSand   = emberTwinPlanet.transform.GetChild(1).gameObject;

        sandFunnel = transform.GetChild(1).gameObject;

        ashTwinPlanet = transform.GetChild(2).gameObject;
        ashTwinSand   = ashTwinPlanet.transform.GetChild(0).gameObject;

        defaultFunnelRotation = sandFunnel.transform.localRotation;

        float sandProgress = 0f; // 0 = start of Ash->Ember flow (Ash full)
        if (randomSandStage) sandProgress = UnityEngine.Random.Range(0f, 1f);

        SetSandScales(sandProgress);
        sandFunnel.transform.localScale = funnelInactiveScale;

        currentState = State.WaitingForTransfer;
        stateTimer = 0f;
    }

    private void FixedUpdate()
    {
        emberTwinPlanet.transform.Rotate(0f, emberSpinSpeed * Time.fixedDeltaTime, 0f, Space.Self);
        ashTwinPlanet.transform.Rotate(0f, ashSpinSpeed * Time.fixedDeltaTime, 0f, Space.Self);

        stateTimer += Time.fixedDeltaTime;

        if (currentState == State.WaitingForTransfer)
        {
            sandFunnel.transform.localScale = funnelInactiveScale;

            if (stateTimer >= waitDurationSeconds)
            {
                stateTimer = 0f;
                currentState = State.Transferring;

                UpdateFunnelOrientation();
            }
        }
        else // Transferring
        {
            float progress = Mathf.Clamp01(stateTimer / transferDurationSeconds);

            SetSandScales(progress);
            sandFunnel.transform.localScale = activeScale;

            if (progress >= 1.0f)
            {
                stateTimer = 0f;
                currentState = State.WaitingForTransfer;
                flowingToAsh = !flowingToAsh;
            }
        }
    }

    private void UpdateFunnelOrientation()
    {
        sandFunnel.transform.localScale = new Vector3(0, 0, -1f);
    }

    public void SetSandScales(float progress)
    {
        float ashFill = flowingToAsh ? progress : (1f - progress);
        float emberFill = 1f - ashFill;

        emberTwinSand.transform.localScale = Vector3.Lerp(emberEmptyScale, emberFullScale, emberFill);
        ashTwinSand.transform.localScale = Vector3.Lerp(ashEmptyScale, ashFullScale, ashFill);
    }
}