using UnityEngine;

namespace DO.Utils
{
    [ExecuteInEditMode]
    public class EmergencyCameraFix : MonoBehaviour
    {
        public Transform target;
        public float height = 30f;
        public Color debugColor = Color.magenta;

        void Update()
        {
            // 1. Buscar la nave si no existe
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target == null) return;

            // 2. Forzar posición de cámara (lejos para ver la nave gigante)
            transform.position = new Vector3(target.position.x, target.position.y + height, target.position.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 3. Forzar parámetros de cámara
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = debugColor; // Color Magenta para saber que ESTE script funciona
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.cullingMask = -1; // Ver TODO
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 1000f;
            }
        }
    }
}
