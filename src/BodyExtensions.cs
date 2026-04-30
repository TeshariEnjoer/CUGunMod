using System.Collections;
using System.Collections.Generic;

namespace CasGunMod {
    public static class BodyExtensions {

        public static bool TryGetHoldingItem(this Body body, out Item item) {

            item = null;

            if(body.HoldingItem(body.handSlot)) {
                item =  body.GetItem(body.handSlot);
                return true;
            }
            return false;
        }

        /* todo
        public static List<Item> GetAllItems(this Body body) {
            List<Item> items = new List<Item>();
            foreach(Item item in body)
       
        }
        */

    }
}
