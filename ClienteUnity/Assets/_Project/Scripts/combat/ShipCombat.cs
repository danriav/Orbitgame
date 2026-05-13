using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using DarkOrbit.Combat;
using DarkOrbit.Visuals;

namespace DarkOrbit.Networking
{
    /// <summary>
    /// Sistema de combate de la nave. Gestiona:
    ///   - Disparo de láseres con munición activa (Ctrl / click derecho en objetivo)
    ///   - Disparo de Particle Cannons: Helios (F1), Nidhogg (F2), Indra (F3)
    ///   - Disparo de Rockets (Space)
    ///   - Ciclar ammo: X (laser) / Z (rocket)
    /// Todo el daño se calcula y aplica en el servidor (authoritative).
    /// </summary>
    public class ShipCombat : NetworkBehaviour
    {
        // ── Referencias ────────────────────────────────────────────────────────
        private NetworkShipController _controller;
        private WeaponLoadout         _loadout;

        [Header("Efectos Visuales")]
        public LaserEffect         laserEffect;
        public ParticleCannonEffect particleEffect;

        // ── Estado de combate ──────────────────────────────────────────────────
        private bool  _isAutoFiring   = false;
        private float _nextLaserTime  = 0f;
        public  float fireRate        = 1.0f;  // 1 disparo/segundo por defecto

        // ══════════════════════════════════════════════════════════════════════
        //  CICLO DE VIDA
        // ══════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _controller = GetComponent<NetworkShipController>();
            _loadout    = GetComponent<WeaponLoadout>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            HandleInput();
        }

        private void FixedUpdate()
        {
            // Solo el servidor aplica el daño real
            if (!IsServerInitialized) return;

            if (_isAutoFiring && Time.time >= _nextLaserTime)
            {
                var target = _controller?.GetCurrentTarget();
                if (target == null) { _isAutoFiring = false; return; }
                ExecuteLaserCycle(target);
                _nextLaserTime = Time.time + fireRate;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INPUT (solo cliente local / owner)
        // ══════════════════════════════════════════════════════════════════════

        private void HandleInput()
        {
            // ── Toggle auto-disparo láser ──────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                var target = _controller?.GetCurrentTarget();
                if (target != null)
                {
                    _isAutoFiring = !_isAutoFiring;
                    string state = _isAutoFiring ? "<color=red>ATAQUE</color>" : "<color=yellow>DETENIDO</color>";
                    Debug.Log($"[COMBAT] Auto-disparo: {state} → {target.entityName}");
                }
                else
                {
                    Debug.LogWarning("[COMBAT] No hay objetivo seleccionado.");
                }
            }

            // ── Ciclar ammo láser (X) ──────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.X))
                _loadout?.CmdCycleLaserAmmo();

            // ── Ciclar rocket (Z) ──────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.Z))
                _loadout?.CmdCycleRocket();

            // ── Rocket (Space) ─────────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.Space))
                FireRocket();

            // ── Particle Cannons ───────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.F1)) FireParticleCannon(ParticleAttribute.Helios);
            if (Input.GetKeyDown(KeyCode.F2)) FireParticleCannon(ParticleAttribute.Nidhogg);
            if (Input.GetKeyDown(KeyCode.F3)) FireParticleCannon(ParticleAttribute.Indra);

            // ── Debug: daño de prueba (K) ──────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.K))
                GetComponent<NetworkHealth>()?.CmdApplyDamage(50000f);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DISPARO LÁSER (Server)
        // ══════════════════════════════════════════════════════════════════════

        [Server]
        private void ExecuteLaserCycle(TargetableEntity target)
        {
            var health = target.GetComponent<NetworkHealth>();
            if (health == null) return;

            var ammo    = _loadout?.GetActiveLaserAmmo();
            var weapons = _loadout?.laserSlots ?? new List<WeaponData>();

            DamageResult result = DamageCalculator.CalculateLaserDamage(
                weapons, ammo, target, target.isNPC);

            health.ServerApplyDamage(result.shieldDamage,
                                     result.nanoHullDamage,
                                     result.hullDamage);

            // Efectos secundarios
            ApplySecondaryEffects(result, target);

            // Notificar efecto visual a todos los clientes
            Color  color = _loadout?.GetActiveLaserColor() ?? Color.red;
            float  width = ammo != null ? 0.05f : 0.05f;
            RpcShowLaser(transform.position, target.transform.position, color, width);

            // Consumir ammo
            _loadout?.ConsumeLaserAmmo();

            // Verificar si murió
            if (health.currentHealth.Value <= 0f)
                _isAutoFiring = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PARTICLE CANNON (Cliente → Servidor → Todos)
        // ══════════════════════════════════════════════════════════════════════

        private void FireParticleCannon(ParticleAttribute attr)
        {
            if (_loadout == null) return;
            if (!_loadout.CanFireCannon(attr))
            {
                Debug.Log($"[PARTICLE] Cannon {attr} en cooldown.");
                return;
            }
            var target = _controller?.GetCurrentTarget();
            if (target == null)
            {
                Debug.LogWarning("[PARTICLE] Sin objetivo para Particle Cannon.");
                return;
            }
            // Iniciar cooldown local (visual)
            var cannon = _loadout.GetCannon(attr);
            _loadout.StartCannonCooldown(attr, cannon.reloadTime);

            CmdFireParticleCannon((int)attr, target.NetworkObject);
        }

        [ServerRpc]
        private void CmdFireParticleCannon(int attrInt, FishNet.Object.NetworkObject targetObj)
        {
            if (targetObj == null) return;
            var attr   = (ParticleAttribute)attrInt;
            var target = targetObj.GetComponent<TargetableEntity>();
            var cannon = _loadout?.GetCannon(attr);
            if (cannon == null || target == null) return;

            DamageResult result = DamageCalculator.CalculateParticleCannonDamage(
                cannon, target, target.corrosiveStacks.Value);

            var health = targetObj.GetComponent<NetworkHealth>();
            health?.ServerApplyDamage(result.shieldDamage,
                                      result.nanoHullDamage,
                                      result.hullDamage);

            // Nidhogg: añadir stack de corrosión
            if (result.corrosiveStacksAdded > 0)
                target.AddCorrosiveStack(result.corrosiveStacksAdded, cannon.corrosiveStackDuration);

            // Helios: splash a objetivos cercanos
            if (attr == ParticleAttribute.Helios)
                ApplyHeliosSplash(cannon, target, result.finalDamage);

            // Indra: chain effect
            if (attr == ParticleAttribute.Indra)
                ApplyIndraChain(cannon, target, result.finalDamage);

            string critTxt = result.isCritical ? " <color=yellow>[CRÍTICO]</color>" : "";
            Debug.Log($"[PARTICLE:{attr}] {result.finalDamage:F0} daño → {target.entityName}{critTxt}");

            // Efecto visual
            RpcShowParticleCannon(attrInt, transform.position, targetObj.transform.position);
            _loadout?.ConsumeParticleSlug();
        }

        [Server]
        private void ApplyHeliosSplash(ParticleCannonData cannon, TargetableEntity mainTarget, float mainDamage)
        {
            float splashDmg = mainDamage * cannon.splashDamagePercent;
            var   hits      = Physics.OverlapSphere(mainTarget.transform.position, cannon.splashRadius);
            int   count     = 0;

            foreach (var hit in hits)
            {
                if (count >= cannon.maxSplashTargets) break;
                var entity = hit.GetComponent<TargetableEntity>();
                if (entity == null || entity == mainTarget) continue;
                var h = entity.GetComponent<NetworkHealth>();
                if (h == null) continue;

                h.ServerApplyDamage(splashDmg * 0.4f, splashDmg * 0.15f, splashDmg * 0.45f);
                count++;
            }
            if (count > 0)
                Debug.Log($"[HELIOS] Splash {splashDmg:F0} daño a {count} objetivos adicionales.");
        }

        [Server]
        private void ApplyIndraChain(ParticleCannonData cannon, TargetableEntity mainTarget, float mainDamage)
        {
            float chainDmg = mainDamage * cannon.chainDamagePercent;
            var   hits     = Physics.OverlapSphere(mainTarget.transform.position, cannon.chainRange);
            int   count    = 0;

            foreach (var hit in hits)
            {
                if (count >= cannon.chainTargets) break;
                var entity = hit.GetComponent<TargetableEntity>();
                if (entity == null || entity == mainTarget) continue;
                var h = entity.GetComponent<NetworkHealth>();
                if (h == null) continue;

                h.ServerApplyDamage(chainDmg * 0.4f, chainDmg * 0.15f, chainDmg * 0.45f);
                count++;
            }
            if (count > 0)
                Debug.Log($"[INDRA] Chain {chainDmg:F0} daño a {count} objetivos encadenados.");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ROCKET
        // ══════════════════════════════════════════════════════════════════════

        private void FireRocket()
        {
            var target = _controller?.GetCurrentTarget();
            if (target == null)
            {
                Debug.LogWarning("[ROCKET] Sin objetivo.");
                return;
            }
            CmdFireRocket(target.NetworkObject);
        }

        [ServerRpc]
        private void CmdFireRocket(FishNet.Object.NetworkObject targetObj)
        {
            if (targetObj == null || _loadout == null) return;
            if (!_loadout.ConsumeRocket()) { Debug.Log("[ROCKET] Sin rockets."); return; }

            var rocket = _loadout.GetActiveRocket();
            var target = targetObj.GetComponent<TargetableEntity>();
            var health = targetObj.GetComponent<NetworkHealth>();
            if (rocket == null || target == null || health == null) return;

            DamageResult result = DamageCalculator.CalculateRocketDamage(rocket, target);
            health.ServerApplyDamage(result.shieldDamage,
                                     result.nanoHullDamage,
                                     result.hullDamage);

            ApplySecondaryEffects(result, target);
            Debug.Log($"[ROCKET:{rocket.displayName}] {result.finalDamage:F0} daño → {target.entityName}");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  EFECTOS SECUNDARIOS
        // ══════════════════════════════════════════════════════════════════════

        [Server]
        private void ApplySecondaryEffects(DamageResult result, TargetableEntity target)
        {
            if (result.speedReductionPercent > 0f)
                target.speedReductionPercent.Value = result.speedReductionPercent;

            if (result.appliesFreeze)
                target.isFrozen.Value = true;

            // TODO: iniciar coroutines de expiración de efectos de control
        }

        // ══════════════════════════════════════════════════════════════════════
        //  RPCs DE EFECTOS VISUALES
        // ══════════════════════════════════════════════════════════════════════

        [ObserversRpc]
        private void RpcShowLaser(Vector3 start, Vector3 end, Color color, float width)
        {
            laserEffect?.Play(start, end, color, width);
        }

        [ObserversRpc]
        private void RpcShowParticleCannon(int attrInt, Vector3 start, Vector3 end)
        {
            particleEffect?.Play((ParticleAttribute)attrInt, start, end);
        }
    }
}