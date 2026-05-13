using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Component.Transforming;

namespace DarkOrbit.Networking
{
    /// <summary>
    /// Controlador de nave con autoridad del servidor (Server Authoritative)
    /// </summary>
    public class NetworkShipController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 50f;
        public float rotationSpeed = 100f;

        // Referencia al componente de sincronización de FishNet
        private NetworkTransform _networkTransform;

        private void Awake()
        {
            _networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Solo habilitamos el input si somos el dueño de esta nave
            if (IsOwner)
            {
                gameObject.name = "LocalPlayerShip";
                gameObject.tag = "Player";
                
                // Buscar la cámara y asignarle el target
                var camController = Camera.main?.GetComponent<DO.Controllers.FollowCameraController>();
                if (camController != null)
                {
                    camController.SetTarget(transform);
                }
            }
            else
            {
                enabled = false;
            }
        }

        private void Update()
        {
            // Solo procesamos input si somos el dueño de esta nave
            if (!IsOwner) return;

            // 1. MOVIMIENTO
            float move = Input.GetAxis("Vertical");
            float rotate = Input.GetAxis("Horizontal");

            if (move != 0 || rotate != 0)
            {
                CmdMove(move, rotate);
            }

            // 2. ACTIVAR HABILIDAD (Tecla 1)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var shield = GetComponent<AbilityPrismaticShield>();
                if (shield != null)
                {
                    // Alternamos el escudo (si está activo lo apaga, si no lo activa)
                    shield.CmdToggleShield(!shield.isShieldActive.Value);
                }
            }

            // 3. SIMULAR DAÑO (Tecla K para testeo)
            if (Input.GetKeyDown(KeyCode.K))
            {
                var health = GetComponent<NetworkHealth>();
                if (health != null)
                {
                    Debug.Log("[TEST] Recibiendo 50,000 de daño...");
                    health.CmdApplyDamage(50000f);
                }
            }
        }

        /// <summary>
        /// Este comando se ejecuta estrictamente en el servidor.
        /// </summary>
        [ServerRpc]
        private void CmdMove(float horizontal, float vertical)
        {
            // Movimiento directo en los ejes X e Y
            Vector3 moveDir = new Vector3(horizontal, vertical, 0);
            if (moveDir.magnitude > 1f) moveDir.Normalize();
            
            transform.position += moveDir * moveSpeed * Time.deltaTime;
            
            // Sin rotación en Z por ahora, como solicitó el usuario
        }
    }
}
