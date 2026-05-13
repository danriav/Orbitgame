using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

// Namespace de combate — esta clase es marcador de objetivos válidos
namespace DarkOrbit.Combat
{
    /// <summary>
    /// Componente que marca una entidad (Nave, Alien, Recurso) como objetivo
    /// seleccionable. Ahora incluye resistencias por tipo de daño y
    /// stacks de corrosión del Particle Cannon Nidhogg.
    /// </summary>
    public class TargetableEntity : NetworkBehaviour
    {
        [Header("Información del Objetivo")]
        public string entityName  = "Desconocido";
        public bool   isHostile   = true;
        public bool   isNPC       = false;   // true para aliens, false para jugadores

        [Header("Resistencias (0 = sin resistencia, 1 = inmune)")]
        [Range(0f, 0.9f)]
        [Tooltip("Resistencia al daño Thermal — Particle Cannon Helios")]
        public float thermalResistance   = 0f;

        [Range(0f, 0.9f)]
        [Tooltip("Resistencia al daño Corrosivo — Particle Cannon Nidhogg")]
        public float corrosiveResistance = 0f;

        [Range(0f, 0.9f)]
        [Tooltip("Resistencia al daño Eléctrico — Particle Cannon Indra")]
        public float electroResistance   = 0f;

        // ── Stacks de corrosión (sincronizados, aplica Nidhogg) ───────────────
        public readonly SyncVar<int>   corrosiveStacks         = new SyncVar<int>(0);
        public readonly SyncVar<float> corrosiveStackExpiry    = new SyncVar<float>(0f);

        // ── Estado ────────────────────────────────────────────────────────────
        public readonly SyncVar<bool>  isFrozen               = new SyncVar<bool>(false);
        public readonly SyncVar<float> speedReductionPercent  = new SyncVar<float>(0f);

        private void Update()
        {
            // Expiración automática de stacks corrosivos en el servidor
            if (!IsServerInitialized) return;
            if (corrosiveStacks.Value > 0 && Time.time >= corrosiveStackExpiry.Value)
            {
                corrosiveStacks.Value = 0;
                Debug.Log($"[COMBAT] Stacks de corrosión de {entityName} expirados.");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API PÚBLICA
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Añade un stack de vulnerabilidad corrosiva (Nidhogg).
        /// Máximo 5 stacks. Cada disparo reinicia la duración.
        /// </summary>
        [Server]
        public void AddCorrosiveStack(int stacks, float duration)
        {
            int maxStacks = 5;
            corrosiveStacks.Value       = Mathf.Min(corrosiveStacks.Value + stacks, maxStacks);
            corrosiveStackExpiry.Value  = Time.time + duration;

            Debug.Log($"[NIDHOGG] {entityName}: {corrosiveStacks.Value}/{maxStacks} stacks corrosivos " +
                      $"(+{corrosiveStacks.Value * 5f}% daño corrosivo recibido)");
        }

        /// <summary>Resistencia corrosiva efectiva (reducida por stacks Nidhogg).</summary>
        public float GetEffectiveCorrosiveResistance(float stackBonus)
        {
            float reduction = corrosiveStacks.Value * stackBonus;
            return Mathf.Max(0f, corrosiveResistance - reduction);
        }
    }
}