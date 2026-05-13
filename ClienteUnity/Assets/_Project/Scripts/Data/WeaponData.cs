using UnityEngine;

namespace DarkOrbit.Combat
{
    /// <summary>
    /// ScriptableObject que define un cañón láser equipable.
    /// Crear via: DO > Combat > Laser Cannon
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "DO/Combat/Laser Cannon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("ID interno: LF-1, LF-4, LF5_Mortifier, Asmodeus, etc.")]
        public string weaponId;
        public string displayName;
        public WeaponTier tier;

        [Header("Estadísticas de Combate")]
        [Tooltip("Daño base por disparo de este cañón (se suman todos los equipados)")]
        public float baseDamage = 200f;

        [Header("Visual del Rayo")]
        public Color  laserColor = new Color(1f, 0.2f, 0.2f); // rojo por defecto
        [Range(0.01f, 0.3f)]
        public float  laserWidth = 0.05f;

        [Header("Set Bonus")]
        [Tooltip("Si forma parte de un set de armas con efecto especial")]
        public bool   hasSetBonus = false;
        [TextArea(2, 4)]
        public string setDescription;
    }
}
