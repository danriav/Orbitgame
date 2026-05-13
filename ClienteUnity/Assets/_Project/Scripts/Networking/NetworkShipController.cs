using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Component.Transforming;

namespace DarkOrbit.Networking
{
    public class NetworkShipController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 50f; 
        
        [Header("Network Settings")]
        [Tooltip("Cada cuántos segundos se envía una actualización al servidor mientras se mantiene presionado el clic")]
        public float dragUpdateRate = 0.15f; 
        private float _nextDragUpdateTime = 0f;
        
        [Header("Visual Settings")]
        [Tooltip("Velocidad a la que la nave gira hacia su destino")]
        public float rotationSpeed = 15f; 
        [Tooltip("Offset para corregir hacia dónde mira la 'nariz' del modelo 3D")]
        public float modelRotationOffset = -90f;

        private Vector3 _targetPosition;
        private bool _isMoving = false;
        private NetworkTransform _networkTransform;

        // Variables para la "Veleta" (Rotación visual)
        private Vector3 _lastPosition;

        private void Awake()
        {
            _networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Guardamos la posición inicial
            _lastPosition = transform.position;

            if (IsOwner)
            {
                gameObject.name = "LocalPlayerShip";
                gameObject.tag = "Player";
                
                var camController = Camera.main?.GetComponent<DO.Controllers.FollowCameraController>();
                if (camController != null)
                {
                    camController.SetTarget(transform);
                }
            }
        }

        private void Update()
        {
            // ----------------------------------------------------
            // 1. LÓGICA VISUAL CLIENT-SIDE (Para todas las naves)
            // ----------------------------------------------------
            HandleVisualRotation();

            // ----------------------------------------------------
            // 2. LÓGICA DEL CLIENTE LOCAL (Inputs)
            // ----------------------------------------------------
            if (IsOwner)
            {
                // Clic inicial
                if (Input.GetMouseButtonDown(0))
                {
                    UpdateDestination();
                }
                // Mantener presionado (limitado por Rate Limiting)
                else if (Input.GetMouseButton(0))
                {
                    if (Time.time >= _nextDragUpdateTime)
                    {
                        UpdateDestination();
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    var shield = GetComponent<AbilityPrismaticShield>();
                    if (shield != null) shield.CmdToggleShield(!shield.isShieldActive.Value);
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    var health = GetComponent<NetworkHealth>();
                    if (health != null) health.CmdApplyDamage(50000f);
                }
            }

            // ----------------------------------------------------
            // 3. LÓGICA DEL SERVIDOR (Movimiento real)
            // ----------------------------------------------------
            if (IsServer && _isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
                {
                    _isMoving = false;
                }
            }
        }

        /// <summary>
        /// Método de ayuda para procesar el input del mouse y enviarlo al servidor
        /// respetando el límite de spam (Rate Limit).
        /// </summary>
        private void UpdateDestination()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; 

            CmdSetDestination(mousePos);
            
            // Reiniciamos el temporizador para la próxima actualización permitida
            _nextDragUpdateTime = Time.time + dragUpdateRate;
        }

        /// <summary>
        /// Calcula y aplica la rotación basándose en el movimiento real de la nave.
        /// </summary>
        private void HandleVisualRotation()
        {
            Vector3 movementDelta = transform.position - _lastPosition;

            if (movementDelta.sqrMagnitude > 0.001f)
            {
                Vector3 direction = movementDelta.normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                Quaternion baseRotation = Quaternion.Euler(90f, 180f, 0f);
                Quaternion zRotation = Quaternion.Euler(0f, 0f, angle + modelRotationOffset);
                Quaternion targetRotation = zRotation * baseRotation;

                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            _lastPosition = transform.position;
        }

        [ServerRpc]
        private void CmdSetDestination(Vector3 destination)
        {
            _targetPosition = destination;
            _isMoving = true;
        }
    }
}