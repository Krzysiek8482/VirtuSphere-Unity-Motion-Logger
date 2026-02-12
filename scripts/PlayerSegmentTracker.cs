using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class PlayerSegmentTracker : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI segmentHud;   // może być puste - wyświetlamy postęp w segmencie

    [Header("Opcje")]
    [Tooltip("Gdy włączone: pozwala na soft exit (auto-domknięcie przy końcu lub przy nowym START). Gdy wyłączone: tylko twardy END domyka segment.")] //czyli wejście w następny start
    public bool softExitEnabled = false; // domyślnie OFF do testów hard exit

    [Header("Stan bieżący (readonly)")]
    public SegmentDescriptor currentSegment;
    public string currentInstanceId;
    public SegmentKind currentKind;
    [Range(0f,1f)] public float progress01;
    public float sMeters;
    public bool insideSegment;

    // progi miękkiego domknięcia
    const float SoftEndProgress = 0.98f; // auto-END
    const float SoftEndOnNewStart = 0.90f; // gdy pojawia się START kolejnego

    bool hardEndLocked; // po twardym END blokujemy soft dla bieżącego segmentu

    void Update()
    {
        if (insideSegment && currentSegment)
        {
            sMeters = currentSegment.ComputeS(transform.position);
            float L = Mathf.Max(currentSegment.lengthL, 0.0001f);
            progress01 = Mathf.Clamp01(sMeters / L);

            // SOFT END: jeżeli prawie koniec, domknij segment nawet bez TriggerEnd (tylko gdy opcja włączona)
            if (softExitEnabled && !hardEndLocked && progress01 >= SoftEndProgress)
            {
                Debug.Log($"[SEGMENT EXIT*] (soft) kind={currentKind} id={Short(currentInstanceId)} s={sMeters:F2}/{currentSegment.lengthL:F2}");
                ClearState();
            }

            if (segmentHud)
                segmentHud.text = $"SEG: {currentKind} ({Short(currentInstanceId)}) s={sMeters:F2}/{L:F2} ({progress01:P0})";
        }
        else
        {
            sMeters = 0f; progress01 = 0f;
            if (segmentHud) segmentHud.text = "SEG: (none)";
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var gate = other.GetComponent<SegmentGate>();
        if (!gate || gate.segment == null) return;

        if (gate.gateType == GateType.Start)
        {   

            // NIE UŻYWANE 
            // Jeśli wjeżdżamy w START kolejnego segmentu, a poprzedni niemal skończony – domknij (tylko gdy soft exit włączony)
            if (softExitEnabled && insideSegment && currentSegment != gate.segment && progress01 >= SoftEndOnNewStart)
            {
                Debug.Log($"[SEGMENT EXIT*] (soft-on-start) kind={currentKind} id={Short(currentInstanceId)} s={sMeters:F2}/{currentSegment.lengthL:F2}");
                ClearState();
            }

            // Jeżeli nadal jesteśmy w innym segmencie i nie spełniliśmy progu
            if (insideSegment && currentSegment != gate.segment)
            {
                //Debug.LogWarning($"[SEGMENT WARN] Nowy START bez END poprzedniego: prev={currentKind}({Short(currentInstanceId)}), new={gate.segment.kind}({Short(gate.segment.instanceId)})");
            }

            currentSegment    = gate.segment;
            currentInstanceId = currentSegment.instanceId;
            currentKind       = currentSegment.kind;
            insideSegment     = true;
            if (TrackSequencer.Instance) TrackSequencer.Instance.AssignIfNeeded(currentSegment);

            Debug.Log($"[SEGMENT ENTER] kind={currentKind} id={Short(currentInstanceId)} pos={Pos2D()}");
            return;
        }

        if (gate.gateType == GateType.End)
        {
            if (insideSegment && gate.segment == currentSegment)
            {
                Debug.Log($"[SEGMENT EXIT ] kind={currentKind} id={Short(currentInstanceId)} s={sMeters:F2}/{currentSegment.lengthL:F2}");
                hardEndLocked = true;
                ClearState();
            }
            else
            {
               // Debug.LogWarning($"[SEGMENT WARN] END bez dopasowanego START: endOf={gate.segment.kind}({Short(gate.segment.instanceId)})");
            }
        }
    }

    void ClearState()
    {
        insideSegment = false;
        currentSegment = null;
        currentInstanceId = "";
        currentKind = default;
        sMeters = 0f; progress01 = 0f;
        hardEndLocked = false;
    }

    string Short(string guid) => string.IsNullOrEmpty(guid) ? "----" : guid.Substring(0, 8);
    string Pos2D(){ var p = transform.position; return $"[{p.x:F1},{p.z:F1}]"; }
}
