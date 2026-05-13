using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace DarkOrbit.Networking
{
    public class NetworkHealth : NetworkBehaviour
    {
        // Nueva sintaxis de FishNet: SyncVar<T>
        public readonly SyncVar<float> currentHealth = new SyncVar<float>(425000f);
        public readonly SyncVar<float> maxHealth = new SyncVar<float>(425000f);

        public event System.Action<float, float> OnHealthUpdate;

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Suscribirse al cambio de vida
            currentHealth.OnChange += OnHealthChanged;
            // Disparo inicial para la UI
            OnHealthUpdate?.Invoke(currentHealth.Value, maxHealth.Value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdApplyDamage(float amount)
        {
            // El servidor modifica el .Value
            currentHealth.Value -= amount;

            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                OnShipDestroyed();
            }
        }

        private void OnHealthChanged(float prev, float next, bool asServer)
        {
            OnHealthUpdate?.Invoke(next, maxHealth.Value);
        }

        private void OnShipDestroyed()
        {
            Debug.Log($"[SERVER] Nave {gameObject.name} destruida.");
        }
    }
}
