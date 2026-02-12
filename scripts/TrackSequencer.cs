using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class TrackSequencer : MonoBehaviour
{
    public static TrackSequencer Instance { get; private set; }

    int _globalSeq = 0;
    readonly Dictionary<SegmentKind, int> _perKind = new Dictionary<SegmentKind, int>();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(this); return; }
        Instance = this;
        ResetAll();
    }

    public void ResetAll()
    {
        _globalSeq = 0;
        _perKind.Clear();

        // wyczyść stare etykiety/numery w scenie (żeby nowy przejazd zaczął od zera)
        var all = FindObjectsOfType<SegmentDescriptor>(true);
        foreach (var seg in all)
        {
            if (!seg) continue;
            seg.sequenceIndex = 0;
            seg.ordinalOfKind = 0;
            seg.label = "";
            seg.Recompute(); // upewnij się, że lengthL/geo są świeże
        }
        Debug.Log("[TrackSequencer] ResetAll()");
    }


    //Nadajemy  sekwencję/etykietę jeśli segment jeszcze jej nie ma.
    //Wołane z PlayerSegmentTracker przy ENTER.
    public void AssignIfNeeded(SegmentDescriptor seg)
    {
        if (!seg) return;
        // już ma przypisane - nic nie robimy
        if (seg.sequenceIndex > 0 && seg.ordinalOfKind > 0 && !string.IsNullOrEmpty(seg.label)) return;

        _globalSeq++;

        int ord;
        if (!_perKind.TryGetValue(seg.kind, out ord)) ord = 0;
        ord++;
        _perKind[seg.kind] = ord;

        seg.SetSequenceData(_globalSeq, ord);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(seg);
#endif

        // (opcjonalnie) log informacyjny
        // Debug.Log($"[TrackSequencer] Assigned {seg.label} to {seg.name}");
    }
}
