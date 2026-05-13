namespace DarkOrbit.Combat
{
    /// <summary>Tipo de daño que determina qué resistencias aplican y qué efectos secundarios ocurren.</summary>
    public enum DamageType
    {
        Standard,       // Daño láser estándar (LCB-10, MCB, UCB)
        ShieldAbsorb,   // SAB-50: drena escudo del objetivo
        Hybrid,         // CBO-100: x3 daño + medio SAB
        AlienBonus,     // JOB-100: x3.5 aliens / x2 jugadores
        Spore,          // PIB-100: x4 + infecta jugadores
        Thermal,        // Particle Cannon Helios — AoE splash
        Corrosive,      // Particle Cannon Nidhogg — stacks de resistencia
        Electro,        // Particle Cannon Indra — chain effect
        Rocket,         // Daño de cohete
        DirectDamage,   // Bypassea escudo (DD-M01 mine)
        EMP,            // EMP-01 / EMP-M01 — deshabilita sistemas
        Slow,           // SL-M01 / DCR-250 — efecto de velocidad
    }

    /// <summary>Categoría de munición para filtrar inventario.</summary>
    public enum AmmoCategory
    {
        LaserBattery,
        ParticleSlug,
        Rocket,
        Mine,
    }

    /// <summary>Atributo del Particle Cannon equipado.</summary>
    public enum ParticleAttribute
    {
        Helios,   // Thermal — AoE splash 40% hasta 5 targets, radio 275u
        Nidhogg,  // Corrosive — stacks de vulnerabilidad 5% hasta 5x, 20s
        Indra,    // Electro — cadena a 3 targets adicionales, 60% daño
    }

    /// <summary>Tier de calidad de un Particle Cannon (determina daño base).</summary>
    public enum ParticleTier
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Masterpiece,
        Legendary,
        Ultimate,
    }

    /// <summary>Tier de un cañón láser.</summary>
    public enum WeaponTier
    {
        Basic,    // LF-1, LF-2, MP-1
        Advanced, // LF-3, LF-4, SL-01/02/03
        Elite,    // LF-5 Mortifier, LF-5-AL, Prometheus, Asmodeus
        Special,  // Set-effect lasers (Odysseus, Caucasus)
    }

    /// <summary>
    /// Resultado de un ciclo de daño. El servidor lo calcula y lo usa
    /// para modificar HP/Shield/NanoHull del objetivo.
    /// </summary>
    public struct DamageResult
    {
        public float finalDamage;       // Daño total aplicado
        public float shieldDamage;      // Porción absorbida por el escudo
        public float nanoHullDamage;    // Porción absorbida por el NanoHull
        public float hullDamage;        // Porción que llega al HP del casco
        public bool  isCritical;        // Hit crítico (Particle Cannons)
        public DamageType type;

        // Efectos secundarios
        public bool  hasDoT;
        public float dotDamagePerSecond;
        public float dotDuration;
        public int   corrosiveStacksAdded; // Nidhogg
        public float speedReductionPercent; // DCR-250
        public bool  appliesFreeze;        // R-IC3
        public float freezeDuration;
    }
}
