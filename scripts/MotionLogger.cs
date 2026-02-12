using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using TMPro;


public class MotionLogger : MonoBehaviour
{   
    [Header("Output")]

    [Tooltip("Jeśli true -> zapis do folderu z .exe. Jeśli false -> użyj outputDirectory.")]
    public bool saveInExeFolder = false;
    [Tooltip(@"Bazowy folder do logów (Windows). Jeśli nie istnieje zostanie utworzony.
Jeśli nie uda się utworzyć  fallback do Application.persistentDataPath.")]
    public string outputDirectory = @"C:\Users\krzys\unity_proj\Creating_race_track_2\Movement_logs";

    [Tooltip("Nazwa = log_YYYYMMDD_HHMMSS.csv")]
    public bool includeDateTimeInName = true;

    [Header("Sampling")]
    [Tooltip("Częstotliwość próbkowania (Hz). Np. 10 = co 0.1 s.")]
    public float sampleHz = 10f;

    [Tooltip("Automatycznie startuj logowanie w OnEnable()")]
    public bool startOnEnable = false;

    [Tooltip("Flush co X sekund (zmniejsza ryzyko utraty danych przy crashu)")]
    public float flushIntervalSec = 1.0f;

    [Header("Lateral offset / lane")]
    [Tooltip("Połowa szerokości pasa (m). Np. 1.0 przy szerokości 2 m.")]
    public float laneHalfWidthM = 1.5f;

    [Tooltip("Promień gracza (m) od środka kapsuły/kuli do krawędzi.")]
    public float playerRadiusM = 0.3f;

    [Header("Session")]
    public string sessionTag = "T1";


    [Header("References")]
    public Transform player;                       // Player transform 
    public Rigidbody rb;                           // Player rigidbody
    public PlayerSegmentTracker tracker;           // aktualny segment/progress
    public VirtuSphereController virtu;            // prędkości/direction z VirtuSphere
    public VirtuSphereGUIController guiControl;    // GUI control wektor/yaw
    public CameraSwitcher camSwitch;               // tryb kamery (FPS/TopDown)
    [Header("View / HMD")]
    public Transform hmd; //kamera Vr
    [Header("Debug UI")]
    [Tooltip("Opcjonalny TextMeshProUGUI do podglądu odchylenia L/R w czasie rzeczywistym.")]
    public TextMeshProUGUI lateralDebugText;

    


    // runtime
    private string _filePath;
    private StreamWriter _writer;
    private float _dtSample;
    private float _timerSample;
    private float _timerFlush;
    private readonly CultureInfo _csvCulture = new CultureInfo("pl-PL"); // liczby z przecinkiem
    private const char SEP = ';';
    private bool _isLogging;

    private bool _hasLastPos = false;
    private Vector3 _lastPos = Vector3.zero;
    private float _distanceM = 0f;

    void OnEnable()
    {
        if (startOnEnable) StartLogging();
    }

    void OnDisable()
    {
        StopLogging();
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }

    public void StartLogging()
    {
        if (_isLogging) return;

        //Ścieżka folderu
    string baseDir;

    if (saveInExeFolder)
    {
        // folder gdzie znajduje się .exe 
        baseDir = Application.dataPath;
        baseDir = Directory.GetParent(baseDir).FullName; 
    }
    else
    {
        baseDir = outputDirectory;
    }

    if (string.IsNullOrWhiteSpace(baseDir))
        baseDir = Application.persistentDataPath;

        try
        {
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MotionLogger] Nie udało się utworzyć katalogu '{baseDir}'. " +
                             $"Fallback -> {Application.persistentDataPath}. Err: {e.Message}");
            baseDir = Application.persistentDataPath;
        }

        // Nazwa pliku
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");  // generuje datę np. 20250213_195012

        string tag = string.IsNullOrWhiteSpace(sessionTag)
            ? "default"               // jeśli sessionTag jest pusty -> ustaw 'default'
            : sessionTag;             // jeśli nie -> użyj sessionTag

        string fname = includeDateTimeInName
            ? $"log_{tag}_{ts}.csv"   // jeśli chcemy datę w nazwie -> log_nazwa_data.csv
            : $"log_{tag}.csv";       // jeśli nie-> log_nazwa.csv


        _filePath = Path.Combine(baseDir, fname);


        // Writer
        try
        {
            _writer = new StreamWriter(_filePath, false, Encoding.UTF8); //false - overwrite a nie append
        }
        catch (Exception e)
        {
            Debug.LogError($"[MotionLogger] Nie udało się otworzyć pliku do zapisu: '{_filePath}'. {e.Message}");
            _writer = null;
            return;
        }

        // Nagłówek
        WriteHeader();

        // Reset dystansu
        _hasLastPos = false;
        _lastPos = Vector3.zero;
        _distanceM = 0f;

        // Timery
        _dtSample = Mathf.Max(0.0001f, 1f / Mathf.Max(0.01f, sampleHz)); 
        _timerSample = 0f;
        _timerFlush = 0f;

        _isLogging = true;
        Debug.Log($"[MotionLogger] Log start -> {_filePath}");
    }

    public void StopLogging()
    {
        if (!_isLogging) return;

        try
        {
            _writer?.Flush(); 
            _writer?.Close(); 
            _writer?.Dispose(); 
        }
        catch { /*ignore*/ }
        finally
        {
            _writer = null;
            _isLogging = false;
            Debug.Log("[MotionLogger] Log stop");
        }
    }

    void Update()
    {
        if (!_isLogging || _writer == null) return;

        _timerSample += Time.unscaledDeltaTime;
        _timerFlush  += Time.unscaledDeltaTime;

        // sampling 
        if (_timerSample >= _dtSample)
        {
            _timerSample -= _dtSample;
            WriteSample();
        }

        // okresowy flush
        if (_timerFlush >= Mathf.Max(0.1f, flushIntervalSec))
        {
            _timerFlush = 0f;
            try { _writer.Flush(); } catch { /*ignore*/ }
        }
    }

    void WriteHeader()
    {
        //srednik jako separator do polskiego Excela
        string[] cols = new string[]
        {
            "timestamp",             
            "timeSinceStart_s",
            "frame",
            "dist_m",

            "pos_x_m","pos_y_m","pos_z_m",
            "view_yaw_deg","view_pitch_deg",

            "rb_vx_mps","rb_vy_mps","rb_vz_mps",
            "rb_speed_mps",
            "rb_angY_radps",

            "segment_kind","segment_id",
            "segment_label","segment_seq","segment_ord",
            "seg_s_m","seg_L_m","seg_progress01",

            
            "seg_offset_m",
            "seg_LR",
            "seg_offset_pct",

            "vs_speed_mps","vs_dir_deg","vs_wz_radps",
            //"gui_hasInput","gui_speed_mps","gui_yaw_deg",

            "cam_mode"               // FPS/TopDown
        };

        _writer.WriteLine(string.Join(SEP, cols));
    }

void WriteSample()
{
    Vector3 pos = player ? player.position : Vector3.zero;
    Vector3 vel = rb ? rb.velocity : Vector3.zero;
    Vector3 angVel = rb ? rb.angularVelocity : Vector3.zero;

    // Kąt patrzenia 
    float viewYaw = 0f;
    float viewPitch = 0f;

    if (hmd)
    {
        Vector3 e = hmd.rotation.eulerAngles;

        // poziom (obrót wokół osi Y) – bez zmian 0 - 360
        viewYaw = e.y;

        // pion (obrót wokół osi X) – zmapujemy do -180 180 
        float pitchRaw = e.x;
        if (pitchRaw > 180f) pitchRaw -= 360f;
        viewPitch = pitchRaw;
    }
    
    // dystans
    if (_hasLastPos)
    {
        Vector2 prev = new Vector2(_lastPos.x, _lastPos.z);
        Vector2 cur  = new Vector2(pos.x, pos.z);
        _distanceM += Vector2.Distance(prev, cur);
    }
    _lastPos = pos;
    _hasLastPos = true;

    // Segment (z trackera):
        string segKind = tracker && tracker.currentSegment ? tracker.currentKind.ToString() : ""; // typ segmentu np. "TurnRight90"
    string segId   = tracker && tracker.currentSegment ? tracker.currentSegment.instanceId : ""; // GUID segmentu
    string segLbl  = tracker && tracker.currentSegment ? tracker.currentSegment.label : ""; // np. "005-TR90(2)"
    int    segSeq  = (tracker && tracker.currentSegment) ? tracker.currentSegment.sequenceIndex : -1;
    int    segOrd  = (tracker && tracker.currentSegment) ? tracker.currentSegment.ordinalOfKind : -1;
    float  segS    = tracker ? tracker.sMeters : 0f; // postęp w segmencie [m]
    float  segL    = (tracker && tracker.currentSegment) ? tracker.currentSegment.lengthL : 0f; // długość segmentu [m]
    float  segP    = tracker ? tracker.progress01 : 0f; // 0-1

    //LATERAL OFFSET: lewo/prawo od linii środka segmentu

    float segOffsetM = 0f;      // w metrach, >0 = lewo, <0 = prawo
    string segLR = "";          // L, R lub C
    float segOffsetPct = 0f;    // -1 - 1

    if (tracker && tracker.currentSegment && player)
    {
        var seg = tracker.currentSegment;

        float? lateralMaybe = null;

        if (!seg.IsTurn)
        {
            // PROSTA
            Transform refPose = seg.startPose ? seg.startPose : seg.transform;

            Vector3 origin = refPose.position;
            Vector3 delta = player.position - origin;
            Vector2 deltaXZ = new Vector2(delta.x, delta.z);

            
            Vector3 fwdWorld = refPose.right;
            Vector2 fwdXZ = new Vector2(fwdWorld.x, fwdWorld.z).normalized;
            Vector2 leftXZ = new Vector2(-fwdXZ.y, fwdXZ.x);

            if (leftXZ.sqrMagnitude > 1e-6f)
            {
                float lateral = Vector2.Dot(deltaXZ, leftXZ);
                lateralMaybe = lateral;
            }
        }
        else
        {
            //ZAKRĘT – offset radialny względem łuku środka
            Vector3 C3 = seg.arcCenter;
            float R = seg.radius;

            if (R > 0.0001f)
            {
                Vector2 C = new Vector2(C3.x, C3.z);
                Vector2 P = new Vector2(player.position.x, player.position.z);
                Vector2 v = P - C;
                float dist = v.magnitude;

                if (dist > 1e-4f)
                {
                    float rawOffset = dist - R; // >0 = „na zewnątrz”, <0 = „do środka”
                    float sideSign = seg.IsLeftTurn ? -1f : +1f;
                    float lateral = rawOffset * sideSign; // >0 = lewo, <0 = prawo (spójnie z prostą)
                    lateralMaybe = lateral;
                }
            }
        }

        if (lateralMaybe.HasValue)
        {
            float lateral = lateralMaybe.Value;
            segOffsetM = lateral;

            // Strona: Left / Right / Center (martwa strefa +-1 cm)
            if (Mathf.Abs(lateral) < 0.01f)
                segLR = "C";
            else
                segLR = (lateral > 0f) ? "L" : "R";
            // Normalizacja do %-ów względem połowy szerokości pasa
            float usableHalf = Mathf.Max(0.01f, laneHalfWidthM - playerRadiusM);
            segOffsetPct = Mathf.Clamp(lateral / usableHalf, -1f, 1f);
        }
    }

    // VirtuSphere:
    float vsSpd = virtu ? virtu.sphereSpeedMS      : 0f;  // m/s
    float vsDir = virtu ? virtu.sphereDirectionDeg : 0f;  // deg
    float vsWz  = virtu ? virtu.verticalRotation   : 0f;  // rad/s

    // Kamera:
    string camMode = "FPS";
    if (camSwitch) camMode = camSwitch.useTopDown ? "TopDown" : "FPS";

    // zapis z ';'
    string iso = "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    int frame = Time.frameCount;
    float t = Time.time;

    if (!(tracker && tracker.currentSegment))
    {
        segKind = "Na";
        segId   = "Na";
        segLbl  = "Na";
        segSeq  = -1;
        segOrd  = -1;
        segS    = 0f;
        segL    = 0f;
        segP    = 0f;

        segOffsetM = 0f;
        segLR = "C";
        segOffsetPct = 0f;
    }

    string[] vals = new string[]
    {
        iso,
        t.ToString("0.000", _csvCulture),
        frame.ToString(_csvCulture),
        _distanceM.ToString("0.###", _csvCulture),

        pos.x.ToString("0.###", _csvCulture),
        pos.y.ToString("0.###", _csvCulture),
        pos.z.ToString("0.###", _csvCulture),
        viewYaw.ToString("0.###", _csvCulture),
        viewPitch.ToString("0.###", _csvCulture),

        vel.x.ToString("0.###", _csvCulture),
        vel.y.ToString("0.###", _csvCulture),
        vel.z.ToString("0.###", _csvCulture),
        new Vector2(vel.x, vel.z).magnitude.ToString("0.###", _csvCulture),
        angVel.y.ToString("0.###", _csvCulture),

        segKind,
        segId,
        segLbl,
        segSeq.ToString(_csvCulture),
        segOrd.ToString(_csvCulture),
        segS.ToString("0.###", _csvCulture),
        segL.ToString("0.###", _csvCulture),
        segP.ToString("0.###", _csvCulture),
        // offset
        segOffsetM.ToString("0.###", _csvCulture),
        segLR,
        segOffsetPct.ToString("0.###", _csvCulture),

        // VirtuSphere
        vsSpd.ToString("0.###", _csvCulture),
        vsDir.ToString("0.###", _csvCulture),
        vsWz.ToString("0.###", _csvCulture),

        camMode
    };
    //debug - podgląd lateral offset w UI
    if (lateralDebugText != null)
        {
            lateralDebugText.text =
                $"{segLR}  {segOffsetM:0.00} m  ({segOffsetPct:0.00})";
        }

    //koniec debugu
    _writer.WriteLine(string.Join(SEP, vals));
}

}
