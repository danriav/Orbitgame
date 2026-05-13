using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;

namespace DarkOrbit.Networking
{
    /// <summary>
    /// Arranca automáticamente el servidor si se ejecuta en modo Headless.
    /// </summary>
    public class HeadlessServerInitializer : MonoBehaviour
    {
        private void Start()
        {
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.Log("[DO-SERVER] Iniciando servidor dedicado...");
                NetworkManager networkManager = GetComponent<NetworkManager>();
                if (networkManager != null)
                {
                    networkManager.ServerManager.StartConnection();
                    Debug.Log("[DO-SERVER] Servidor FishNet iniciado en el puerto por defecto.");
                }
            }
        }
    }
}
