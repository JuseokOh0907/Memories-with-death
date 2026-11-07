using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MetroidVania.EditorTools
{
    /// <summary>
    /// Tools ▸ Art ▸ Build Tiles From Manifests.
    /// For every Assets/Art/**/&lt;name&gt;.tiles.json:
    ///   autotile "blob" → &lt;name&gt;_Rule.asset, a Rule Tile with one rule per blob mask (47),
    ///                      variants as random output (weights by repetition);
    ///                      with flush sets (manifests whose flushOf names this PNG) it is an AttachRuleTile
    ///                      that swaps in the flush sprite where a Hazard/Deco tile touches its top or bottom;
    ///   flushOf set     → no asset of its own (read by the blob set above);
    ///   otherwise       → Tiles/&lt;sprite&gt;.asset, one Tile per sprite (collider Sprite when it has a shape).
    /// Re-running updates the same assets in place, so painted tilemaps keep their references.
    /// </summary>
    public static class TileAssetBuilder
    {
        // Blob mask bits (Pixelbolt): N=1 NE=2 E=4 SE=8 S=16 SW=32 W=64 NW=128.
        // Order matches RuleTile.TilingRule's default m_NeighborPositions.
        static readonly (Vector3Int pos, int bit, int sideA, int sideB)[] Neighbours =
        {
            (new Vector3Int(-1, 1, 0), 128, 1, 64),  // NW needs N and W
            (new Vector3Int(0, 1, 0), 1, 0, 0),      // N
            (new Vector3Int(1, 1, 0), 2, 1, 4),      // NE needs N and E
            (new Vector3Int(-1, 0, 0), 64, 0, 0),    // W
            (new Vector3Int(1, 0, 0), 4, 0, 0),      // E
            (new Vector3Int(-1, -1, 0), 32, 16, 64), // SW needs S and W
            (new Vector3Int(0, -1, 0), 16, 0, 0),    // S
            (new Vector3Int(1, -1, 0), 8, 16, 4),    // SE needs S and E
        };

        [MenuItem("Tools/Art/Build Tiles From Manifests")]
        public static void BuildAll()
        {
            var manifests = AssetDatabase.FindAssets("", new[] { TileManifest.ArtRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(TileManifest.Suffix))
                .Distinct()
                .Select(p => (path: p, manifest: TileManifest.Load(p)))
                .Where(m => m.manifest != null)
                .ToList();

            int built = 0;
            foreach (var (path, manifest) in manifests)
            {
                if (!string.IsNullOrEmpty(manifest.flushOf)) continue;

                var texture = TileManifest.TexturePathFor(path);
                var sprites = SpritesOf(texture);
                if (sprites.Count == 0)
                {
                    Debug.LogWarning($"[TileAssetBuilder] skipped {path}: no sliced sprites in {texture}");
                    continue;
                }

                if (manifest.autotile == "blob")
                {
                    string dir = Path.GetDirectoryName(path)!.Replace('\\', '/');
                    string png = Path.GetFileName(texture);
                    var flushes = manifests
                        .Where(m => m.manifest.flushOf == png && Path.GetDirectoryName(m.path)!.Replace('\\', '/') == dir)
                        .ToList();
                    BuildBlobRuleTile(path, manifest, sprites, flushes);
                }
                else BuildPlainTiles(path, manifest, sprites);
                built++;
            }

            AssetDatabase.SaveAssets();
            // Already painted cells keep their old sprites until refreshed (only redraws; nothing in the scene changes).
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)) tilemap.RefreshAllTiles();
            Debug.Log($"[TileAssetBuilder] built tiles from {built} manifest(s)");
        }

        static Dictionary<string, Sprite> SpritesOf(string texture) =>
            AssetDatabase.LoadAllAssetsAtPath(texture).OfType<Sprite>()
                .GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First());

        static void BuildBlobRuleTile(string manifestPath, TileManifest manifest, Dictionary<string, Sprite> sprites,
            List<(string path, TileManifest manifest)> flushes)
        {
            string assetPath = TileManifest.TexturePathFor(manifestPath).Replace(".png", "_Rule.asset");
            var rule = LoadOrCreate(assetPath, attach: flushes.Count > 0);
            if (rule == null) return;

            var collider = manifest.collider == "none" ? Tile.ColliderType.None : Tile.ColliderType.Sprite;
            var byMask = manifest.tiles.Where(t => t.mask >= 0).GroupBy(t => t.mask).OrderByDescending(g => g.Key);

            rule.m_TilingRules = new List<RuleTile.TilingRule>();
            int id = 0;
            foreach (var group in byMask)
            {
                int mask = group.Key;
                var output = new List<Sprite>();
                foreach (var t in group.OrderBy(t => t.variant))
                    if (sprites.TryGetValue(t.name, out var s))
                        for (int k = 0; k < Mathf.Max(1, t.weight); k++) output.Add(s);
                if (output.Count == 0) continue;

                var r = new RuleTile.TilingRule
                {
                    m_Id = id++,
                    m_Sprites = output.ToArray(),
                    m_Output = output.Count > 1
                        ? RuleTile.TilingRuleOutput.OutputSprite.Random
                        : RuleTile.TilingRuleOutput.OutputSprite.Single,
                    m_ColliderType = collider,
                    m_NeighborPositions = Neighbours.Select(n => n.pos).ToList(),
                    m_Neighbors = Neighbours.Select(n =>
                    {
                        bool diagonal = n.sideA != 0;
                        if (diagonal && ((mask & n.sideA) == 0 || (mask & n.sideB) == 0))
                            return 0; // don't care: a diagonal only counts when both sides match
                        return (mask & n.bit) != 0
                            ? RuleTile.TilingRuleOutput.Neighbor.This
                            : RuleTile.TilingRuleOutput.Neighbor.NotThis;
                    }).ToList(),
                };
                rule.m_TilingRules.Add(r);
            }

            var full = manifest.tiles.FirstOrDefault(t => t.mask == 255);
            if (full != null && sprites.TryGetValue(full.name, out var def)) rule.m_DefaultSprite = def;
            rule.m_DefaultColliderType = collider;

            string flushNote = "";
            if (rule is AttachRuleTile attach)
            {
                var sets = new Dictionary<int, AttachRuleTile.FlushSet>();
                foreach (var (path, fm) in flushes)
                {
                    var fsprites = SpritesOf(TileManifest.TexturePathFor(path));
                    foreach (var t in fm.tiles)
                    {
                        if (t.mask < 0 || !fsprites.TryGetValue(t.name, out var s)) continue;
                        sets.TryGetValue(t.mask, out var set);
                        set.mask = t.mask;
                        switch (fm.flushSide)
                        {
                            case "n": set.n = s; break;
                            case "s": set.s = s; break;
                            case "ns": set.ns = s; break;
                        }
                        sets[t.mask] = set;
                    }
                }
                attach.flush = sets.Values.OrderByDescending(s => s.mask).ToArray();
                flushNote = $", {attach.flush.Length} flush masks from {flushes.Count} set(s)";
            }

            EditorUtility.SetDirty(rule);
            Debug.Log($"[TileAssetBuilder] {assetPath}: {rule.m_TilingRules.Count} rules{flushNote}");
        }

        /// <summary>
        /// Loads the Rule Tile at <paramref name="assetPath"/>, creating it when missing. When it must be an
        /// AttachRuleTile but is a plain RuleTile, its script reference is swapped in place, so the asset keeps
        /// its GUID and every tilemap already painted with it keeps working.
        /// </summary>
        static RuleTile LoadOrCreate(string assetPath, bool attach)
        {
            var rule = AssetDatabase.LoadAssetAtPath<RuleTile>(assetPath);
            if (rule == null)
            {
                rule = attach ? ScriptableObject.CreateInstance<AttachRuleTile>() : ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(rule, assetPath);
                return rule;
            }
            if (!attach || rule is AttachRuleTile) return rule;

            AssetDatabase.SaveAssetIfDirty(rule);
            var probe = ScriptableObject.CreateInstance<AttachRuleTile>();
            var script = MonoScript.FromScriptableObject(probe);
            Object.DestroyImmediate(probe);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long _))
            {
                Debug.LogError("[TileAssetBuilder] AttachRuleTile script GUID not found");
                return null;
            }

            string file = Path.GetFullPath(assetPath);
            string text = File.ReadAllText(file);
            string swapped = Regex.Replace(text, @"m_Script: \{fileID: 11500000, guid: [0-9a-f]{32}, type: 3\}",
                $"m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}");
            if (swapped == text)
            {
                Debug.LogError($"[TileAssetBuilder] {assetPath}: m_Script line not found, class not changed");
                return null;
            }
            File.WriteAllText(file, swapped);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            rule = AssetDatabase.LoadAssetAtPath<RuleTile>(assetPath);
            if (rule is AttachRuleTile) Debug.Log($"[TileAssetBuilder] {assetPath}: RuleTile → AttachRuleTile (same GUID)");
            else Debug.LogError($"[TileAssetBuilder] {assetPath}: still {rule?.GetType().Name} after the swap; reopen the project and run again");
            return rule;
        }

        static void BuildPlainTiles(string manifestPath, TileManifest manifest, Dictionary<string, Sprite> sprites)
        {
            string dir = Path.Combine(Path.GetDirectoryName(manifestPath)!, "Tiles").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(manifestPath)!.Replace('\\', '/'), "Tiles");

            foreach (var t in manifest.tiles)
            {
                if (!sprites.TryGetValue(t.name, out var sprite)) continue;
                string path = $"{dir}/{t.name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.sprite = sprite;
                tile.colliderType = manifest.collider != "none" && t.collision.Length > 0
                    ? Tile.ColliderType.Sprite
                    : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
            }
        }
    }
}
