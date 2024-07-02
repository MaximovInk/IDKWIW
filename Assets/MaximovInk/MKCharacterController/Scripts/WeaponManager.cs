using UnityEngine;

namespace MaximovInk
{
    public class WeaponManager : MonoBehaviourSingleton<WeaponManager>
    {
        public WeaponDatabase Database => _database;

        [SerializeField]
        private WeaponDatabase _database;
    }
}
