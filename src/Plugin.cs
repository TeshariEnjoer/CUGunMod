using BepInEx;
using BepInEx.Logging;
using CasGunMod.Assets;
using System.IO;
using System.Reflection;

namespace CasGunMod
{
    [BepInPlugin("org.bepinex.plugins.gun_mod", "[FEN] Gun Mod", "1.0.0")]
    public class Plugin : BaseUnityPlugin {

        private void Awake() {

            Logger.LogInfo("Loadig assets");
            LoadAssets(Logger);
            var harmony = new HarmonyLib.Harmony("org.bepinex.plugins.gun_mod");

            Logger.LogInfo("Begin patching");
            PlayerCameraOverride.Patch(harmony, Logger);
            GunPatcher.PatchAll(Logger);
        }

        private void LoadAssets(ManualLogSource log) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var assemblyDir = Path.GetDirectoryName(assembly.Location);
            var path = Path.Combine(assemblyDir, "Assets");
            AssetStorage.LoadAll(path, log);
        }
    }
}