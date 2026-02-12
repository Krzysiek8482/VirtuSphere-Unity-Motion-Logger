using UnityEngine;

public class TopDownFollow : MonoBehaviour
{
    public Transform target;                 // target do śledzenia
    public Vector3 offset = new Vector3(0, 12f, -12f); // wysokość + odsunięcie
    [Range(0f, 89f)] public float pitch = 45f; // nachylenie w dół
    public float yaw = 0f;                   // stały kierunek „na północ”
    public float followSmooth = 8f;          // płynność śledzenia

    void LateUpdate()
    {
        if (!target) return;
        // pozycja: cel + offset
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSmooth);
        // rotacja: pitch + yaw
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
