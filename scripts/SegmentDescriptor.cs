using UnityEngine;

public enum SegmentKind {
    Straight,
    TurnLeft30, TurnRight30,
    TurnLeft45, TurnRight45,
    TurnLeft60, TurnRight60,
    TurnLeft90, TurnRight90
}


public class SegmentDescriptor : MonoBehaviour
{
    [Header("Typ segmentu")]
    public SegmentKind kind = SegmentKind.Straight;

    [Header("Markery (ustaw w prefabie)")]
    public Transform startPose;   // środek pasa na wejściu; X+ = kierunek jazdy Z+=lewo
    public Transform endPose;     // środek pasa na wyjściu; X+ = kierunek jazdy Z+=Lewo

    [Header("Auto-wyliczane (read-only)")]
    public float lengthL;         // długość linii środka (m)
    public float radius;          // przy łukach (m)
    public float angleDeg;        // 0 / 30 / 60 / 90
    public Vector3 arcCenter;     // przy łukach (w świecie)

    [Header("Id instancji (dla logów)")]
    public string instanceId;     // GUID

    public bool IsTurn =>
        kind == SegmentKind.TurnLeft30 || kind == SegmentKind.TurnLeft45  || kind == SegmentKind.TurnLeft60 || kind == SegmentKind.TurnLeft90 ||
        kind == SegmentKind.TurnRight30 || kind == SegmentKind.TurnRight45 || kind == SegmentKind.TurnRight60 || kind == SegmentKind.TurnRight90;

    public bool IsLeftTurn =>
        kind == SegmentKind.TurnLeft30 || kind == SegmentKind.TurnLeft45  || kind == SegmentKind.TurnLeft60 || kind == SegmentKind.TurnLeft90;


    [Header("Kolejność / Etykieta (auto)")]
    public int sequenceIndex;      // kolejność na torze (TrackSequencer) np 5
    public int ordinalOfKind;      // który z kolei dany typ (np drugi zakret w prawo 90 stopni = TR90 (2))
    public string label;           // np. "005-TR90 (2)"

    void Reset()
    {
        if (string.IsNullOrEmpty(instanceId)) instanceId = System.Guid.NewGuid().ToString(); //Jeśli instanceId puste generujemy nowy GUID.
        EnsureUniqueInstanceId(); //upewnienie że inny segment nie ma identycznego ID.
        Recompute(); //lizcenie geometrii
    }
    void OnValidate()
    {
        EnsureUniqueInstanceId(); //jak wyżej
        Recompute();
    }

    void OnEnable()
    {
        EnsureUniqueInstanceId(); // na wszelki wypadek w Play/po wklejeniu do sceny
        Recompute();
    }


    public void Recompute() // Przeliczenie parametrów geometrycznych segmentu
    {
        if (!startPose || !endPose) return;


        angleDeg = GetNominalAngle(kind);

        Vector2 S = new Vector2(startPose.position.x, startPose.position.z); 
        Vector2 E = new Vector2(endPose.position.x, endPose.position.z); 

        Vector2 tS = new Vector2(startPose.right.x, startPose.right.z).normalized;
        Vector2 tE = new Vector2(endPose.right.x, endPose.right.z).normalized;

        bool isTurn = angleDeg > 0.1f;

        if (!isTurn) // Przypadek odcinka prostego
        {
            lengthL = Vector2.Distance(S, E);
            radius = 0f;
            arcCenter = Vector3.zero;
            return;
        }
        // Przypadek łuku kołowego – wyznaczenie środka okręgu
        Vector2 nS = new Vector2(-tS.y, tS.x); 
        Vector2 nE = new Vector2(-tE.y, tE.x);

        float det = nS.x * (-nE.y) - nS.y * (-nE.x); 
        if (Mathf.Abs(det) < 1e-5f) 
        {

            float chord = Vector2.Distance(S, E);
            float angRad = Mathf.Deg2Rad * angleDeg;
            radius = chord / (2f * Mathf.Sin(angRad / 2f));

            Vector2 mid = 0.5f * (S + E);
            Vector2 dirMid = (E - S).normalized;
            Vector2 leftNormal = new Vector2(-dirMid.y, dirMid.x);

            float side = IsLeft(kind) ? 1f : -1f;
            float h = Mathf.Sqrt(Mathf.Max(radius * radius - (0.5f * chord) * (0.5f * chord), 0f));
            Vector2 C2 = mid + side * leftNormal * h;
            arcCenter = new Vector3(C2.x, startPose.position.y, C2.y);
        }
        else 
        {

            Vector2 rhs = E - S;

            float a11 = nS.x, a12 = -nE.x;
            float a21 = nS.y, a22 = -nE.y;
            float D = a11 * a22 - a12 * a21;
            float Du = rhs.x * a22 - a12 * rhs.y;

            float u = Mathf.Abs(D) < 1e-5f ? 0f : Du / D;
            Vector2 C2 = S + nS * u;
            arcCenter = new Vector3(C2.x, startPose.position.y, C2.y);
            radius = Vector2.Distance(C2, S);
        }

        // Długość łuku
        float angRadNom = Mathf.Deg2Rad * angleDeg;
        lengthL = Mathf.Abs(angRadNom) * Mathf.Max(radius, 0f);
    }

    public static float GetNominalAngle(SegmentKind k)
    {
        switch (k)
        {
            case SegmentKind.TurnLeft30:
            case SegmentKind.TurnRight30: return 30f;
            case SegmentKind.TurnLeft45:
            case SegmentKind.TurnRight45: return 45f;
            case SegmentKind.TurnLeft60:
            case SegmentKind.TurnRight60: return 60f;
            case SegmentKind.TurnLeft90:
            case SegmentKind.TurnRight90: return 90f;
            default: return 0f;
        }
    }

    public static bool IsLeft(SegmentKind k)
    {
        return k == SegmentKind.TurnLeft30 || k == SegmentKind.TurnLeft45 || k == SegmentKind.TurnLeft60 || k == SegmentKind.TurnLeft90;
    }

    public float ComputeS(Vector3 worldPos)
    {
        if (!startPose || !endPose) return 0f;

        Vector2 S = new Vector2(startPose.position.x, startPose.position.z);
        Vector2 E = new Vector2(endPose.position.x, endPose.position.z);
        Vector2 P = new Vector2(worldPos.x, worldPos.z);

        float ang = angleDeg;

        if (ang < 0.1f)
        {
            // Prosta
            Vector2 SE = (E - S);
            float L = SE.magnitude;
            if (L < 1e-5f) return 0f;
            Vector2 dir = SE / L;
            float s = Vector2.Dot(P - S, dir);
            return Mathf.Clamp(s, 0f, L);
        }

        // Łuk
        Vector2 C = new Vector2(arcCenter.x, arcCenter.z);
        Vector2 CS = (S - C).normalized;
        Vector2 CP = (P - C);
        if (CP.sqrMagnitude < 1e-6f) return 0f;

        float R = radius;
        float thetaSigned = SignedAngleRad(CS, CP.normalized);
        float theta = Mathf.Abs(thetaSigned);

        // Ogranicz do nominalnego kąta łuku
        float thetaNom = Mathf.Deg2Rad * ang;
        theta = Mathf.Clamp(theta, 0f, thetaNom);

        return theta * R;
    }

    static float SignedAngleRad(Vector2 a, Vector2 b)
    {
        float ang = Mathf.Atan2(a.x * b.y - a.y * b.x, Vector2.Dot(a, b));
        return (ang < 0f) ? -ang : ang; 
    }

void EnsureUniqueInstanceId()
{
    //Jeśli puste – nadaj GUID
    if (string.IsNullOrEmpty(instanceId))
    {
        instanceId = System.Guid.NewGuid().ToString();
    }

    if (!Application.isPlaying)
        return;

    //w trakcie dzialania upewniamy się, że nie ma duplikatów.
    var all = FindObjectsOfType<SegmentDescriptor>(true);
    for (int i = 0; i < all.Length; i++)
    {
        var other = all[i];
        if (other == null || other == this) continue;
        if (other.instanceId == this.instanceId)
        {
            instanceId = System.Guid.NewGuid().ToString();
            break;
        }
    }
}

    
    public static string ShortCode(SegmentKind k)
    {
        switch (k)
        {
            case SegmentKind.Straight:   return "ST";
            case SegmentKind.TurnLeft30: return "TL30";
            case SegmentKind.TurnRight30:return "TR30";
            case SegmentKind.TurnLeft45: return "TL45";
            case SegmentKind.TurnRight45:return "TR45";
            case SegmentKind.TurnLeft60: return "TL60";
            case SegmentKind.TurnRight60:return "TR60";
            case SegmentKind.TurnLeft90: return "TL90";
            case SegmentKind.TurnRight90:return "TR90";
            default: return k.ToString();
        }
    }

    public void SetSequenceData(int seqIndex, int ordinalOfThisKind)
    {
        sequenceIndex = seqIndex;
        ordinalOfKind = ordinalOfThisKind;
        label = $"{sequenceIndex:000}-{ShortCode(kind)}({ordinalOfKind})";
    }


}
