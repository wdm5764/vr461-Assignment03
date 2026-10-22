using UnityEngine;
using Oculus.Interaction;

public class AssemblyPiece : MonoBehaviour
{
    public float snapDist = 0.15f;
    public float snapAngle = 180f;

    [HideInInspector] public Transform target;
    [HideInInspector] public bool placed;
    Grabbable grabbable;
    Rigidbody rb;
    AssemblyManager mgr;

    public void Init(AssemblyManager m, Transform t)
    {
        mgr = m; target = t;
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        grabbable.WhenPointerEventRaised += OnPointer;
    }

    void OnPointer(PointerEvent e)
    {
        if (e.Type == PointerEventType.Unselect) TrySnap();
    }

    void TrySnap()
    {
        if (placed || !mgr.CanPlace(this)) return;
        if (Vector3.Distance(transform.position, target.position) > snapDist) return;
        if (Quaternion.Angle(transform.rotation, target.rotation) > snapAngle) return;

        placed = true;
        transform.SetPositionAndRotation(target.position, target.rotation);
        rb.isKinematic = true;
        mgr.PieceDone(this);
        Invoke(nameof(LockPiece), 0.1f);
    }

    void LockPiece()
    {
        grabbable.enabled = false;
        foreach (Transform c in transform)
            if (c.name.StartsWith("ISDK_")) c.gameObject.SetActive(false);
    }
}