using UnityEngine;
[ExecuteAlways]
[RequireComponent(typeof(SegmentDescriptor))]
[RequireComponent(typeof(LineRenderer))]
public class SegmentCenterLine : MonoBehaviour
{
    public SegmentDescriptor segment;

    [Header("Wygląd linii")]
    [Range(2, 64)] public int samples = 24;
    public Material lineMaterial;       // materiał ustawiany w Inspectorze
    public float lineWidth = 0.05f;     // grubość linii
    public float lineHeightOffset = 0.02f; //podniesienie linii nad podłożem

    LineRenderer lr;

    void Reset()
    {
        lr = GetComponent<LineRenderer>();
        if (!segment) segment = GetComponentInParent<SegmentDescriptor>();

        lr.useWorldSpace = true;
        lr.positionCount = samples;

        if (lineMaterial)
            lr.sharedMaterial = lineMaterial;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
    }

    void OnValidate()
    {
        if (!segment) segment = GetComponentInParent<SegmentDescriptor>();
        if (!lr) lr = GetComponent<LineRenderer>();

        if (lr != null)
        {
            lr.positionCount = samples;
            if (lineMaterial)
                lr.sharedMaterial = lineMaterial;

            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
        }
    }

    void LateUpdate()
    {
        if (!segment || !segment.startPose || !segment.endPose) return;
        if (!lr) lr = GetComponent<LineRenderer>();

        lr.positionCount = samples;

        if (lineMaterial && lr.sharedMaterial != lineMaterial)
            lr.sharedMaterial = lineMaterial;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        float yOffset = segment.startPose.position.y + lineHeightOffset;

        if (!segment.IsTurn)
        {
            //Prosty odcinek
            Vector3 S = segment.startPose.position + Vector3.up * lineHeightOffset;
            Vector3 E = segment.endPose.position + Vector3.up * lineHeightOffset;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                lr.SetPosition(i, Vector3.Lerp(S, E, t));
            }
            return;
        }

        //Zakręt (łuk)
        Vector3 C = segment.arcCenter;
        float R = segment.radius;
        if (R <= 0.0001f) return;

        Vector3 S3 = segment.startPose.position;
        Vector2 CS = new Vector2(S3.x - C.x, S3.z - C.z).normalized;

        float totalRad = Mathf.Deg2Rad * segment.angleDeg;
        float dirSign = SegmentDescriptor.IsLeft(segment.kind) ? +1f : -1f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            float theta = dirSign * t * totalRad;

            float cos = Mathf.Cos(theta);
            float sin = Mathf.Sin(theta);
            Vector2 rotated = new Vector2(
                CS.x * cos - CS.y * sin,
                CS.x * sin + CS.y * cos);

            Vector3 p = new Vector3(
                C.x + rotated.x * R,
                yOffset,                                  //offset wysokości
                C.z + rotated.y * R);

            lr.SetPosition(i, p);
        }
    }
}
