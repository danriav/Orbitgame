using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using FishNet.Managing;
using System.IO;

namespace DO.Editor
{
    public class MainSpaceSceneBuilder : EditorWindow
    {
        [MenuItem("DO/Scene Setup/Create Main_Space Scene")]
        public static void CreateMainSpaceScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "No puedes regenerar la escena en Play Mode. Por favor, detén el juego primero.", "OK");
                return;
            }

            string scenePath = "Assets/_Project/Scenes/Main_Space.unity";
            string dir = Path.GetDirectoryName(scenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            CreateEnvironment(newScene);
            
            EditorSceneManager.SaveScene(newScene, scenePath);
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log($"[DO] ✅ Escena 'Main_Space' creada correctamente.");
        }

        private static void CreateEnvironment(Scene scene)
        {
            // 1. LUZ
            GameObject lightObj = new GameObject("Sun Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.8f;
            light.color = new Color(0.7f, 0.8f, 1.0f); // Tono azulado espacial
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Ajustar luz ambiental: Un gris oscuro para que no sea negro absoluto
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.2f); // Azul oscuro sutil
            RenderSettings.skybox = null; 

            // 2. PREFAB DE NAVE
            GameObject shipPrefab = CreateShipPrefab();
            
            // 3. NAVE DE REFERENCIA
            if (shipPrefab != null)
            {
                GameObject refShip = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
                PrefabUtility.UnpackPrefabInstance(refShip, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                refShip.name = "[VISUAL] Ship Reference";
                foreach (var comp in refShip.GetComponentsInChildren<Component>())
                {
                    if (!(comp is Transform || comp is MeshFilter || comp is MeshRenderer))
                        DestroyImmediate(comp);
                }
            }

            // 4. RED (NetworkManager) - CONFIGURACIÓN SEGURA
            GameObject nmObj = new GameObject("NetworkManager");
            var nm = nmObj.AddComponent<NetworkManager>();
            
            // Cargar colección por defecto
            var prefabCollection = AssetDatabase.LoadAssetAtPath<FishNet.Managing.Object.PrefabObjects>("Assets/DefaultPrefabObjects.asset");
            
            // Usar SerializedObject para asignar ANTES de que FishNet de error
            SerializedObject so = new SerializedObject(nm);
            SerializedProperty sp = so.FindProperty("_spawnablePrefabs");
            if (sp == null) sp = so.FindProperty("SpawnablePrefabs");
            
            if (sp != null && prefabCollection != null)
            {
                sp.objectReferenceValue = prefabCollection;
                so.ApplyModifiedPropertiesWithoutUndo();
                
                // Forzar inicialización manual
                if (nm.SpawnablePrefabs == null) nm.SpawnablePrefabs = prefabCollection;
            }

            nmObj.AddComponent<FishNet.Transporting.Tugboat.Tugboat>();
            nmObj.AddComponent<DO.Networking.DOSimpleNetworkHUD>();
            nmObj.AddComponent<DarkOrbit.Networking.HeadlessServerInitializer>();
            
            var spawner = nmObj.AddComponent<FishNet.Component.Spawning.PlayerSpawner>();
            if (shipPrefab != null) spawner.SetPlayerPrefab(shipPrefab.GetComponent<FishNet.Object.NetworkObject>());

            // 5. EVENT SYSTEM
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 6. CÁMARA
            CreateCamera();
        }

        private static GameObject CreateShipPrefab()
        {
            string prefabPath = "Assets/_Project/Prefabs/PlayerShip_Net.prefab";
            if (!Directory.Exists("Assets/_Project/Prefabs")) Directory.CreateDirectory("Assets/_Project/Prefabs");

            GameObject tempShip = new GameObject("PlayerShip_Template");
            tempShip.tag = "Player";
            var filter = tempShip.AddComponent<MeshFilter>();
            var renderer = tempShip.AddComponent<MeshRenderer>();
            Mesh vMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/default_48.asset");
            
            if (vMesh != null) 
            {
                filter.sharedMesh = vMesh;
                // Rotación final descubierta por el usuario
                tempShip.transform.rotation = Quaternion.Euler(90, 180, 0); 
            }
            else 
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                filter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(cube);
            }

            // Intentar cargar el material original de la Lightning
            Material shipMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/ship_v-lightning.mat");
            if (shipMat == null) shipMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/M_DO_1_48.mat");
            
            if (shipMat != null)
            {
                renderer.sharedMaterial = shipMat;
                // Si el shader original está roto (se ve blanco), intentamos usar Standard
                if (shipMat.shader == null || shipMat.shader.name.Contains("Error") || shipMat.shader.name.Contains("Blinn"))
                {
                    var stdShader = Shader.Find("Standard");
                    if (stdShader != null)
                    {
                        // Guardar texturas actuales
                        Texture diff = shipMat.GetTexture("_Diffuse");
                        Texture norm = shipMat.GetTexture("_Normal");
                        
                        shipMat.shader = stdShader;
                        
                        // Reasignar al nuevo shader
                        if (diff != null) shipMat.SetTexture("_MainTex", diff);
                        if (norm != null) {
                            shipMat.SetTexture("_BumpMap", norm);
                            shipMat.EnableKeyword("_NORMALMAP");
                        }
                    }
                }
            }
            else
            {
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }

            var rb = tempShip.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            
            // Escala de la nave: Las naves extraídas suelen ser masivas (cientos de metros)
            tempShip.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            
            tempShip.AddComponent<FishNet.Object.NetworkObject>();
            tempShip.AddComponent<FishNet.Component.Transforming.NetworkTransform>();
            tempShip.AddComponent<DarkOrbit.Networking.NetworkHealth>();
            tempShip.AddComponent<DarkOrbit.Networking.AbilityPrismaticShield>();
            tempShip.AddComponent<DarkOrbit.Networking.NetworkShipController>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempShip, prefabPath);
            GameObject.DestroyImmediate(tempShip);
            return prefab;
        }

        private static void CreateCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            cam.orthographicSize = 15f;
            cam.farClipPlane = 10000f;
            // Posición para plano XY
            camObj.transform.position = new Vector3(0, 0, -50);
            camObj.transform.rotation = Quaternion.Euler(0, 0, 0);
            camObj.AddComponent<DO.Controllers.FollowCameraController>();
            camObj.AddComponent<AudioListener>();
        }
    }
}
