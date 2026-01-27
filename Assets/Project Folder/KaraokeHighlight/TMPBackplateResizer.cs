using NaughtyAttributes;
using TMPro;
using UnityEngine;


/// <summary>
/// Resizes a background mesh (e.g., Quad) to match the bounds of a 3D TextMeshPro text.
/// Also draws gizmos showing the detected bounds.
/// </summary>
[ExecuteAlways]
public class TMPBackplateResizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("3D TextMeshPro component (NOT TextMeshProUGUI).")]
    public TextMeshPro tmp;

    [Tooltip("Background mesh (Quad/Plane) positioned behind the text.")]
    public Transform backplate;

    [Header("Padding (local units)")]
    [Tooltip("Adds extra space around the text bounds. X = horizontal, Y = vertical.")]
    public Vector2 padding = new Vector2(0.1f, 0.05f);

    [Header("Options")]
    [Tooltip("Automatically resize the backplate when Awake() is called.")]
    public bool resizeOnAwake = true;

    [Tooltip("Continuously update bounds every frame. Useful for debugging.")]
    public bool resizeEveryFrame = false;

    // Cached last computed bounds for drawing gizmos
    private Bounds lastBounds;

    private void Reset()
    {
        // Auto-fill references when the script is first added
        tmp = GetComponent<TextMeshPro>();
        if (tmp != null && backplate == null && transform.childCount > 0)
        {
            backplate = transform.GetChild(0);
        }
    }

    private void Awake()
    {
        if (resizeOnAwake)
            UpdateBackplate();
    }

    private void Update()
    {
        if (resizeEveryFrame)
            UpdateBackplate();
    }

    /// <summary>
    /// Manual button you can use in the Inspector via right-click -> Resize Backface.
    /// </summary>
    [Button]
    public void ResizeBackface()
    {
        UpdateBackplate();
    }

    /// <summary>
    /// Optional UI button for NaughtyAttributes users:
    /// </summary>
    /*
    [Button("Resize Backface")]
    private void ButtonResizeBackface()
    {
        UpdateBackplate();
    }
    */

    /// <summary>
    /// The core logic: computes the text bounds and resizes the backplate to fit.
    /// </summary>
    public void UpdateBackplate()
    {
        if (tmp == null || backplate == null)
            return;

        // Ensure mesh data is up-to-date
        tmp.ForceMeshUpdate();

        // Text bounds in LOCAL SPACE of the TMP object
        Bounds b = tmp.bounds;
        lastBounds = b; // Save for gizmo drawing

        Vector3 size = b.size;
        Vector3 center = b.center;

        // Apply padding around the text
        float width = size.x + padding.x * 2f;
        float height = size.y + padding.y * 2f;

        // Scale backplate:
        // We assume the backplate mesh is a 1x1 square centered at (0,0)
        Vector3 scale = backplate.localScale;
        scale.x = width;
        scale.y = height;
        backplate.localScale = scale;

        // Position backplate behind the text
        Vector3 pos = backplate.localPosition;
        pos.x = center.x;
        pos.y = center.y;
        // Z position stays as-is (you control how far back it is)
        backplate.localPosition = pos;
    }

    /// <summary>
    /// Draw magenta gizmos showing the text bounds in the Scene View.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (tmp == null)
            return;

        Gizmos.color = Color.magenta;

        // Convert bounds from local to world space
        Matrix4x4 localToWorld = tmp.transform.localToWorldMatrix;

        // Extract bounds info
        Vector3 c = lastBounds.center;
        Vector3 s = lastBounds.size;

        // Define the 8 corners of the bounding box
        Vector3[] corners = new Vector3[8];
        Vector3 ext = s * 0.5f;

        corners[0] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, -ext.y, -ext.z));
        corners[1] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, -ext.y, -ext.z));
        corners[2] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, ext.y, -ext.z));
        corners[3] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, ext.y, -ext.z));

        corners[4] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, -ext.y, ext.z));
        corners[5] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, -ext.y, ext.z));
        corners[6] = localToWorld.MultiplyPoint3x4(c + new Vector3(ext.x, ext.y, ext.z));
        corners[7] = localToWorld.MultiplyPoint3x4(c + new Vector3(-ext.x, ext.y, ext.z));

        // Draw the 12 edges of the box
        DrawBoxEdge(0, 1, corners); DrawBoxEdge(1, 2, corners); DrawBoxEdge(2, 3, corners); DrawBoxEdge(3, 0, corners);
        DrawBoxEdge(4, 5, corners); DrawBoxEdge(5, 6, corners); DrawBoxEdge(6, 7, corners); DrawBoxEdge(7, 4, corners);
        DrawBoxEdge(0, 4, corners); DrawBoxEdge(1, 5, corners); DrawBoxEdge(2, 6, corners); DrawBoxEdge(3, 7, corners);
    }

    private void DrawBoxEdge(int a, int b, Vector3[] corners)
    {
        Gizmos.DrawLine(corners[a], corners[b]);
    }
}
