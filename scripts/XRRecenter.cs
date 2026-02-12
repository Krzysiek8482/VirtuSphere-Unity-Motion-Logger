using UnityEngine;

public class XRRecenter : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;     
    public Transform hmd;          
    public Transform playerBody;   

    [Header("Settings")]
    public bool ignoreVertical = true;  

    [Header("Auto recenter")]
    public bool autoRecenter = false;        // czy automatyczne centrowanie
    public float autoRecenterInterval = 5f;  //   interwał automatycznego centrowania [s]
    private float autoTimer = 0f;            // timer

    void Update()
    {
        // manualne centrowanie (np. F7)
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Recenter();
        }

        // automatyczne centrowanie
        if (autoRecenter)
        {
            autoTimer += Time.unscaledDeltaTime;

            if (autoTimer >= autoRecenterInterval)
            {
                autoTimer = 0f;
                Recenter();
            }
        }
    }

    public void Recenter()
    {
        if (xrOrigin == null || hmd == null || playerBody == null)
        {
            Debug.LogWarning("[XRRecenter] Brak referencji!");
            return;
        }

        Vector3 offset = hmd.position - playerBody.position;

        if (ignoreVertical)
            offset.y = 0f;

        xrOrigin.position -= offset;

        Debug.Log($"[XRRecenter] Recenter applied. Offset: {offset}");
    }
}
