using MelonLoader;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class StarBackground : MonoBehaviour
{
    public StarBackground(IntPtr ptr) : base(ptr) { }
    
    const float FarPlaneSafetyMargin = 0.3f;

    const float RescanInterval = 1.5f;
    const float RescanWindowSeconds = 500f;
    const string CopyName = "StarBackground_CameraCopy";
    
    bool _ready;
    
    public static Shader starShader;
    public static Texture starTexture;

    class CameraStars
    {
        public Camera Camera;
        public Transform Transform;
        public Renderer Renderer;
    }

    readonly List<CameraStars> _copies = new ();

    Mesh _mesh;
    Material _material;
    float _meshRadius;
    Quaternion _baseRotation = Quaternion.identity;
    Quaternion _lastRotation = Quaternion.identity;

    float _rescanTimer;
    float _rescanUntil;

    bool _hooked;
    Action<ScriptableRenderContext, Camera> _beginCameraHandler;


    void Start()
    {
        var filter = GetComponent<MeshFilter>();
        var templateRenderer = GetComponent<Renderer>();

        if (filter == null || filter.sharedMesh == null || templateRenderer == null)
        {
            MelonLogger.Error("[StarBackground] Template has no mesh/renderer, star background disabled.");
            enabled = false;
            return;
        }

        if (Main.solarSystem.Root != null && transform.parent != Main.solarSystem.Root.transform)
            transform.SetParent(Main.solarSystem.Root.transform, true);

        _mesh = filter.sharedMesh;
        _meshRadius = Mathf.Max(_mesh.bounds.extents.magnitude, 1e-4f);

        _material = new Material(starShader);
        _material.name = "StarMat_Runtime";
        _material.hideFlags = HideFlags.DontUnloadUnusedAsset;
        if (starTexture != null)
            _material.SetTexture("_BaseMap", starTexture);
        _material.SetFloat("_Intensity", 2f);
        _material.SetFloat("_Saturation", 1f);

        _baseRotation = transform.localRotation;
        _lastRotation = _baseRotation;

        templateRenderer.enabled = false;

        RegisterRenderHook();

        Actions.onMapInitialized += OnMapInitialized;

        BeginRescanWindow();
        _ready = true;
        ScanCameras();
    }

    void OnDestroy()
    {
        try
        {
            if (_hooked && _beginCameraHandler != null)
                RenderPipelineManager.beginCameraRendering -= _beginCameraHandler;
        }
        catch { }
    }

    void Update()
    {
        if (Time.unscaledTime > _rescanUntil) return;

        _rescanTimer -= Time.unscaledDeltaTime;
        if (_rescanTimer > 0f) return;

        _rescanTimer = RescanInterval;
        ScanCameras();
    }
    
    void LateUpdate()
    {
        if (_hooked) return;

        Quaternion rot = ComputeStarRotation();
        foreach (var c in _copies)
        {
            if (c.Transform == null || c.Camera == null) continue;
            ApplyToCopy(c.Transform, c.Camera, rot);
        }
    }

    void ApplyToCopy(Transform t, Camera cam, Quaternion rot)
    {
        t.rotation = rot;

        // World radius = just inside this camera's far plane, whatever scale the camera's parents have
        float desiredWorldScale = cam.farClipPlane * FarPlaneSafetyMargin / _meshRadius;
        Transform parent = t.parent;
        float parentScale = parent != null ? parent.lossyScale.x : 1f;
        if (Mathf.Abs(parentScale) < 1e-6f) parentScale = 1f;

        t.localScale = Vector3.one * (desiredWorldScale / parentScale);
    }


    // Sun-based rotation
    Quaternion ComputeStarRotation()
    {
        GameObject sun = Main.solarSystem.Sun;
        if (sun != null)
            _lastRotation = sun.transform.rotation * _baseRotation;

        return _lastRotation;
    }

    void OnMapInitialized(string mapName)
    {
        BeginRescanWindow();
        ScanCameras();
    }

    void BeginRescanWindow()
    {
        _rescanUntil = Time.unscaledTime + RescanWindowSeconds;
        _rescanTimer = RescanInterval;
    }

    public void RescaleToFarClip()
    {
        BeginRescanWindow();
        ScanCameras();
    }


    void ScanCameras()
    {
        if (!_ready) return;
        _copies.RemoveAll(c => c.Camera == null || c.Transform == null);

        foreach (Camera cam in Object.FindObjectsOfType<Camera>(true))
        {
            if (IsTracked(cam)) continue;
            if (!ShouldHaveStars(cam)) continue;

            CreateCopy(cam);
        }
    }

    bool IsTracked(Camera cam)
    {
        foreach (var c in _copies)
            if (c.Camera == cam) return true;
        return false;
    }

    static bool ShouldHaveStars(Camera cam)
    {
        switch (cam.cameraType)
        {
            case CameraType.SceneView:
            case CameraType.Preview:
            case CameraType.Reflection:
                return false;
        }
        try
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null && data.renderType == CameraRenderType.Overlay) return false;
        }
        catch { }

        return true;
    }

    void CreateCopy(Camera cam)
    {
        var go = new GameObject(CopyName);
        go.layer = PickLayer(cam);
        
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        Transform t = go.transform;
        t.SetParent(cam.transform, false);
        t.localPosition = Vector3.zero;
        ApplyToCopy(t, cam, _lastRotation);

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = _mesh;

        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = _material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        
        r.enabled = !_hooked;

        _copies.Add(new CameraStars { Camera = cam, Transform = t, Renderer = r });
    }

    static int PickLayer(Camera cam)
    {
        int mask = cam.cullingMask;
        int own = cam.gameObject.layer;

        if ((mask & (1 << own)) != 0) return own;
        if ((mask & 1) != 0) return 0;

        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0) return i;

        return own;
    }


    void RegisterRenderHook()
    {
        if (_hooked) return;

        try
        {
            _beginCameraHandler = OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += _beginCameraHandler;
            _hooked = true;
        }
        catch (Exception ex)
        {
            _hooked = false;
            MelonLogger.Warning("[StarBackground] Could not hook beginCameraRendering, stars will render on all cameras: " + ex.Message);
        }
    }
    
    void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        Quaternion rot = ComputeStarRotation();

        foreach (var c in _copies)
        {
            if (c.Renderer == null || c.Transform == null) continue;

            bool mine = c.Camera == cam;
            c.Renderer.enabled = mine;
            if (mine) ApplyToCopy(c.Transform, cam, rot);
        }
    }
}