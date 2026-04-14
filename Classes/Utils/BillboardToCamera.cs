using UnityEngine;

/// <summary>
/// Hace que un objeto (sprite o modelo 3D) mire siempre a la cámara.
/// Útil para "visuales" de proyectiles 3D (por ejemplo, notas musicales).
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    public enum BillboardMode
    {
        FaceCameraPosition,
        MatchCameraForward
    }

    [SerializeField] private UnityEngine.Camera targetCamera;
    [SerializeField] private BillboardMode mode = BillboardMode.FaceCameraPosition;
    [Tooltip("Si está activado, la rotación solo se ajusta en Y (evita inclinación arriba/abajo).")]
    [SerializeField] private bool lockYAxis = true;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        if (mode == BillboardMode.MatchCameraForward)
        {
            transform.forward = targetCamera.transform.forward;
            return;
        }

        Vector3 toCam = targetCamera.transform.position - transform.position;
        if (lockYAxis)
            toCam.y = 0f;

        if (toCam.sqrMagnitude < 0.000001f) return;

        // "Mirar" a cámara (por posición)
        transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }
}

