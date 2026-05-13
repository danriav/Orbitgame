using UnityEngine;

namespace DO.Controllers
{
    /// <summary>
    /// Controlador de movimiento de la nave base del jugador.
    /// Usa el nuevo Input System de Unity 6 con WASD/flechas.
    /// Opera en el plano XZ (perspectiva cenital 3D).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipMovementController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField, Tooltip("Velocidad de traslación en unidades/segundo")]
        private float _moveSpeed = 10f;

        [SerializeField, Tooltip("Velocidad de rotación en grados/segundo")]
        private float _rotationSpeed = 200f;

        [SerializeField, Tooltip("Suavizado de aceleración (0 = instantáneo, 1 = muy suave)")]
        [Range(0f, 0.99f)]
        private float _movementSmoothing = 0.1f;

        // ── Referencias ────────────────────────────────────────────────
        private Rigidbody _rb;
        private Vector3 _smoothVelocity;
        private Vector3 _inputDirection;

        // ── Propiedades públicas ───────────────────────────────────────
        public float MoveSpeed => _moveSpeed;
        public bool IsMoving => _inputDirection.sqrMagnitude > 0.01f;

        // ── Ciclo de vida Unity ────────────────────────────────────────
        private void Awake() => CacheReferences();
        private void Start() => InitializeRigidbody();
        private void Update() => ReadInput();
        private void FixedUpdate() => ApplyMovement();

        // ── Métodos privados ───────────────────────────────────────────

        private void CacheReferences()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void InitializeRigidbody()
        {
            _rb.freezeRotation = true;
            _rb.linearDamping = 2f;
            _rb.useGravity = false;
        }

        private void ReadInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D o ←/→
            float vertical   = Input.GetAxisRaw("Vertical");   // W/S o ↑/↓

            _inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        }

        private void ApplyMovement()
        {
            if (_inputDirection.sqrMagnitude > 0.01f)
            {
                // Rotación hacia la dirección del movimiento
                Quaternion targetRotation = Quaternion.LookRotation(_inputDirection);
                _rb.MoveRotation(Quaternion.RotateTowards(
                    _rb.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));

                // Traslación suavizada
                Vector3 targetVelocity = _inputDirection * _moveSpeed;
                _rb.linearVelocity = Vector3.SmoothDamp(
                    _rb.linearVelocity, targetVelocity, ref _smoothVelocity,
                    _movementSmoothing, _moveSpeed * 2f, Time.fixedDeltaTime);
            }
            else
            {
                // Frenado suave al soltar teclas
                _rb.linearVelocity = Vector3.SmoothDamp(
                    _rb.linearVelocity, Vector3.zero, ref _smoothVelocity,
                    _movementSmoothing * 2f, _moveSpeed, Time.fixedDeltaTime);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
#endif
    }
}
