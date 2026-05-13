using UnityEngine;

namespace DarkOrbit.Combat
{
    /// <summary>
    /// ScriptableObject para Particle Cannons (Helios, Nidhogg, Indra).
    /// Se pueden equipar hasta 3 (uno por atributo).
    /// Crear via: DO > Combat > Particle Cannon
    /// </summary>
    [CreateAssetMenu(fileName = "NewParticleCannon", menuName = "DO/Combat/Particle Cannon")]
    public class ParticleCannonData : ScriptableObject
    {
        // ── Identidad ──────────────────────────────────────────────────────────
        [Header("Identidad")]
        public string          cannonId;
        public string          displayName;
        public ParticleAttribute attribute;
        public ParticleTier    tier;

        // ── Stats Base ─────────────────────────────────────────────────────────
        [Header("Estadísticas Base")]
        [Tooltip("Daño base en tier Common ~1660. Aumenta con tier.")]
        public float baseDamage   = 1660f;
        public float range        = 600f;
        public float reloadTime   = 4f;   // segundos de cooldown

        [Header("Críticos")]
        [Range(0f, 1f)] public float critChance     = 0.10f;   // 10%
        [Range(1f, 3f)] public float critMultiplier = 1.15f;   // +15%

        [Header("Varianza de Daño")]
        [Range(0f, 1f)] public float minDamagePercent = 0.75f;
        [Range(0f, 1f)] public float maxDamagePercent = 1.00f;

        // ── Helios (Thermal) ───────────────────────────────────────────────────
        [Header("Helios — Thermal (AoE Splash)")]
        [Tooltip("Radio de splash en unidades de mapa")]
        public float splashRadius        = 275f;
        [Range(0f, 1f)]
        [Tooltip("40% del daño principal a objetivos secundarios")]
        public float splashDamagePercent = 0.40f;
        public int   maxSplashTargets    = 5;

        // ── Nidhogg (Corrosive) ────────────────────────────────────────────────
        [Header("Nidhogg — Corrosive (Vulnerability Stacks)")]
        [Range(0f, 0.2f)]
        [Tooltip("5% de aumento en daño corrosivo recibido por stack")]
        public float corrosiveStackBonus = 0.05f;
        public int   maxCorrosiveStacks  = 5;
        [Tooltip("Duración de cada stack en segundos")]
        public float corrosiveStackDuration = 20f;

        // ── Indra (Electro) ────────────────────────────────────────────────────
        [Header("Indra — Electro (Chain Effect)")]
        [Tooltip("Cantidad de objetivos adicionales que encadena")]
        public int   chainTargets        = 3;
        [Range(0f, 1f)]
        [Tooltip("60% del daño principal a objetivos encadenados")]
        public float chainDamagePercent  = 0.60f;
        [Tooltip("Radio de búsqueda para el efecto de cadena")]
        public float chainRange          = 300f;

        // ── Visual ─────────────────────────────────────────────────────────────
        [Header("Visual")]
        [Tooltip("Helios=naranja, Nidhogg=verde ácido, Indra=cian eléctrico")]
        public Color beamColor = Color.cyan;
        [Range(0.05f, 0.5f)]
        public float beamWidth = 0.12f;

        // ── Helper: Daño base según tier ────────────────────────────────────────
        /// <summary>
        /// Devuelve el daño base ajustado según el tier del cannon.
        /// Los multiplicadores son aproximados al diseño oficial.
        /// </summary>
        public float GetTieredBaseDamage()
        {
            return tier switch
            {
                ParticleTier.Common      => baseDamage,
                ParticleTier.Uncommon    => baseDamage * 1.15f,
                ParticleTier.Rare        => baseDamage * 1.35f,
                ParticleTier.Epic        => baseDamage * 1.60f,
                ParticleTier.Masterpiece => baseDamage * 1.90f,
                ParticleTier.Legendary   => baseDamage * 2.30f,
                ParticleTier.Ultimate    => baseDamage * 2.80f,
                _                        => baseDamage,
            };
        }
    }
}
