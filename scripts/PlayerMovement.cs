using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Keyboard movement")]
    [SerializeField] private float keyboardMoveSpeed = 6f;
    [SerializeField] private float groundDrag = 5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.3f;
    private bool grounded;

    [Header("References")]
    [SerializeField] private Transform orientation; //orientacja się kręci a nie rigidbody-osobny transform, który trzyma kierunek przodu gracza

    [Header("Debug")]
    public TextMeshProUGUI debugText; //fpv debug widok
    public TMPro.TextMeshProUGUI debugText2;  // topdown debug widok

    [Header("Emulator")]
    [SerializeField] private bool useEmulator = false; // czy używać VirtuSphere jako źródła ruchu
    [SerializeField] private VirtuSphereController virtu;   // obiekt z moveVector
    [SerializeField] private float emulatorSpeedScale = 1f; // 1.0 = m/s 1:1
    [SerializeField] private bool yawFromEmulator = true;   // obrót w miejscu z verticalRotation
    [SerializeField] private float yawMultiplier = 1f;      // czułość yaw
    [SerializeField] private float maxEmuSpeedMS = 0f;      // 0 = bez limitu; >0 = hard cap m/s

    private float horizontalInput; //wejście z klawiatury - nieużywane gdy useEmulator = true
    private float verticalInput; //wejście z klawiatury - nieużywane gdy useEmulator = true

    [Tooltip("Stały obrót ruchu z emulatora w stopniach (np. 90 lub -90).")]
    [SerializeField] private float emulatorYawOffsetDeg = 0f;

    private Vector3 moveDirection;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Ground check - sprawdzenie czy gracz stoi na ziemi było używane do debugu
        grounded = Physics.CheckSphere(groundCheck.position, groundRadius, whatIsGround);

        ReadInput();

        // Clamp prędkości tylko dla klawiatury
        if (!useEmulator) SpeedControl();

        // Drag: emulator = 0 (żeby nic nie hamowało)
        rb.drag = useEmulator ? 0f : (grounded ? groundDrag : 0f);

        float rbFlat = new Vector2(rb.velocity.x, rb.velocity.z).magnitude; // aktualna prędkość płaska (XZ) rzeczywistego Rigidbody w grze.
        float emuFlat = (virtu != null) ?  //prędkość płaska z emulatora
            new Vector2(virtu.moveVector.x, virtu.moveVector.z).magnitude * emulatorSpeedScale : 0f;

        Vector3 p = transform.position; //pozycja gracza

        string dbg =
            $"EMU: {emuFlat:F2} m/s\n" +            //prędkość z emulatora
            $"RB : {rbFlat:F2} m/s\n" +             //faktyczna prędkość gracza z fizyki.
            $"Pos XZ: [{p.x:F1}, {p.z:F1}] m\n" +   //pozycja po płaskich osiach, w metrach
            $"Grounded: {grounded}\n" +             //groundcheck
            $"Y pos: {p.y:F2}\n" +                  //wysokość Y
            $"Y vel: {rb.velocity.y:F2}";           //pionowa prędkość Y - używana do naprawy bumpów

        if (debugText) debugText.text = dbg;   // HUD FPS
        if (debugText2) debugText2.text = dbg;   // HUD TopDown

    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void ReadInput()
    {
        if (!useEmulator) //gdy ustawiona klawiatura - aktualnie nie używane (było do nauki ruchu)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
        }
        else
        {
            // opcjonalny obrót w miejscu z emulatora
            if (yawFromEmulator && virtu != null)
            {
                float yawDegPerSec = virtu.verticalRotation * Mathf.Rad2Deg * yawMultiplier; // zamiana z rad/s na deg/s
                orientation.Rotate(0f, yawDegPerSec * Time.deltaTime, 0f, Space.World);
            }
        }
    }

    private void MovePlayer()
    {
        if (useEmulator)
        {
            // prędkość XZ z emulatora
            Vector3 mv = (virtu != null) ? virtu.moveVector * emulatorSpeedScale : Vector3.zero;

            // STAŁY OBRÓT RUCHU (np. +90° / -90°)
            if (Mathf.Abs(emulatorYawOffsetDeg) > 0.01f)
            {
                Quaternion rot = Quaternion.Euler(0f, emulatorYawOffsetDeg, 0f);
                mv = rot * mv;
            }

            // opcjonalny limit prędkości z emulatora
            if (maxEmuSpeedMS > 0f)
            {
                float flat = new Vector2(mv.x, mv.z).magnitude;
                if (flat > maxEmuSpeedMS)
                {
                    Vector2 flat2 = new Vector2(mv.x, mv.z).normalized * maxEmuSpeedMS;
                    mv.x = flat2.x; mv.z = flat2.y;
                }
            }

            rb.velocity = new Vector3(mv.x, rb.velocity.y, mv.z);
            return;
        }

        // klawiatura – jak było kiedyś do uczenia sie ruchu
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        rb.AddForce(moveDirection.normalized * keyboardMoveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl() //limit predkosci klawiatury - nieużywane 
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > keyboardMoveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * keyboardMoveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }
    

}
