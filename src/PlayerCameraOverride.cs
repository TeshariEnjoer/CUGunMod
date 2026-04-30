using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CasGunMod {
    internal class PlayerCameraOverride {
        

        public static void Patch(Harmony harmony, ManualLogSource log) {
            harmony.PatchAll(typeof(PlayerCameraOverride));
            log.LogInfo("PlayerCamera patched.");
        }

        [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
        [HarmonyPrefix]
        private static bool HandleGunMenu_Prefix() {
            var camera = PlayerCamera.main;
            var body = camera.body;


            if (body == null)
                return false;

            camera.gunMenu.SetActive(false);
            bool hasGun = body.TryGetHoldingItem(out var item) && GunHelpers.IsGun(item);

            var gunHandle = camera.gameObject.GetComponent<GunAimComponent>();

            if (hasGun) {
                if (gunHandle == null) {
                    gunHandle = camera.gameObject.AddComponent<GunAimComponent>();
                    gunHandle.Attach(camera, body);
                } else {
                    gunHandle.Camera = camera;
                    gunHandle.Body = body;
                }

            } else {
                if (gunHandle != null) {
                    GameObject.Destroy(gunHandle);
                }
            }

            return false;
        }
    }
}
