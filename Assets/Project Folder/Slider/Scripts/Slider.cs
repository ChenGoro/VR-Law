#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class Slider : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("number of intervals. minimum 1)")]
    [Min(1)]
    public int NumOfSteps = 10;
    public float MinValue = 0f;
    public float MaxValue = 1f;

    [SerializeField, ReadOnly] private float StepSize => (MaxValue - MinValue) / NumOfSteps;
    [SerializeField] private string valueFormat = "F2";
    [SerializeField] private string valueUnitSymbol = "$";
    [Range(0f, 1f)]
    [SerializeField] private float currentNormalized = 0f;
    [ReadOnly] public float CurrentValue = 0f;

    public bool ShowStepMarks = true;
    public bool AllowContinuousValues = false;

    [SerializeField] private bool hideOnAwake = false;

    [Header("References")]
    [SerializeField] private GameObject stepMarkPrefab;
    [SerializeField] private Transform stepMarkParent;
    [SerializeField] private Transform minPoint;
    [SerializeField] private Transform maxPoint;
    [SerializeField] private TextMeshPro minPointText; // Optional: for displaying min value
    [SerializeField] private TextMeshPro maxPointText; // Optional: for displaying max value
    [SerializeField] private Transform valueMark; //or: slider handle
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private TextMeshPro valueText;
    [SerializeField] private BoxCollider lineCollider;
    [SerializeField] private float colliderHeight = 0.02f;
    [SerializeField] private float colliderDepth = 0.02f;

    [SerializeField, HideInInspector]
    private List<GameObject> stepMarks = new List<GameObject>();
#if UNITY_EDITOR
    private bool _pendingEditorRegen = false;
#endif
    private bool _isRebuilding = false;
    // Track last-known endpoint positions to detect motion in edit/play
    private Vector3 _prevMinPos;
    private Vector3 _prevMaxPos;
    private const float _posEpsilonSqr = 1e-8f;

    #region Unity LifeCycle
    private void Awake()
    {
        if (hideOnAwake)
        {
            gameObject.SetActive(false);
        }
    }


    private void Start()
    {
        if (stepMarkPrefab == null || minPoint == null || maxPoint == null || valueMark == null || lineRenderer == null)
        {
            Debug.LogError("Slider is not properly configured. Please assign all required references.");
            return;
        }
        DrawLine();
        UpdateCollider();


        UpdateStepMarks();

        SetValue(CurrentValue);

        if (stepMarkParent == null)
        {
            stepMarkParent = transform; // Use the slider's transform if no parent is assigned
        }
    }

    private void Update()
    {
        if (minPoint == null || maxPoint == null) return;

        var a = minPoint.position;
        var b = maxPoint.position;

        if ((a - _prevMinPos).sqrMagnitude > _posEpsilonSqr ||
            (b - _prevMaxPos).sqrMagnitude > _posEpsilonSqr)
        {
            OnEndpointsMoved();
            _prevMinPos = a;
            _prevMaxPos = b;
        }
    }

    private void OnEnable()
    {
        if (minPoint != null) _prevMinPos = minPoint.position;
        if (maxPoint != null) _prevMaxPos = maxPoint.position;
    }

    private void OnValidate()
    {
        ApplyFromNormalized(currentNormalized); // snaps if needed, or stays continuous

        if (minPoint == null || maxPoint == null || lineRenderer == null) return;
        if (stepMarkParent == null) stepMarkParent = transform;

        DrawLine();
        UpdateCollider();
        RequestRegenerateStepMarks();
        UpdateEndValueTexts();
    }

    #endregion

    #region Update visual elements
    private void UpdateStepMarks()
    {
        RequestRegenerateStepMarks();
    }

    private void RequestRegenerateStepMarks()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_pendingEditorRegen) return;
            _pendingEditorRegen = true;

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;            // object might be gone
                _pendingEditorRegen = false;
                RegenerateStepMarksNow();            // safe point (not in OnValidate)
                EditorUtility.SetDirty(this);
                if (gameObject != null && gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
            };
            return;
        }
#endif
        // Play mode: do it immediately (uses Destroy)
        RegenerateStepMarksNow();
    }


    private void RegenerateStepMarksNow()
    {
        if (_isRebuilding) return;
        _isRebuilding = true;

        // Ensure parent exists
        if (stepMarkParent == null) stepMarkParent = transform;

        ClearStepMarksNow();

        if (!ShowStepMarks)
        {
            _isRebuilding = false;
            return; // No step marks to create
        }

        if (stepMarkPrefab == null || stepMarkParent == null || minPoint == null || maxPoint == null)
        {
            _isRebuilding = false;
            return;
        }

        for (int i = 0; i < NumOfSteps; i++)
        {
            float stepValue = MinValue + (i * StepSize);
            float t = (MaxValue - MinValue) > 0f ? (stepValue - MinValue) / (MaxValue - MinValue) : 0f;
            Vector3 pos = Vector3.Lerp(minPoint.position, maxPoint.position, t);

            GameObject mark;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                mark = (GameObject)PrefabUtility.InstantiatePrefab(stepMarkPrefab, stepMarkParent);
                mark.transform.position = pos;
            }
            else
#endif
            {
                mark = Instantiate(stepMarkPrefab, pos, Quaternion.identity, stepMarkParent);
            }

            mark.name = $"StepMark_{i}";
            stepMarks.Add(mark);
        }

        _isRebuilding = false;
    }

    private void ClearStepMarksNow()
    {
        if (stepMarks == null || stepMarks.Count == 0) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Edit mode: must be immediate (but NOT from OnValidate)
            foreach (var go in stepMarks)
                if (go) DestroyImmediate(go);
        }
        else
#endif
        {
            // Play mode: normal deferred destroy
            foreach (var go in stepMarks)
                if (go) Destroy(go);
        }
        stepMarks.Clear();
    }

    private void OnEndpointsMoved()
    {
        DrawLine();
        UpdateCollider();
        if (ShowStepMarks)
            RepositionStepMarksOrRebuild(); // cheap when possible

        // Keep handle/text in sync with new endpoints
        SetValue(CurrentValue);
    }

    private void RepositionStepMarksOrRebuild()
    {
        if (stepMarks == null) stepMarks = new List<GameObject>();

        // If the list is invalid (wrong count or nulls), rebuild via your safe path
        bool needsRebuild = stepMarks.Count != NumOfSteps || stepMarks.Exists(m => m == null);
        if (needsRebuild)
        {
            RequestRegenerateStepMarks();
            return;
        }

        // Fast path: just move them
        for (int i = 0; i < NumOfSteps; i++)
        {
            // Using value-space spacing; identical to your Create loop:
            float stepValue = MinValue + (i * StepSize);
            float t = (MaxValue - MinValue) > 0f ? (stepValue - MinValue) / (MaxValue - MinValue) : 0f;
            Vector3 pos = Vector3.Lerp(minPoint.position, maxPoint.position, t);
            var mark = stepMarks[i];
            if (mark != null) mark.transform.position = pos;
        }
    }



    private void UpdateValueText()
    {
        if (valueText != null)
        {
            valueText.text = CurrentValue.ToString(valueFormat) + valueUnitSymbol;
        }
    }

    public void DrawLine()
    {
        lineRenderer.positionCount = 2;
        if (minPoint != null && maxPoint != null)
        {
            lineRenderer.SetPosition(0, minPoint.position);
            lineRenderer.SetPosition(1, maxPoint.position);
        }
        else
        {
            Debug.LogError("Min or Max point is not assigned for the line renderer.");
        }
    }

    public void UpdateCollider()
    {
        if (lineCollider == null || minPoint == null || maxPoint == null)
        {
            Debug.LogError("Collider or points not assigned.");
            return;
        }

        // Work in world space
        Vector3 start = minPoint.position;
        Vector3 end = maxPoint.position;
        Vector3 mid = (start + end) / 2f;
        Vector3 dir = (end - start).normalized;
        float length = Vector3.Distance(start, end);

        // Ensure collider is a child of lineRenderer transform
        if (lineCollider.transform.parent != lineRenderer.transform)
        {
            lineCollider.transform.SetParent(lineRenderer.transform, false);
        }

        // Reset local scale in case it was skewed before
        lineCollider.transform.localScale = Vector3.one;

        // Set collider world position/rotation, but keep relative to lineRenderer
        lineCollider.transform.position = mid;
        if (dir != Vector3.zero)
            lineCollider.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);

        // Size: long axis = X, thin height/depth from inspector
        lineCollider.size = new Vector3(length, colliderHeight, colliderDepth);
    }


    private void UpdateValueMark()
    {
        Vector3 newPosition = Vector3.Lerp(minPoint.position, maxPoint.position, (CurrentValue - MinValue) / (MaxValue - MinValue));
        valueMark.position = newPosition;
    }


    private void UpdateEndValueTexts()
    {
        if (minPointText != null)
            minPointText.text = MinValue.ToString(valueFormat) + valueUnitSymbol;

        if (maxPointText != null)
            maxPointText.text = MaxValue.ToString(valueFormat) + valueUnitSymbol;
    }
    #endregion 


    public void SetValue(float newValue)
    {
        ApplyValue(newValue);
    }

    private float SnapToStep(float v)
    {
        if (MaxValue <= MinValue)
            return MinValue;

        v = Mathf.Clamp(v, MinValue, MaxValue);

        if (AllowContinuousValues || NumOfSteps <= 0)
            return v;

        // number of intervals, so each step is this size:
        float stepSize = (MaxValue - MinValue) / NumOfSteps;

        // find nearest step index
        int stepIndex = Mathf.RoundToInt((v - MinValue) / stepSize);

        return MinValue + stepIndex * stepSize;
    }


    private void ApplyFromNormalized(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        float raw = Mathf.Lerp(MinValue, MaxValue, t01);
        ApplyValue(raw); // centralize snapping + visuals
    }

    private void ApplyValue(float newValue)
    {
        // clamp, then snap only if needed
        float v = Mathf.Clamp(newValue, MinValue, MaxValue);
        if (!AllowContinuousValues)
        {
            v = SnapToStep(v);
        }

        CurrentValue = v;

        // reflect result back to normalized so the Inspector shows where it landed
        currentNormalized = (MaxValue > MinValue)
            ? Mathf.InverseLerp(MinValue, MaxValue, CurrentValue)
            : 0f;

        UpdateValueMark();
        UpdateValueText();
    }

}
