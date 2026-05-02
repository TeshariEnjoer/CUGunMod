using BepInEx.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CasGunMod.Assets {


    public static class AssetStorage {
        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        private static readonly Dictionary<string, Sprite> _spritesByName = new Dictionary<string, Sprite>();

        private static readonly Dictionary<string, string> _yamlRaw = new Dictionary<string, string>();
        private static readonly Dictionary<string, Dictionary<string, object>> _yamlItems = new Dictionary<string, Dictionary<string, object>>();

        public static Sprite TestSprite { get; private set; }

        public static void LoadAll(string rootFolder, ManualLogSource log) {
            _sprites.Clear();
            _yamlRaw.Clear();
            _yamlItems.Clear();

            if (!Directory.Exists(rootFolder)) {
                log.LogError($"Failed to resolve: {rootFolder}");
                return;
            }

            string[] files = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories);

            foreach (string file in files) {
                string extension = Path.GetExtension(file).ToLower();
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg") {
                    string relativePath = GetRelativePath(file, rootFolder);

                    Sprite sprite = AssetLoader.LoadSprite(file);
                    if (sprite != null) {
                        _sprites[relativePath] = sprite;
                        _spritesByName[sprite.name] = sprite;

                        if (relativePath.Contains("test.png"))
                            TestSprite = sprite;
                    }
                }
                else if (extension == ".yml" || extension == ".yaml") {
                    string relativePath = GetRelativePath(file, rootFolder);
                    try {
                        string raw = File.ReadAllText(file);
                        _yamlRaw[relativePath] = raw;
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build();

                        object parsed = null;
                        try {
                            parsed = deserializer.Deserialize(new StringReader(raw));
                        }
                        catch (System.Exception ex) {
                            log.LogWarning($"Yaml parsing failed for '{relativePath}': {ex.Message}");
                        }

                        string name = Path.GetFileNameWithoutExtension(file);
                        if (parsed != null)
                            _yamlItems[name] = (Dictionary<string, object>)parsed;

                        log.LogInfo($"Loaded yaml: {relativePath}");
                    }
                    catch (System.Exception ex) {
                        log.LogWarning($"Failed to load yaml '{file}': {ex.Message}");
                    }
                }
            }

            log.LogInfo($"Loaded sprites: {_sprites.Count}");
            log.LogInfo($"Loaded yaml files: {_yamlRaw.Count}");
        }

        public static Sprite GetSprite(string relativePath) {
            if (_sprites.TryGetValue(relativePath, out Sprite sprite))
                return sprite;

            Debug.LogWarning($"Failed to resolve path: {relativePath}");
            return null;
        }

        public static Sprite GetSpriteByName(string name) {
            if (_spritesByName.TryGetValue(name, out Sprite sprite))
                return sprite;

            Debug.LogWarning($"Failed to resolve name: {name}");
            return null;
        }

        public static string GetYamlRaw(string relativePath) {
            if (_yamlRaw.TryGetValue(relativePath, out string raw))
                return raw;

            Debug.LogWarning($"Failed to resolve yaml path: {relativePath}");
            return null;
        }

        public static object GetYamlItemByName(string name) {
            if (_yamlItems.TryGetValue(name, out var obj))
                return obj;

            Debug.LogWarning($"Failed to resolve yaml item: {name}");
            return null;
        }

        public static IReadOnlyDictionary<string, object> GetAllYamlItems() {
            return (IReadOnlyDictionary<string, object>)_yamlItems;
        }

        public static string GetYamlItemType(string name) {
            if (!_yamlItems.TryGetValue(name, out var obj) || obj == null)
                return null;

            var dictObj = obj as System.Collections.IDictionary;
            if (dictObj != null) {
                foreach (var key in dictObj.Keys) {
                    if (key != null && key.ToString() == "type") {
                        var val = dictObj[key];
                        return val?.ToString();
                    }
                }
            }

            return null;
        }

        private static string GetRelativePath(string fullPath, string rootPath) {
            if (string.IsNullOrEmpty(rootPath))
                return fullPath;

            string relative = fullPath.Replace(rootPath, "").TrimStart('\\', '/');
            return relative.Replace('\\', '/');
        }

    }

    public static class AssetLoader {
        public static Sprite LoadSprite(string fullPath) {
            if (!File.Exists(fullPath)) {
                Debug.LogError($"Failed to find file: {fullPath}");
                return null;
            }

            byte[] data = File.ReadAllBytes(fullPath);

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            tex.filterMode = FilterMode.Point;
            tex.anisoLevel = 0;

            if (!ImageConversion.LoadImage(tex, data)) {
                Debug.LogError($"Failed to load sprite: {fullPath}");
                Object.Destroy(tex);
                return null;
            }


            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                32f,
                0,
                SpriteMeshType.Tight
            );

            sprite.name = Path.GetFileNameWithoutExtension(fullPath);
            return sprite;
        }
    }
}
