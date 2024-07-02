using System.Linq;
using UnityEngine;

namespace MaximovInk
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "MaximovInk/ItemDatabase")]
    public class MKItemDatabase : ScriptableObject
    {
        public MKItemData[] Data;

        public MKItemData Get(string ID)
        {
            return Data.FirstOrDefault(n => n.ID == ID);
        }

    }
}
