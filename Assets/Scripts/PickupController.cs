using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PickupController : MonoBehaviour
{
    [Header("Components")]
    public Transform target;

    [Header("Parameters")]
    public LayerMask targetMask;        // Layers you can pick up
    public LayerMask ignoreTargetMask;  // Walls/floor/items (NOT PickupLayer, NOT portals)
    public LayerMask portalMask;        // Portal layer
    public float minDistance = 1.5f;
    public float maxScale = 5f;
    public float offsetFactor = 0.866f;
    public int maxPortalDepth = 4;

    [Header("Overlay")]
    public string overlayLayerName = "PickupLayer"; // layer ONLY the overlay camera renders
    [Header("Collision")]
    public float fitPadding = 0.98f;   // slightly shrink the test box so resting contact isn't "overlap"
    public float minCenterDistance = 0.05f;   // was 0.3
    public float nearMargin = 0.2f;           // near face stays at least 20% of d in front of the camera

    float ratio;   // scale-to-distance ratio actually used (may be capped)
    public int fitIterations = 12;

    Vector3 localBoundsCenter;   // object's bounds at scale 1
    Vector3 localBoundsExtents;
    float originalDistance;
    float originalScale;
    float originalMass;
    Rigidbody targetRb;
    Quaternion heldRotation;

    // Overlay clone
    Transform visual;
    Renderer[] realRenderers;
    ShadowCastingMode[] realShadowModes;

    struct Segment
    {
        public Vector3 origin, dir;
        public float length;
        public Quaternion rot;
    }
    readonly List<Segment> path = new List<Segment>();

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnEnable() { PortalTeleporter.PlayerTeleported += OnPlayerTeleported; }
    void OnDisable() { PortalTeleporter.PlayerTeleported -= OnPlayerTeleported; }

    void OnPlayerTeleported(Quaternion rot)
    {
        if (target != null) heldRotation = rot * heldRotation;
    }

    void Update()
    {
        HandleInput();
    }

    // Runs after the player has moved/teleported this frame, so the object never lags a frame behind
    void LateUpdate()
    {
        ResizeTarget();
    }

    bool Trace(LayerMask wallMask, out RaycastHit hit, out float totalDistance) //allows for portal-bent raycast paths
    {
        path.Clear();
        totalDistance = 0f;
        hit = default;

        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;
        Quaternion rot = Quaternion.identity;

        for (int i = 0; i <= maxPortalDepth; i++)
        {
            // Nearest real surface
            bool wallHit = Physics.Raycast(origin, dir, out hit, Mathf.Infinity, wallMask, QueryTriggerInteraction.Ignore);
            float wallDist = wallHit ? hit.distance : Mathf.Infinity;

            // Nearest portal (plane math, no colliders needed)
            PortalTeleporter best = null;
            float bestT = Mathf.Infinity;
            foreach (PortalTeleporter p in PortalTeleporter.All)
            {
                if (p.RayHit(origin, dir, out float t) && t < bestT)
                {
                    best = p;
                    bestT = t;
                }
            }

            // No portal in front of the nearest wall: the ray ends here
            if (best == null || bestT > wallDist + 0.05f)
            {
                if (!wallHit) return false;

                path.Add(new Segment { origin = origin, dir = dir, length = hit.distance, rot = rot });
                totalDistance += hit.distance;
                return true;
            }

            // Pass through the portal and continue from the exit
            path.Add(new Segment { origin = origin, dir = dir, length = bestT, rot = rot });
            totalDistance += bestT;

            Vector3 point = origin + dir * bestT;
            best.TransformRay(point, dir, out origin, out dir);
            rot = best.PortalRotation * rot;
            origin += dir * 0.01f;
        }
        return false;
    }

    Vector3 PointAlongPath(float distance, out Quaternion rot)
    {
        foreach (Segment seg in path)
        {
            if (distance <= seg.length)
            {
                rot = seg.rot;
                return seg.origin + seg.dir * distance;
            }
            distance -= seg.length;
        }
        Segment last = path[path.Count - 1];
        rot = last.rot;
        return last.origin + last.dir * last.length;
    }

    void HandleInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (target == null)
        {
            if (Trace(targetMask | ignoreTargetMask, out RaycastHit hit, out float distance)
                && ((1 << hit.collider.gameObject.layer) & targetMask) != 0)
            {
                target = hit.transform;
                targetRb = target.GetComponent<Rigidbody>();
                targetRb.isKinematic = true;

                originalDistance = distance;
                originalScale = target.localScale.x;
                MeshFilter mf = target.GetComponentInChildren<MeshFilter>();
                Bounds b = mf.sharedMesh.bounds;
                float unit = 1f / originalScale;
                localBoundsCenter = b.center * mf.transform.lossyScale.x * unit;
                localBoundsExtents = b.extents * mf.transform.lossyScale.x * unit;
                originalMass = targetRb.mass;
                float boundRadius = localBoundsExtents.magnitude;
                float maxRatio = (1f - nearMargin) / boundRadius;
                ratio = Mathf.Min(originalScale / originalDistance, maxRatio);
                heldRotation = Quaternion.Inverse(path[path.Count - 1].rot) * target.rotation;

                target.GetComponent<Collider>().enabled = false;
                CreateVisual();
            }
        }
        else
        {
            DestroyVisual();
            targetRb.isKinematic = false;
            target.GetComponent<Collider>().enabled = true;
            target = null;
        }
    }
    bool FitsAt(float d, float r)
    {
        float s = r * d;
        Vector3 pos = PointAlongPath(d, out Quaternion pathRot);
        Quaternion rot = pathRot * heldRotation;

        Vector3 center = pos + rot * (localBoundsCenter * s);
        Vector3 half = localBoundsExtents * s * fitPadding;

        return !Physics.CheckBox(center, half, rot, ignoreTargetMask, QueryTriggerInteraction.Ignore);
    }
    void CreateVisual()
    {
        // Visual-only copy for the overlay camera
        GameObject clone = Instantiate(target.gameObject);
        clone.name = target.name + " (overlay)";

        foreach (var c in clone.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var m in clone.GetComponentsInChildren<MonoBehaviour>()) Destroy(m);
        foreach (var rb in clone.GetComponentsInChildren<Rigidbody>()) Destroy(rb);

        int overlayLayer = LayerMask.NameToLayer(overlayLayerName);
        foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = overlayLayer;

        // The real object casts the shadows, so the clone shouldn't cast a second set
        foreach (var r in clone.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = ShadowCastingMode.Off;

        visual = clone.transform;

        // Real object: invisible, but still casts shadows into the world
        realRenderers = target.GetComponentsInChildren<Renderer>();
        realShadowModes = new ShadowCastingMode[realRenderers.Length];
        for (int i = 0; i < realRenderers.Length; i++)
        {
            realShadowModes[i] = realRenderers[i].shadowCastingMode;
            realRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    void DestroyVisual()
    {
        if (visual != null) Destroy(visual.gameObject);
        visual = null;

        if (realRenderers != null)
        {
            for (int i = 0; i < realRenderers.Length; i++)
                if (realRenderers[i] != null)
                    realRenderers[i].shadowCastingMode = realShadowModes[i];
        }
        realRenderers = null;
    }

    void ResizeTarget()
    {
        if (target == null) return;
        if (!Trace(ignoreTargetMask, out RaycastHit hit, out float distance)) return;

        float r = ratio;
        // Start from the ideal placement
        float centerDistance = distance / (1f + offsetFactor * r);
        if (r * centerDistance > maxScale) centerDistance = maxScale / r;

        // If the object would overlap something, find the largest distance that fits
        if (!FitsAt(centerDistance, r))
        {
            float lo = minCenterDistance, hi = centerDistance;
            for (int i = 0; i < fitIterations; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (FitsAt(mid, r)) lo = mid; else hi = mid;
            }
            centerDistance = lo;
        }

        float s = r * centerDistance;

        // Real object: portal-bent path
        target.localScale = Vector3.one * s;
        target.position = PointAlongPath(centerDistance, out Quaternion pathRot);
        target.rotation = pathRot * heldRotation;
        targetRb.mass = originalMass * (s / originalScale);

        // Overlay clone: straight line, same scale and distance, so same look on screen
        if (visual != null)
        {
            visual.localScale = Vector3.one * s;
            visual.position = transform.position + transform.forward * centerDistance;
            visual.rotation = heldRotation;
        }
    }
}