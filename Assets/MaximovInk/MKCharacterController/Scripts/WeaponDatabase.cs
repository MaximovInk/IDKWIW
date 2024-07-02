using System.Linq;
using UnityEngine;

namespace MaximovInk
{
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "MaximovInk/WeaponDatabase")]
    public class WeaponDatabase : ScriptableObject
    {
        public Weapon[] Weapons => _weapons;
        [SerializeField] private Weapon[] _weapons;

        public Weapon Get(string id)
        {
            return _weapons.FirstOrDefault(x => x.ID == id);
        }

        public Weapon Get(int id)
        {
            if (id < 0 || id >= _weapons.Length) return null;

            return _weapons[id];
        }
    }
}
