
namespace CasGunMod {
    public static class GunHelpers {
        public static bool IsGun(Item item) {
            
            if (item == null) return false;

            if (item.Stats.HasTag("gun") || item.GetComponent<GunComponent>()) {
                return true;
            }

            return false;
        }

    }
}
