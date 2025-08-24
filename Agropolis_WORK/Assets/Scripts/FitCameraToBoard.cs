using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FitCameraToBoard : MonoBehaviour
{
    public Transform boardRoot;     // arrastra aquí el objeto Board
    public float padding = 0.25f;   // margen
    public bool autoUpdate = true;  // recalcular automáticamente

    Camera cam;
    Bounds lastBounds;
    int lastChildCount = -1;
    float lastAspect = -1f;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        TryFit();
    }

    void Start()
    {
        TryFit();
    }

    void LateUpdate()
    {
        if (!autoUpdate || boardRoot == null || cam == null) return;

        // Recalcular si cambió algo relevante
        int childCount = boardRoot.childCount;
        float aspect = cam.aspect;

        if (childCount != lastChildCount || Mathf.Abs(aspect - lastAspect) > 0.0001f)
            TryFit();
    }

    [ContextMenu("Fit Now")]
    public void TryFit()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (boardRoot == null) return;

        var renderers = boardRoot.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
        foreach (var r in renderers)
        {
            if (r.enabled && r.gameObject.activeInHierarchy)
                b.Encapsulate(r.bounds);
        }

        lastBounds = b;
        lastChildCount = boardRoot.childCount;
        lastAspect = cam.aspect;

        cam.orthographic = true;
        float aspect = Mathf.Max(0.0001f, cam.aspect);
        float sizeByHeight = b.extents.y;
        float sizeByWidth = b.extents.x / aspect;

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + padding;
        cam.transform.position = new Vector3(b.center.x, b.center.y, cam.transform.position.z);
    }
}
