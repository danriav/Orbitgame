using UnityEngine;
using FishNet.Managing;

namespace DO.Networking
{
    public class DOSimpleNetworkHUD : MonoBehaviour
    {
        private NetworkManager _networkManager;

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
        }

        private void OnGUI()
        {
            if (_networkManager == null || _networkManager.ServerManager == null || _networkManager.ClientManager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 200));

            if (_networkManager.ServerManager.Started || _networkManager.ClientManager.Started)
            {
                if (GUILayout.Button("DETENER RED", GUILayout.Height(40)))
                {
                    _networkManager.ServerManager.StopConnection(true);
                    _networkManager.ClientManager.StopConnection();
                }
            }
            else
            {
                if (GUILayout.Button("MODO HOST (Server+Player)", GUILayout.Height(50)))
                {
                    _networkManager.ServerManager.StartConnection();
                    _networkManager.ClientManager.StartConnection();
                }

                GUILayout.Space(10);

                if (GUILayout.Button("MODO CLIENTE", GUILayout.Height(40)))
                {
                    _networkManager.ClientManager.StartConnection();
                }
            }

            GUILayout.EndArea();
        }
    }
}
