using UnityEngine;

public class PlayerAttackGuideLine
{
    private const float HeightOffset = 0.49f;
    private const float LineWidth = 0.08f;
    private readonly RaycastHit[] _wallHitBuffer = new RaycastHit[8];
    private Material _lineMaterial;
    private GameObject _lineObject;
    private LineRenderer _lineRenderer;

    public void Update(Vector3 playerPosition, Vector3 pointerPosition)
    {
        Vector3 lineStart = playerPosition;
        lineStart.y -= HeightOffset;

        Vector3 lineEnd = pointerPosition;
        lineEnd.y = lineStart.y;

        Vector3 direction = lineEnd - lineStart;
        float targetDistance = direction.magnitude;
        if (targetDistance <= Mathf.Epsilon)
        {
            Hide();
            return;
        }

        Ray attackRay = new Ray(lineStart, direction.normalized);
        int hitCount = Physics.RaycastNonAlloc(attackRay, _wallHitBuffer, targetDistance);
        float wallDistance = targetDistance;
        bool hasWall = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (!_wallHitBuffer[i].collider.CompareTag("Wall")) continue;
            if (_wallHitBuffer[i].distance < wallDistance)
            {
                wallDistance = _wallHitBuffer[i].distance;
                hasWall = true;
            }
        }

        if (hasWall)
        {
            lineEnd = lineStart + attackRay.direction * wallDistance;
        }

        EnsureLineRenderer();
        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, lineStart);
        _lineRenderer.SetPosition(1, lineEnd);
    }

    public void Hide()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = false;
    }

    public void Destroy()
    {
        if (_lineObject == null) return;
        Object.Destroy(_lineObject);
        _lineObject = null;
        _lineRenderer = null;
    }

    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null) return;

        _lineObject = new GameObject("PlayerAttackGuideLine");
        _lineRenderer = _lineObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = LineWidth;
        _lineRenderer.endWidth = LineWidth;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.enabled = false;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
        if (_lineMaterial == null && PlayerController.Instance != null)
        {
            _lineMaterial = PlayerController.Instance.LineMaterial;
        }
        if (_lineMaterial != null)
        {
            _lineRenderer.sharedMaterial = _lineMaterial;
        }
    }

    public void SetMaterial(Material material)
    {
        _lineMaterial = material;
        if (_lineRenderer == null) return;
        _lineRenderer.sharedMaterial = _lineMaterial;
    }
}
