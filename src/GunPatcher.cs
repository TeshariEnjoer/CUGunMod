using BepInEx.Logging;

namespace CasGunMod {
    public static class GunPatcher {


        private static bool IsGun(ItemInfo item)
            => item.HasTag("gun");

        public static void PatchAll(ManualLogSource logger) {

            int count = 0;
            foreach (ItemInfo item in Item.GlobalItems.Values) {
                if(!IsGun(item)) continue;
                PatchGun(item, logger);
            }
            logger.LogInfo("Finished patching " + count + " guns");
        }

        public static void PatchGun(ItemInfo item, ManualLogSource logger) {


        }
    }
}
