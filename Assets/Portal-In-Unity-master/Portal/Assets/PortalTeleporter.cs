using System;
using System.Collections.Generic;
using UnityEngine;

public class PortalTeleporter : MonoBehaviour
{
    public Transform player;
    public Transform reciever;

    [Tooltip("Half size of the portal surface (x = local X, y = local Z). Leave at 0 to auto-detect from the mesh.")]
    public Vector2 halfSizeOverride;

    public static event Action<Quaternion> PlayerTeleported;
    public static readonly List<PortalTeleporter> All = new List<PortalTeleporter>();

    private bool playerIsOverlapping = false;
    private Vector2 halfSize = Vector2.one;

    void Awake()
    {
        if (halfSizeOverride != Vector2.zero)
        {
            halfSize = halfSizeOverride;
            return;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            Vector3 e = mf.sharedMesh.bounds.extents;
            Vector3 s = mf.transform.lossyScale;
            halfSize = new Vector2(e.x * Mathf.Abs(s.x), e.z * Mathf.Abs(s.z)); // Unity Plane primitive
        }
    }

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public Quaternion PortalRotation
    {
        get
        {
            float rotationDiff = -Quaternion.Angle(transform.rotation, reciever.rotation) + 180f;
            return Quaternion.Euler(0f, rotationDiff, 0f);
        }
    }

    public void TransformRay(Vector3 point, Vector3 dir, out Vector3 newPoint, out Vector3 newDir)
    {
        Quaternion rot = PortalRotation;
        newPoint = reciever.position + rot * (point - transform.position);
        newDir = rot * dir;
    }

    // Does a ray enter this portal's front face? Returns distance along the ray.
    public bool RayHit(Vector3 origin, Vector3 dir, out float t)
    {
        t = 0f;
        Vector3 n = transform.up;
        float denom = Vector3.Dot(dir, n);
        if (denom >= -1e-4f) return false; // moving away from / parallel to the front face

        t = Vector3.Dot(transform.position - origin, n) / denom;
        if (t <= 0f) return false;

        Vector3 offset = origin + dir * t - transform.position;
        float x = Vector3.Dot(offset, transform.right);
        float z = Vector3.Dot(offset, transform.forward);
        return Mathf.Abs(x) <= halfSize.x && Mathf.Abs(z) <= halfSize.y;
    }

    void Update()
    {
        if (!playerIsOverlapping) return;

        Vector3 portalToPlayer = player.position - transform.position;
        float dotProduct = Vector3.Dot(transform.up, portalToPlayer);

        if (dotProduct < 0f)
        {
            Quaternion rot = PortalRotation;

            CharacterController cc = player.GetComponent<CharacterController>();
            cc.enabled = false;

            player.rotation = rot * player.rotation;
            player.position = reciever.position + rot * portalToPlayer;

            cc.enabled = true;

            playerIsOverlapping = false;
            PlayerTeleported?.Invoke(rot);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerIsOverlapping = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerIsOverlapping = false;
    }
}