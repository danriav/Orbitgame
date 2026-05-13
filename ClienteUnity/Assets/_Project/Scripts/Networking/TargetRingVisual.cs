using UnityEngine;

namespace DarkOrbit.UI // <-- La Vengeance también buscará esto
{
    public class TargetRingVisual : MonoBehaviour
    {
        public float spinSpeed = 90f;
        public float zOffset = -0.5f; 
        private Transform _targetTransform;

        private void Start() => gameObject.SetActive(false);

        private void Update()
        {
            if (_targetTransform != null)
            {
                Vector3 targetPos = _targetTransform.position;
                targetPos.z += zOffset;
                transform.position = targetPos;
                transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            _targetTransform = newTarget;
            gameObject.SetActive(true);
        }

        public void ClearTarget()
        {
            _targetTransform = null;
            gameObject.SetActive(false);
        }
    }
}