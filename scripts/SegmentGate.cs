using UnityEngine;

public enum GateType { Start, End }

[RequireComponent(typeof(Collider))]
public class SegmentGate : MonoBehaviour
{
    public GateType gateType = GateType.Start;
    public SegmentDescriptor segment;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (!segment) segment = GetComponentInParent<SegmentDescriptor>();
    }

    void OnValidate()
    {
        if (!segment) segment = GetComponentInParent<SegmentDescriptor>();
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Start()
    {
        var renderer = GetComponent<MeshRenderer>();
        //if (renderer) renderer.enabled = false; //tutaj komentować jak chce by było widoczne
    }
}
