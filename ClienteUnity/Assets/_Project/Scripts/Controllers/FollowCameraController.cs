using UnityEngine;

namespace DO.Controllers
{
    /// <summary>
    /// Cámara de seguimiento cenital (top-down) para la nave del jugador.
    /// Se posiciona directamente encima del target con un offset configurable.
    /// </summary>
    public class FollowCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("Transform de la nave a seguir")]
        private Transform _target;

        [Header("Position Settings")]
        [SerializeField, Tooltip("Altura de la cámara sobre la nave")]
        private float _height = 20f;

        [SerializeField, Tooltip("Offset lateral/frontal adicional (XZ)")]
        private Vector3 _positionOffset = Vector3.zero;

        [SerializeField, Tooltip("Suavizado del seguimiento (0 = instantáneo)")]
        [Range(0f, 0.5f)]
        private float _smoothTime = 0.12f;

        [Header("Rotation Settings")]
        [SerializeField, Tooltip("Ángulo de inclinación cenital (90 = perfectamente vertical)")]
        [Range(60f, 90f)]
        private float _pitchAngle = 85f;

        [SerializeField, Tooltip("Rotar la cámara con la nave")]
        private bool _followRotation = false;

        [Header("Field of View")]
        [SerializeField, Range(20f, 120f)]
        private float _fieldOfView = 60f;

        // ── Referencias ────────────────────────────────────────────────
        private Camera _cam;
        private Vector3 _currentVelocity;

        // ── Propiedades ────────────────────────────────────────────────
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        // ── Ciclo de vida Unity ────────────────────────────────────────
        private void Awake() => CacheReferences();
        private void Start()
        {
            ApplySettings();
            if (_target == null) FindPlayerTarget();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                // Intentar encontrar la nave si se ha perdido el objetivo
                GameObject player = GameObject.Find("PlayerShip");
                if (player != null) _target = player.transform;
                return;
            }
            FollowTarget();
        }

        private void FindPlayerTarget()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _target = player.transform;
        }

        // ── Métodos privados ───────────────────────────────────────────

        private void CacheReferences()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null)
                _cam = gameObject.AddComponent<Camera>();
        }

        private void ApplySettings()
        {
            _cam.fieldOfView = _fieldOfView;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 5000f;
        }

        private void FollowTarget()
        {
            if (_target == null) return;

            // En modo ortográfico, Z es solo para la profundidad de renderizado
            Vector3 desiredPosition = new Vector3(
                _target.position.x + _positionOffset.x,
                _target.position.y + _positionOffset.y,
                -50f // Distancia fija en Z para la cámara
            );

            // Suavizado de posición
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition,
                ref _currentVelocity, _smoothTime);

            // Mantener rotación 0,0,0 para vista frontal XY
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        /// <summary>
        /// Asigna el target de la cámara en runtime.
        /// </summary>
        public void SetTarget(Transform target) => _target = target;

        /// <summary>
        /// Teleporta la cámara al target sin suavizado (útil en respawn).
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            transform.position = new Vector3(
                _target.position.x + _positionOffset.x,
                _target.position.y + _positionOffset.y,
                -50f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _target.position);
            Gizmos.DrawWireSphere(_target.position, 1f);
        }
#endif
    }
}
