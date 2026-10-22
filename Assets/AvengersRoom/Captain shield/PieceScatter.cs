using UnityEngine;

public class PieceScatter : MonoBehaviour
{
    public bool useThisObject = true;     // true = spawn at this object's position (the Pieces Transform)
    public Vector3 spawnPosition = new Vector3(0.55f, 1.1f, 0.75f);   // used when the box above is off
    public int columns = 4;
    public Vector2 spacing = new Vector2(0.22f, 0.12f);

    void Start() { Arrange(); }

    [ContextMenu("Arrange Pieces")]
    void Arrange()
    {
        Vector3 origin = useThisObject ? transform.position : spawnPosition;
        int count = transform.childCount;
        int rows = Mathf.CeilToInt(count / (float)columns);
        int i = 0;
        foreach (Transform c in transform)
        {
            int col = i % columns, row = i / columns;
            c.position = origin
                + transform.right   * ((col - (columns - 1) / 2f) * spacing.x)
                + transform.forward * ((row - (rows - 1) / 2f) * spacing.y);

            var rb = c.GetComponent<Rigidbody>();
            if (rb) { rb.useGravity = false; rb.isKinematic = true; }
            i++;
        }
    }
}