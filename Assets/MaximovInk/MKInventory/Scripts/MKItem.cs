using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MaximovInk
{
    [System.Serializable]
    public class MKItem
    {
        public string ItemID;
        public int Count;
        public float Durability;

        public MKItemData Data => MKInventoryManager.Instance.Database.Get(ItemID);

        public bool CanStack(MKItem other)
        {
            if (other == null) return false;
            if(other.ItemID != ItemID) return false;

            var data = Data;

            if (Count + other.Count > data.MaxCount) return false;

            if (data.IsDurable)
            {
                var durabilityDiff = Mathf.Abs(other.Durability - Durability);
                if (durabilityDiff > 0.05f)
                {
                    return data.CanDurabilityStack;
                }


            }

            return true;

        }

        public MKItem Stack(MKItem item)
        {
            if (item == null) return this;
            if (item.ItemID != ItemID) return this;


            return new MKItem
            {
                Count = Count + item.Count,
                Durability = (Durability + item.Durability) / 2f,
                ItemID = ItemID
            };
        }
    }
}
