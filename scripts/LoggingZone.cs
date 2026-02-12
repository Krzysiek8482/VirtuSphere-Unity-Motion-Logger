using UnityEngine;


[RequireComponent(typeof(Collider))]
public class LoggingZone : MonoBehaviour
{
    [Header("Co logujemy")]
    public MotionLogger logger;      
    public string sessionTag = "T1"; 

    [Header("Zachowanie")]
    public bool stopOnExit = false;  

    void Reset()
    {
        
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // szukanie gracza po PlayerSegmentTracker 
        var tracker = other.GetComponentInParent<PlayerSegmentTracker>();
        if (!tracker) return;
        if (!logger) return;

        logger.sessionTag = sessionTag;
        logger.StartLogging();
        Debug.Log($"[LoggingZone] START logowania, tag={sessionTag}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!stopOnExit) return;

        var tracker = other.GetComponentInParent<PlayerSegmentTracker>();
        if (!tracker) return;
        if (!logger) return;

        logger.StopLogging();
        Debug.Log($"[LoggingZone] STOP logowania, tag={sessionTag}");
    }
}
