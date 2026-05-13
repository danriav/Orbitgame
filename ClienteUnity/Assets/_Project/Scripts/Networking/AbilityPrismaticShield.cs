using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace DarkOrbit.Networking
{
    public class AbilityPrismaticShield : NetworkBehaviour
    {
        public readonly SyncVar<bool> isShieldActive = new SyncVar<bool>(false);

        [Header("Spectrum Plus Stats")]
        public float damageMitigation = 0.25f; 
        public float damageReflection = 0.75f; 

        [ServerRpc]
        public void CmdToggleShield(bool active)
        {
            isShieldActive.Value = active;
            Debug.Log($"[SERVER] Prismatic Shield {(active ? "Activado" : "Desactivado")}");
        }

        public float HandleIncomingDamage(float originalDamage)
        {
            if (!isShieldActive.Value) return originalDamage;

            float mitigatedDamage = originalDamage * (1f - damageMitigation);
            return mitigatedDamage;
        }
    }
}
