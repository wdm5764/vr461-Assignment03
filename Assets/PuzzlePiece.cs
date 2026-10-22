using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;

public class PuzzlePiece : MonoBehaviour
{
    // piece we want to be in the same position of
    public Transform targetPuzzle;

    // piece that must be placed before we can place this one
    public PuzzlePiece prerequisite;

    // progression
    public bool isSolved;

    // stop regression if we advanced any further than this piece
    public bool grabLock;

    // components of this object
    private Rigidbody rigidbody;
    private MeshCollider meshCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        meshCollider = GetComponent<MeshCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(transform.position, targetPuzzle.position);

        if (grabLock)
        {
            meshCollider.enabled = false;
        } else
        {
            meshCollider.enabled = true;
        }

        if (!isSolved && (prerequisite == null || prerequisite.isSolved))
        {
            targetPuzzle.gameObject.GetComponent<MeshRenderer>().enabled = true;
        } else
        {
            targetPuzzle.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }

        if (distance < 0.05f && !isSolved && (prerequisite==null || prerequisite.isSolved))
        {
            IEnumerable<GrabInteractor> setInteractors = transform.GetChild(0).GetComponent<GrabInteractable>().Interactors;
            foreach (GrabInteractor interactor in setInteractors) { interactor.Unselect(); }

            isSolved = true;
            if (prerequisite) prerequisite.grabLock = true;


            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            // targetPuzzle.gameObject.SetActive(false);
        } else if (distance > 0.1f && !grabLock)
        {
            rigidbody.constraints = RigidbodyConstraints.None;
            // targetPuzzle.gameObject.SetActive(true);
            isSolved = false;
            if (prerequisite) prerequisite.grabLock = false;
        }

        if (isSolved)
        {
            transform.SetPositionAndRotation(targetPuzzle.position, targetPuzzle.rotation);
        }
    }
}
