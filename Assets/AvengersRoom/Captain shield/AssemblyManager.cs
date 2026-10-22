using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AssemblyManager : MonoBehaviour
{
    [Header("Scene references")]
    public Transform pieces;
    public Transform targets;
    public GameObject fullShield;
    public TMP_Text instruction;         // optional

    [Header("Materials")]
    public Material ghostMaterial;
    public Material highlightMaterial;   // optional: brighter material for the next piece

    [Header("Snapping")]
    public float snapDistance = 0.3f;    // meters, bigger = easier
    public float snapAngle = 360f;       // 360 = ignore rotation completely
    public bool requireOrder = false;    // tick this to force the step order

    readonly List<AssemblyPiece> list = new();
    int placedCount;
    int step;

    void Start()
    {
        fullShield.SetActive(false);
        for (int i = 0; i < pieces.childCount; i++)
        {
            var p = pieces.GetChild(i);
            var t = targets.Find(p.name);
            if (t == null) { Debug.LogWarning("No ghost for " + p.name); continue; }

            SetMaterial(t, ghostMaterial);
            t.gameObject.SetActive(true);

            var ap = p.GetComponent<AssemblyPiece>();
            if (ap == null) ap = p.gameObject.AddComponent<AssemblyPiece>();
            ap.Init(this, t);
            ap.snapDist = snapDistance;
            ap.snapAngle = snapAngle;
            list.Add(ap);
        }
        step = 0;
        Refresh();
    }

    public bool CanPlace(AssemblyPiece p)
    {
        return !requireOrder || (step < list.Count && list[step] == p);
    }

    public void PieceDone(AssemblyPiece p)
    {
        p.target.gameObject.SetActive(false);
        placedCount++;
        while (step < list.Count && list[step].placed) step++;

        if (placedCount >= list.Count) Finish();
        else Refresh();
    }

    void Refresh()
    {
        if (highlightMaterial != null && requireOrder)
            for (int i = 0; i < list.Count; i++)
                if (!list[i].placed)
                    SetMaterial(list[i].target, i == step ? highlightMaterial : ghostMaterial);

        if (instruction)
            instruction.text = $"Placed {placedCount}/{list.Count}. Grab a piece and match the ghost.";
    }

    void Finish()
    {
        if (instruction) instruction.text = "Shield complete!";
        pieces.gameObject.SetActive(false);
        targets.gameObject.SetActive(false);
        fullShield.SetActive(true);
    }

    static void SetMaterial(Transform t, Material m)
    {
        if (m == null) return;
        foreach (var r in t.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int k = 0; k < mats.Length; k++) mats[k] = m;
            r.sharedMaterials = mats;
        }
    }
}