using UnityEngine;
using UnityEngine.XR.Management;

public class StartupConfig : MonoBehaviour
{
    [Header("Referencje")]
    public CameraSwitcher camSwitch;     // obiekt ze skryptem CameraSwitcher
    public Canvas canvasVR;              // HUD do VR
    public Canvas canvasTopDown;         // HUD do widoku z góry
    public GameObject xrOriginRoot;      // XR Origin (cały rig, ten z Main Camera)

    [Header("Ustawienia startowe")]
    [Tooltip("OFF = start w VR, ON = start w widoku z góry")]

    [Header("Kalibracja VirtuSphere")]
    public VirtuSphereController virtu;   // przeciągnij obiekt z VirtuSphereController
    public Transform calibrationForward;  // np. Player/Orientation albo XR Origin (kierunek "do przodu")
    public bool startTopDown = false;

    void Awake()
    {
        // na starcie ustaw tryb zgodnie z checkboxem
        Apply(startTopDown);
    }

    public void Apply(bool topDown)
    {
        bool useVR = !topDown;

        //Przełącz kamery przez CameraSwitcher
        if (camSwitch != null)
        {
            camSwitch.useTopDown = topDown;
            camSwitch.Apply();
        }

        // XR Origin włącz/wyłącz
        if (xrOriginRoot != null)
            xrOriginRoot.SetActive(useVR);

        // Canvasy
        if (canvasVR != null)
            canvasVR.gameObject.SetActive(useVR);

        if (canvasTopDown != null)
            canvasTopDown.gameObject.SetActive(topDown);

        // kursor myszy
        Cursor.lockState = topDown ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = topDown;

        // XR start/stop
        StopAllCoroutines();
        if (useVR)
            StartCoroutine(StartXR());
        else
            StartCoroutine(StopXR());
    }

    public void StartVRMode() => Apply(false);
    public void StartTopDownMode() => Apply(true);

    System.Collections.IEnumerator StartXR()
    {
        var settings = XRGeneralSettings.Instance;
        if (settings == null || settings.Manager == null) yield break;

        if (!settings.Manager.isInitializationComplete)
        {
            yield return settings.Manager.InitializeLoader();

            if (settings.Manager.activeLoader == null)
            {
                Debug.LogError("[StartupConfig] XR init FAILED (brak activeLoader)");
                yield break;
            }
        }

        settings.Manager.StartSubsystems();
        Debug.Log("[StartupConfig] XR START");
    }

    System.Collections.IEnumerator StopXR()
    {
        var settings = XRGeneralSettings.Instance;
        if (settings == null || settings.Manager == null) yield break;
        if (!settings.Manager.isInitializationComplete) yield break;

        settings.Manager.StopSubsystems();
        settings.Manager.DeinitializeLoader();
        Debug.Log("[StartupConfig] XR STOP");
    }

    void Update()
    {
        // F8 = kalibracja kierunku VirtuSphere
        if (Input.GetKeyDown(KeyCode.F8))
        {
            DoCalibration();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape)) // ESC = wyjście z aplikacji (w przypadku builda exe)
        {
            Application.Quit();
        }
    }

    void DoCalibration()
    {
        if (virtu == null)
        {
            Debug.LogWarning("[StartupConfig] Brak referencji do VirtuSphereController!");
            return;
        }

        float targetYaw = 0f;

        if (calibrationForward != null)
        {
            targetYaw = calibrationForward.eulerAngles.y - 90f;
        }

        // Zaokrąglenie do najbliższego kroku 90 stopni - wyznajemy tylko 4 główne kierunki do configu
        float snapStep = 90f;
        targetYaw = Mathf.Round(targetYaw / snapStep) * snapStep;

        virtu.CalibrateToCurrentDirection(targetYaw);
        Debug.Log($"[StartupConfig] Kalibracja F8: snapped targetYaw={targetYaw:F1}°");
    }


}
