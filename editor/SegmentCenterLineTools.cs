using UnityEditor;
using UnityEngine;

public static class SegmentCenterLineTools
{
    [MenuItem("Tools/Race Track/Show center lines")]
    public static void ShowCenterLines()
    {
        SetCenterLinesVisible(true);
    }

    [MenuItem("Tools/Race Track/Hide center lines")]
    public static void HideCenterLines()
    {
        SetCenterLinesVisible(false);
    }

    private static void SetCenterLinesVisible(bool visible)
    {
        // znajdź wszystkie SegmentCenterLine w aktualnie otwartej scenie
        var all = Object.FindObjectsOfType<SegmentCenterLine>(true); // true = także nieaktywne obiekty

        foreach (var scl in all)
        {
            if (scl == null) continue;

            // włącz / wyłącz sam LineRenderer
            var lr = scl.GetComponent<LineRenderer>();
            if (lr != null)
                lr.enabled = visible;

            // opcjonalnie wyłączamy też sam skrypt, żeby nie liczył w LateUpdate
            scl.enabled = visible;
        }

        Debug.Log($"SegmentCenterLine: {(visible ? "ON" : "OFF")} for {all.Length} segments");
    }
}
