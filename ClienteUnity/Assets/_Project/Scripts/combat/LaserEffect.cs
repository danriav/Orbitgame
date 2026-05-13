using UnityEngine;
using System.Collections;

namespace DarkOrbit.Visuals
{
    /// <summary>
    /// Efecto visual de rayo láser usando LineRenderer.
    /// Ahora soporta color y grosor dinámicos según el tipo de ammo.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserEffect : MonoBehaviour
    {
        private LineRenderer _line;

        [Tooltip("Tiempo que permanece el rayo en pantalla (segundos)")]
        public float duration = 0.12f;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.enabled       = false;
            _line.positionCount = 2;
            _line.useWorldSpace = true;

            // Material por defecto — se anulará con el color del ammo
            _line.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// <summary>
        /// Muestra el rayo con el color y grosor del ammo activo.
        /// </summary>
        public void Play(Vector3 start, Vector3 end, Color color, float width = 0.05f)
        {
            StopAllCoroutines();
            StartCoroutine(ShowLaser(start, end, color, width));
        }

        /// <summary>Sobrecarga de compatibilidad con el sistema anterior (color rojo por defecto).</summary>
        public void Play(Vector3 start, Vector3 end)
            => Play(start, end, new Color(1f, 0.2f, 0.1f), 0.05f);

        private IEnumerator ShowLaser(Vector3 start, Vector3 end, Color color, float width)
        {
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
            _line.startWidth = width;
            _line.endWidth   = width * 0.3f;   // Afila el extremo del rayo
            _line.startColor = color;
            _line.endColor   = new Color(color.r, color.g, color.b, 0f); // Fade out en el extremo
            _line.enabled    = true;

            // Efecto de flash rápido al inicio
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Desvanecer gradualmente
                float alpha = 1f - (elapsed / duration);
                _line.startColor = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _line.enabled = false;
        }
    }
}