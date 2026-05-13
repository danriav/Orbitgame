using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using DarkOrbit.Combat;

namespace DarkOrbit.Networking
{
    /// <summary>
    /// Gestiona la salud de una nave en red:
    ///   - HP (Casco)      — barra verde
    ///   - Shield (Escudo) — barra azul    → absorbe primero
    ///   - NanoHull        — barra amarilla → absorbe después del escudo
    ///
    /// Patrón FishNet correcto:
    ///   Clientes llaman [ServerRpc] → el RPC llama al método [Server] interno.
    ///   El servidor llama directamente al método [Server] interno.
    /// </summary>
    public class NetworkHealth : NetworkBehaviour
    {
        // ── Stats Máximos ──────────────────────────────────────────────────────
        [Header("Salud Máxima (por defecto: Retiarus Plus)")]
        public readonly SyncVar<float> maxHealth   = new SyncVar<float>(425000f);
        public readonly SyncVar<float> maxShield   = new SyncVar<float>(300000f);
        public readonly SyncVar<float> maxNanoHull = new SyncVar<float>(100000f);

        // ── Valores Actuales ───────────────────────────────────────────────────
        public readonly SyncVar<float> currentHealth   = new SyncVar<float>(425000f);
        public readonly SyncVar<float> currentShield   = new SyncVar<float>(300000f);
        public readonly SyncVar<float> currentNanoHull = new SyncVar<float>(100000f);

        // ── Eventos para el HUD ────────────────────────────────────────────────
        public event System.Action<float, float, float, float, float, float> OnStatsUpdate;

        // ══════════════════════════════════════════════════════════════════════
        //  CICLO DE VIDA
        // ══════════════════════════════════════════════════════════════════════

        public override void OnStartClient()
        {
            base.OnStartClient();
            currentHealth.OnChange   += OnAnyStatChanged;
            currentShield.OnChange   += OnAnyStatChanged;
            currentNanoHull.OnChange += OnAnyStatChanged;
            FireStatsEvent();
        }

        private void OnAnyStatChanged(float prev, float next, bool asServer) => FireStatsEvent();

        private void FireStatsEvent()
            => OnStatsUpdate?.Invoke(
                currentHealth.Value,   maxHealth.Value,
                currentShield.Value,   maxShield.Value,
                currentNanoHull.Value, maxNanoHull.Value);

        /// <summary>Fuerza un disparo del evento (para suscripción tardía del HUD).</summary>
        public void FireInitialEvent() => FireStatsEvent();

        // ══════════════════════════════════════════════════════════════════════
        //  API PÚBLICA — el servidor llama estos métodos [Server] directamente.
        //  Los clientes deben usar los [ServerRpc] equivalentes.
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica daño distribuido (escudo/nano/casco) — llamar desde el servidor.
        /// </summary>
        [Server]
        public void ServerApplyDamage(float shieldDmg, float nanoDmg, float hullDmg)
        {
            float so = ApplyToStat(currentShield.Value, shieldDmg, maxShield.Value, out float newShield);
            currentShield.Value = newShield;

            float no = ApplyToStat(currentNanoHull.Value, nanoDmg + so, maxNanoHull.Value, out float newNano);
            currentNanoHull.Value = newNano;

            ApplyToStat(currentHealth.Value, hullDmg + no, maxHealth.Value, out float newHealth);
            currentHealth.Value = newHealth;

            if (currentHealth.Value <= 0f)
                OnShipDestroyed();
        }

        /// <summary>Aplica daño simple (distribuido automáticamente) — llamar desde el servidor.</summary>
        [Server]
        public void ServerApplyFlatDamage(float amount)
        {
            ServerApplyDamage(amount * 0.40f, amount * 0.15f, amount * 0.45f);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  [ServerRpc] — para uso desde CLIENTES (p.ej. debug, habilidades propias)
        // ══════════════════════════════════════════════════════════════════════

        [ServerRpc(RequireOwnership = false)]
        public void CmdApplyDamage(float amount)
        {
            ServerApplyFlatDamage(amount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdHeal(float amount)
        {
            currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth.Value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdRestoreShield(float amount)
        {
            currentShield.Value = Mathf.Min(currentShield.Value + amount, maxShield.Value);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private float ApplyToStat(float current, float damage, float max, out float newValue)
        {
            if (damage <= 0f) 
            {
                newValue = current;
                return 0f;
            }
            newValue = current - damage;
            if (newValue < 0f)
            {
                float overflow = -newValue;
                newValue = 0f;
                return overflow;
            }
            newValue = Mathf.Min(newValue, max);
            return 0f;
        }

        [Server]
        private void OnShipDestroyed()
        {
            Debug.Log($"[SERVER] <color=red>Nave {gameObject.name} destruida.</color>");
            // TODO: Spawn wreckage, notificar kills, respawn timer
        }
    }
}
