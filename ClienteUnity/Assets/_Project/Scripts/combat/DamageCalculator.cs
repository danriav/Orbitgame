using System.Collections.Generic;
using UnityEngine;

namespace DarkOrbit.Combat
{
    /// <summary>
    /// Calculadora estática de daño para todo el sistema de combate.
    /// El servidor es quien llama estos métodos; el cliente solo muestra efectos visuales.
    /// </summary>
    public static class DamageCalculator
    {
        // ══════════════════════════════════════════════════════════════════════
        //  DAÑO LÁSER
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula el daño de un ciclo de disparo láser completo.
        /// Suma el daño base de todos los láseres equipados y aplica
        /// el multiplicador del ammo activo + resistencias del objetivo.
        /// </summary>
        public static DamageResult CalculateLaserDamage(
            List<WeaponData>  equippedWeapons,
            AmmoData          ammo,
            TargetableEntity  target,
            bool              targetIsNPC = false)
        {
            if (ammo == null || equippedWeapons == null)
                return new DamageResult();

            // 1. Daño base total (suma de todos los láseres equipados)
            float totalBaseDamage = 0f;
            foreach (var weapon in equippedWeapons)
                if (weapon != null) totalBaseDamage += weapon.baseDamage;

            // 2. Aplicar multiplicador de ammo (JOB-100 y RB-214 son dinámicos)
            float multiplier = ammo.GetMultiplierFor(targetIsNPC);
            float rawDamage  = totalBaseDamage * multiplier;

            // 3. Resistencias del objetivo para daño especial
            rawDamage = ApplyTypeResistances(rawDamage, ammo.damageType, target);

            // 4. Construir resultado
            var result = new DamageResult { type = ammo.damageType };

            if (ammo.absorbsShield)
            {
                // SAB-50: todo el daño va al escudo
                result.shieldDamage  = rawDamage;
                result.finalDamage   = rawDamage;
            }
            else if (ammo.ignoresShield)
            {
                // DD-M01 mine: bypasea escudo y nanocasco
                result.hullDamage  = rawDamage;
                result.finalDamage = rawDamage;
            }
            else if (ammo.stealsShield)
            {
                // SR-5: daña y roba escudo (healing externo al atacante)
                result.shieldDamage = rawDamage * ammo.shieldStealPercent;
                result.hullDamage   = rawDamage * (1f - ammo.shieldStealPercent);
                result.finalDamage  = rawDamage;
            }
            else
            {
                DistributeDamage(rawDamage, target, out result.shieldDamage,
                                 out result.nanoHullDamage, out result.hullDamage);
                result.finalDamage = rawDamage;
            }

            // 5. Efectos secundarios
            if (ammo.appliesDoT)
            {
                result.hasDoT             = true;
                result.dotDamagePerSecond = ammo.dotDamagePerSecond;
                result.dotDuration        = ammo.dotDuration;
            }
            if (ammo.reducesSpeed)
                result.speedReductionPercent = ammo.speedReductionPercent;
            if (ammo.appliesFreeze)
            {
                result.appliesFreeze = true;
                result.freezeDuration = ammo.freezeDuration;
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DAÑO PARTICLE CANNON
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula el daño de un disparo de Particle Cannon, incluyendo
        /// varianza, críticos y efectos especiales por atributo.
        /// </summary>
        public static DamageResult CalculateParticleCannonDamage(
            ParticleCannonData cannon,
            TargetableEntity   target,
            int                existingCorrosiveStacks = 0)
        {
            if (cannon == null) return new DamageResult();

            // 1. Varianza de daño (75%–100%)
            float variance    = Random.Range(cannon.minDamagePercent, cannon.maxDamagePercent);
            float rawDamage   = cannon.GetTieredBaseDamage() * variance;

            // 2. Crítico
            bool isCrit = Random.value < cannon.critChance;
            if (isCrit) rawDamage *= cannon.critMultiplier;

            // 3. Daño adicional por stacks de Nidhogg existentes
            if (cannon.attribute == ParticleAttribute.Nidhogg && existingCorrosiveStacks > 0)
            {
                float bonus = existingCorrosiveStacks * cannon.corrosiveStackBonus;
                rawDamage  *= (1f + bonus);
            }

            // 4. Mapeamos atributo → tipo de daño
            DamageType dmgType = cannon.attribute switch
            {
                ParticleAttribute.Helios  => DamageType.Thermal,
                ParticleAttribute.Nidhogg => DamageType.Corrosive,
                ParticleAttribute.Indra   => DamageType.Electro,
                _                         => DamageType.Standard,
            };

            rawDamage = ApplyTypeResistances(rawDamage, dmgType, target);

            var result = new DamageResult { type = dmgType, isCritical = isCrit };
            DistributeDamage(rawDamage, target,
                             out result.shieldDamage,
                             out result.nanoHullDamage,
                             out result.hullDamage);
            result.finalDamage = rawDamage;

            // 5. Efectos por atributo
            if (cannon.attribute == ParticleAttribute.Nidhogg)
                result.corrosiveStacksAdded = 1;

            return result;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DAÑO COHETE
        // ══════════════════════════════════════════════════════════════════════

        public static DamageResult CalculateRocketDamage(
            AmmoData         rocket,
            TargetableEntity target)
        {
            if (rocket == null) return new DamageResult();

            float raw    = rocket.rocketBaseDamage;
            var   result = new DamageResult { type = DamageType.Rocket };

            DistributeDamage(raw, target,
                             out result.shieldDamage,
                             out result.nanoHullDamage,
                             out result.hullDamage);
            result.finalDamage = raw;

            if (rocket.reducesSpeed)
            {
                result.speedReductionPercent = rocket.speedReductionPercent;
            }
            if (rocket.appliesFreeze)
            {
                result.appliesFreeze  = true;
                result.freezeDuration = rocket.freezeDuration;
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS PRIVADOS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica las resistencias específicas por tipo de daño del objetivo.
        /// </summary>
        private static float ApplyTypeResistances(
            float            damage,
            DamageType       type,
            TargetableEntity target)
        {
            if (target == null) return damage;

            float resistance = type switch
            {
                DamageType.Thermal   => target.thermalResistance,
                DamageType.Corrosive => target.corrosiveResistance,
                DamageType.Electro   => target.electroResistance,
                _                    => 0f,
            };

            return damage * (1f - Mathf.Clamp01(resistance));
        }

        /// <summary>
        /// Distribuye el daño entre Escudo → NanoHull → Casco.
        /// Mecánica DO: el escudo absorbe primero, luego el nanocasco.
        /// Simplificado para emulación: asumimos que la salud actual
        /// del escudo/nano se valida en NetworkHealth.
        /// </summary>
        private static void DistributeDamage(
            float            totalDamage,
            TargetableEntity target,
            out float        shieldDmg,
            out float        nanoDmg,
            out float        hullDmg)
        {
            // Distribución estándar DO simplificada:
            // 40% al escudo, 15% al nanocasco, 45% al casco.
            // En NetworkHealth se saturará si el escudo/nano llegó a 0.
            shieldDmg = totalDamage * 0.40f;
            nanoDmg   = totalDamage * 0.15f;
            hullDmg   = totalDamage * 0.45f;
        }
    }
}
