using UnityEngine;

namespace DarkOrbit.Combat
{
    /// <summary>
    /// ScriptableObject para toda la munición del juego:
    /// baterías láser, rockets, minas y slugs de Particle Cannon.
    /// Crear via: DO > Combat > Ammo Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewAmmo", menuName = "DO/Combat/Ammo Data")]
    public class AmmoData : ScriptableObject
    {
        // ── Identidad ──────────────────────────────────────────────────────────
        [Header("Identidad")]
        [Tooltip("ID único: LCB-10, MCB-25, UCB-100, RSB-75, PLT-3030, EMP-01, etc.")]
        public string       ammoId;
        public string       displayName;
        public AmmoCategory category;
        public DamageType   damageType;

        // ── Baterías Láser ─────────────────────────────────────────────────────
        [Header("Batería Láser")]
        [Tooltip("Multiplicador aplicado al daño base total de los láseres equipados")]
        public float damageMultiplier = 1f;
        // LCB-10=1x | MCB-25=2x | MCB-50=3x | UCB-100=4x | RSB-75=6x
        // CBO-100=3x | JOB-100=3.5x (aliens) / 2x (pvp) | PIB-100=4x | RB-214=4x / 8x vs Demaner

        // ── Cohetes ────────────────────────────────────────────────────────────
        [Header("Cohete")]
        public float rocketBaseDamage = 1000f;
        // R-310=1000 | PLT-2026=2000 | PLT-2021=4000 | PLT-3030=6000
        public float rocketSpeed      = 250f;
        public float rocketRange      = 1000f;

        // ── Particle Cannon Slug ───────────────────────────────────────────────
        [Header("Particle Slug")]
        [Tooltip("Qué tipo de Particle Cannon debe estar equipado para usar este slug")]
        public ParticleAttribute requiredCannon;

        // ── Efectos Especiales ─────────────────────────────────────────────────
        [Header("Efectos Especiales")]
        [Tooltip("SAB-50, SAB-M01: el daño se aplica directamente al escudo")]
        public bool  absorbsShield       = false;

        [Tooltip("DD-M01: ignora escudo, daño directo al casco")]
        public bool  ignoresShield       = false;

        [Tooltip("El ammo aplica daño sobre el tiempo (DoT)")]
        public bool  appliesDoT          = false;
        public float dotDamagePerSecond  = 0f;
        public float dotDuration         = 0f;   // segundos

        [Tooltip("DCR-250: reduce velocidad del objetivo")]
        public bool  reducesSpeed        = false;
        [Range(0f, 1f)]
        public float speedReductionPercent = 0f;  // 0.30 = 30%
        public float speedEffectDuration   = 0f;  // segundos

        [Tooltip("R-IC3: congela al objetivo")]
        public bool  appliesFreeze       = false;
        public float freezeDuration      = 2f;

        [Tooltip("PLD-8: reduce precisión del objetivo")]
        public bool  reducesAccuracy     = false;
        [Range(0f, 1f)]
        public float accuracyReduction   = 0f;

        [Tooltip("EMP-01/M01: deshabilita sistemas del objetivo")]
        public bool  appliesEMP          = false;
        public float empDuration         = 3f;

        [Tooltip("SR-5: roba escudo del objetivo")]
        public bool  stealsShield        = false;
        [Range(0f, 1f)]
        public float shieldStealPercent  = 0.5f;

        [Tooltip("PIB-100: infecta a jugadores con esporas")]
        public bool  appliesSpore        = false;

        // ── AoE (Minas y efectos en área) ─────────────────────────────────────
        [Header("AoE")]
        public bool  isAoE              = false;
        public float aoeRadius          = 0f;
        [Range(0f, 1f)]
        public float aoeDamagePercent   = 1f;   // % del daño principal a objetivos secundarios

        // ── Visual ─────────────────────────────────────────────────────────────
        [Header("Visual")]
        public Color  projectileColor   = Color.red;
        public Sprite ammoIcon;

        // ── Helper ─────────────────────────────────────────────────────────────
        /// <summary>Retorna el multiplicador de daño real según el tipo de objetivo.</summary>
        public float GetMultiplierFor(bool targetIsNPC)
        {
            // JOB-100 tiene multiplicador distinto para NPC vs PvP
            if (ammoId == "JOB-100") return targetIsNPC ? 3.5f : 2f;
            // RB-214 vs Demaner Freighter x8, vs todo lo demás x4
            if (ammoId == "RB-214") return targetIsNPC ? 8f : 4f;
            return damageMultiplier;
        }
    }
}
