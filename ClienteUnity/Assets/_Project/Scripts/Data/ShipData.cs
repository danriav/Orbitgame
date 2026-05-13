using UnityEngine;

namespace DO.Data
{
    [CreateAssetMenu(fileName = "NewShipData", menuName = "DO/Ship Data")]
    public class ShipData : ScriptableObject
    {
        public string shipName;
        
        [Header("Base Stats")]
        public float maxHealth = 425000f;
        public float maxNanoHull = 100000f;
        public float baseSpeed = 350f;
        
        [Header("Equipment Slots")]
        public int laserSlots = 16;
        public int generatorSlots = 18;
        public int moduleSlots = 4;

        [Header("Visuals")]
        public Mesh shipMesh;
        public Material shipMaterial;
        public float modelScale = 0.1f;
    }
}
