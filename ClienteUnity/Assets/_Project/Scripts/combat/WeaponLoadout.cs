using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace DarkOrbit.Combat
{
    /// <summary>
    /// Gestiona el loadout completo de armas de una nave:
    ///   - Hasta 16 slots de cañones láser
    ///   - 3 slots de Particle Cannons (uno por atributo)
    ///   - Selección activa de ammo láser, rocket y particle slug
    ///   - Inventario de munición sincronizado en red
    /// </summary>
    public class WeaponLoadout : NetworkBehaviour
    {
        // ── Cañones Equipados ──────────────────────────────────────────────────
        [Header("Cañones Láser (hasta 16 slots)")]
        public List<WeaponData> laserSlots = new List<WeaponData>(16);

        [Header("Particle Cannons (uno por atributo)")]
        public ParticleCannonData heliosSlot;    // Thermal — AoE
        public ParticleCannonData nidhoggSlot;   // Corrosive — stacks
        public ParticleCannonData indraSlot;     // Electro — chain

        // ── Munición Disponible ────────────────────────────────────────────────
        [Header("Munición disponible (asigna en Inspector o por código)")]
        public List<AmmoData> availableLaserAmmo    = new List<AmmoData>();
        public List<AmmoData> availableRockets       = new List<AmmoData>();
        public List<AmmoData> availableParticleSlugs = new List<AmmoData>();

        // ── Selección Activa (SyncVars) ────────────────────────────────────────
        public readonly SyncVar<int> activeLaserAmmoIndex = new SyncVar<int>(0);
        public readonly SyncVar<int> activeRocketIndex    = new SyncVar<int>(0);
        public readonly SyncVar<int> activeSlugIndex      = new SyncVar<int>(0);

        // ── Inventario (SyncLists) ─────────────────────────────────────────────
        public readonly SyncList<int> laserAmmoCounts  = new SyncList<int>();
        public readonly SyncList<int> rocketCounts     = new SyncList<int>();
        public readonly SyncList<int> slugCounts       = new SyncList<int>();

        // ── Cooldowns de Particle Cannon (locales) ────────────────────────────
        [System.NonSerialized] public float heliosCooldown  = 0f;
        [System.NonSerialized] public float nidhoggCooldown = 0f;
        [System.NonSerialized] public float indraCooldown   = 0f;

        // ── Eventos para el HUD ────────────────────────────────────────────────
        public event System.Action OnLoadoutChanged;

        // ══════════════════════════════════════════════════════════════════════
        //  CICLO DE VIDA
        // ══════════════════════════════════════════════════════════════════════

        public override void OnStartServer()
        {
            base.OnStartServer();
            InitializeAmmoInventory();
        }

        private void Update()
        {
            // Tick de cooldowns de Particle Cannons (solo cliente local, para HUD)
            if (!IsOwner) return;
            if (heliosCooldown  > 0f) heliosCooldown  -= Time.deltaTime;
            if (nidhoggCooldown > 0f) nidhoggCooldown -= Time.deltaTime;
            if (indraCooldown   > 0f) indraCooldown   -= Time.deltaTime;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INICIALIZACIÓN DE INVENTARIO
        // ══════════════════════════════════════════════════════════════════════

        [Server]
        private void InitializeAmmoInventory()
        {
            laserAmmoCounts.Clear();
            foreach (var _ in availableLaserAmmo)
                laserAmmoCounts.Add(10000);   // 10k por tipo por defecto

            rocketCounts.Clear();
            foreach (var _ in availableRockets)
                rocketCounts.Add(100);

            slugCounts.Clear();
            foreach (var _ in availableParticleSlugs)
                slugCounts.Add(50);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GETTERS DE SELECCIÓN ACTIVA
        // ══════════════════════════════════════════════════════════════════════

        public AmmoData GetActiveLaserAmmo()
        {
            if (availableLaserAmmo == null || availableLaserAmmo.Count == 0) return null;
            int idx = Mathf.Clamp(activeLaserAmmoIndex.Value, 0, availableLaserAmmo.Count - 1);
            return availableLaserAmmo[idx];
        }

        public AmmoData GetActiveRocket()
        {
            if (availableRockets == null || availableRockets.Count == 0) return null;
            int idx = Mathf.Clamp(activeRocketIndex.Value, 0, availableRockets.Count - 1);
            return availableRockets[idx];
        }

        public AmmoData GetActiveParticleSlug()
        {
            if (availableParticleSlugs == null || availableParticleSlugs.Count == 0) return null;
            int idx = Mathf.Clamp(activeSlugIndex.Value, 0, availableParticleSlugs.Count - 1);
            return availableParticleSlugs[idx];
        }

        public int GetActiveLaserAmmoCount()
        {
            int idx = activeLaserAmmoIndex.Value;
            if (idx < 0 || idx >= laserAmmoCounts.Count) return 0;
            return laserAmmoCounts[idx];
        }

        public int GetActiveRocketCount()
        {
            int idx = activeRocketIndex.Value;
            if (idx < 0 || idx >= rocketCounts.Count) return 0;
            return rocketCounts[idx];
        }

        /// <summary>Daño láser base total (suma de todos los láseres equipados).</summary>
        public float GetTotalLaserBaseDamage()
        {
            float total = 0f;
            foreach (var w in laserSlots)
                if (w != null) total += w.baseDamage;
            return total;
        }

        /// <summary>Color del ammo láser activo (para el LineRenderer).</summary>
        public Color GetActiveLaserColor()
        {
            var ammo = GetActiveLaserAmmo();
            return ammo != null ? ammo.projectileColor : Color.red;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CICLAR MUNICIÓN (ServerRpc)
        // ══════════════════════════════════════════════════════════════════════

        [ServerRpc]
        public void CmdCycleLaserAmmo()
        {
            if (availableLaserAmmo.Count == 0) return;
            int next = (activeLaserAmmoIndex.Value + 1) % availableLaserAmmo.Count;
            activeLaserAmmoIndex.Value = next;
            RpcOnAmmoChanged();
        }

        [ServerRpc]
        public void CmdCycleRocket()
        {
            if (availableRockets.Count == 0) return;
            int next = (activeRocketIndex.Value + 1) % availableRockets.Count;
            activeRocketIndex.Value = next;
            RpcOnAmmoChanged();
        }

        [ObserversRpc]
        private void RpcOnAmmoChanged() => OnLoadoutChanged?.Invoke();

        // ══════════════════════════════════════════════════════════════════════
        //  CONSUMO DE MUNICIÓN (Server-side)
        // ══════════════════════════════════════════════════════════════════════

        [Server]
        public bool ConsumeLaserAmmo()
        {
            int idx = activeLaserAmmoIndex.Value;
            if (idx >= laserAmmoCounts.Count || laserAmmoCounts[idx] <= 0) return false;
            laserAmmoCounts[idx]--;
            return true;
        }

        [Server]
        public bool ConsumeRocket()
        {
            int idx = activeRocketIndex.Value;
            if (idx >= rocketCounts.Count || rocketCounts[idx] <= 0) return false;
            rocketCounts[idx]--;
            return true;
        }

        [Server]
        public bool ConsumeParticleSlug()
        {
            int idx = activeSlugIndex.Value;
            if (idx >= slugCounts.Count || slugCounts[idx] <= 0) return false;
            slugCounts[idx]--;
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PARTICLE CANNON — obtener por atributo
        // ══════════════════════════════════════════════════════════════════════

        public ParticleCannonData GetCannon(ParticleAttribute attr)
        {
            return attr switch
            {
                ParticleAttribute.Helios  => heliosSlot,
                ParticleAttribute.Nidhogg => nidhoggSlot,
                ParticleAttribute.Indra   => indraSlot,
                _                         => null,
            };
        }

        public bool CanFireCannon(ParticleAttribute attr)
        {
            return attr switch
            {
                ParticleAttribute.Helios  => heliosSlot  != null && heliosCooldown  <= 0f,
                ParticleAttribute.Nidhogg => nidhoggSlot != null && nidhoggCooldown <= 0f,
                ParticleAttribute.Indra   => indraSlot   != null && indraCooldown   <= 0f,
                _                         => false,
            };
        }

        public void StartCannonCooldown(ParticleAttribute attr, float time)
        {
            switch (attr)
            {
                case ParticleAttribute.Helios:  heliosCooldown  = time; break;
                case ParticleAttribute.Nidhogg: nidhoggCooldown = time; break;
                case ParticleAttribute.Indra:   indraCooldown   = time; break;
            }
        }

        public float GetCannonCooldownNormalized(ParticleAttribute attr)
        {
            var cannon = GetCannon(attr);
            if (cannon == null) return 0f;
            float remaining = attr switch
            {
                ParticleAttribute.Helios  => heliosCooldown,
                ParticleAttribute.Nidhogg => nidhoggCooldown,
                ParticleAttribute.Indra   => indraCooldown,
                _                         => 0f,
            };
            return Mathf.Clamp01(remaining / cannon.reloadTime);
        }
    }
}
