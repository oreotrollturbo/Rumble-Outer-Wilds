using System;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class QuantumOrbiter : MonoBehaviour
{
    private bool hasChangedPositions;
    private static Transform _worldOriginAnchor;
    private Renderer _renderer;
    private Orbiter _orbiter;
    
    public Dictionary<Transform,float> orbitParents = new ();
    
    private List<OrbitTarget> _cachedOrbitTargets = new ();
    
    public QuantumOrbiter(IntPtr ptr) : base(ptr) {}
    
    private struct OrbitTarget
    {
        public Transform ParentTransform;
        public Renderer ParentRenderer;
        public float OrbitDistance;
    }

    private void Start()
    {
        // Lazy-create one shared anchor at world origin; survives scene loads
        if (_worldOriginAnchor == null)
        {
            var go = new GameObject("QuantumOrbiter_OriginAnchor");
            GameObject.DontDestroyOnLoad(go);
            _worldOriginAnchor = go.transform;
        }

        _renderer = GetComponentInChildren<Renderer>();
        _orbiter  = GetComponent<Orbiter>();
        _cachedOrbitTargets = new List<OrbitTarget>();

        if (orbitParents != null)
        {
            foreach (var parent in orbitParents)
            {
                if (parent.Key == null) continue;
                _cachedOrbitTargets.Add(new OrbitTarget
                {
                    ParentTransform = parent.Key,
                    ParentRenderer  = parent.Key.GetComponentInChildren<Renderer>(),
                    OrbitDistance   = parent.Value,
                });
            }
        }

        if (_cachedOrbitTargets.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, _cachedOrbitTargets.Count);
            _orbiter.orbitParent = _cachedOrbitTargets[randomIndex].ParentTransform;
        }
    }

    void FixedUpdate()
    {
        if (_renderer == null) return;

        bool isBeingLookedAt = _renderer.isVisible;

        if (hasChangedPositions)
        {
            if (isBeingLookedAt)
                hasChangedPositions = false;
            return;
        }

        if (isBeingLookedAt) return;

        if (_cachedOrbitTargets.Count > 0)
        {
            int count      = _cachedOrbitTargets.Count;
            int startIndex = UnityEngine.Random.Range(0, count);
            OrbitTarget chosenTarget = default;
            bool foundTarget = false;

            for (int i = 0; i < count; i++)
            {
                int index  = (startIndex + i) % count;
                var target = _cachedOrbitTargets[index];

                // Parent is disabled (anchored to map origin by SolarSystem) —
                // orbit world origin at double distance instead of skipping entirely.
                if (target.ParentTransform == null ||
                    !target.ParentTransform.gameObject.activeInHierarchy)
                {
                    chosenTarget = new OrbitTarget
                    {
                        ParentTransform = _worldOriginAnchor,
                        ParentRenderer  = null,
                        OrbitDistance   = target.OrbitDistance * 2f,
                    };
                    foundTarget = true;
                    break;
                }

                if (target.ParentRenderer == null) continue;

                if (!target.ParentRenderer.isVisible)
                {
                    chosenTarget = target;
                    foundTarget  = true;
                    break;
                }
            }

            if (foundTarget && _orbiter != null)
            {
                _orbiter.orbitParent   = chosenTarget.ParentTransform;
                _orbiter.SetCurrentAngle(Orbiter.GetRandomAngle());
                _orbiter.orbitDistance = chosenTarget.OrbitDistance;
                hasChangedPositions    = true;
            }
        }
    }
    
}