using UnityEngine;
using System.Collections;

namespace DarkOrbit.Visuals
{
    /// <summary>
    /// Efecto visual para Particle Cannons (Helios / Nidhogg / Indra).
    /// Usa LineRenderer con partículas para diferenciar el tipo de cannon.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ParticleCannonEffect : MonoBehaviour
    {
        private LineRenderer _line;
        private ParticleSystem _particles;

        [Header("Duración del rayo en pantalla")]
        public float duration = 0.25f;

        // Paleta de colores por atributo
        private static readonly Color HeliosColor  = new Color(1.0f, 0.5f, 0.1f); // naranja
        private static readonly Color NidhoggColor = new Color(0.3f, 1.0f, 0.2f); // verde ácido
        private static readonly Color IndraColor   = new Color(0.2f, 0.8f, 1.0f); // cian eléctrico

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.enabled       = false;
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.material      = new Material(Shader.Find("Sprites/Default"));

            // Partículas secundarias
            _particles = gameObject.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;
            main.loop        = false;
            main.playOnAwake = false;
            main.duration    = 0.2f;
            main.startSize   = 0.4f;
            main.startLifetime = 0.3f;
            main.startSpeed  = 3f;
        }

        /// <summary>
        /// Dispara el efecto visual de Particle Cannon hacia el objetivo.
        /// </summary>
        public void Play(DarkOrbit.Combat.ParticleAttribute attr, Vector3 start, Vector3 end)
        {
            StopAllCoroutines();
            Color color = attr switch
            {
                DarkOrbit.Combat.ParticleAttribute.Helios  => HeliosColor,
                DarkOrbit.Combat.ParticleAttribute.Nidhogg => NidhoggColor,
                DarkOrbit.Combat.ParticleAttribute.Indra   => IndraColor,
                _                                          => Color.white,
            };

            // Ajustar color de partículas
            var main = _particles.main;
            main.startColor = color;

            StartCoroutine(ShowBeam(start, end, color));
        }

        private IEnumerator ShowBeam(Vector3 start, Vector3 end, Color color)
        {
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
            _line.startWidth = 0.18f;
            _line.endWidth   = 0.05f;
            _line.startColor = color;
            _line.endColor   = new Color(color.r, color.g, color.b, 0f);
            _line.enabled    = true;

            _particles.transform.position = end;
            _particles.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / duration);
                _line.startColor = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _line.enabled = false;
        }
    }
}
