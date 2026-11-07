using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace MetroidVania.EditorTools
{
    /// <summary>
    /// Pixel-art import settings for every texture under Assets/Art (docs/art-pipeline/postprocess.md P12):
    /// Sprite, Point filter, no compression, no mipmaps, Clamp, Full Rect mesh, PPU from the manifest (default 32).
    /// When a sibling &lt;name&gt;.tiles.json exists, the texture is sliced from it (sprite names kept stable)
    /// and each sprite gets its custom physics shape from the manifest's collision polygons.
    /// </summary>
    public class PixelArtTexturePostprocessor : AssetPostprocessor
    {
        const int DefaultPpu = 32;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TileManifest.ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            var manifest = TileManifest.Load(TileManifest.PathForTexture(assetPath));

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = manifest != null && manifest.ppu > 0 ? manifest.ppu : DefaultPpu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.spriteImportMode = manifest != null ? SpriteImportMode.Multiple : SpriteImportMode.Single;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            if (manifest != null) ApplyManifest(importer, manifest);
        }

        static void ApplyManifest(TextureImporter importer, TileManifest manifest)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Keep each sprite's ID by name so Rule Tiles and Tile assets keep their references on reimport.
            var existing = new Dictionary<string, GUID>();
            foreach (var r in provider.GetSpriteRects())
                existing.TryAdd(r.name, r.spriteID);

            var rects = new List<SpriteRect>(manifest.tiles.Length);
            foreach (var t in manifest.tiles)
            {
                rects.Add(new SpriteRect
                {
                    name = t.name,
                    rect = new Rect(t.x, t.y, t.w, t.h),
                    alignment = t.sink != 0 ? SpriteAlignment.Custom : SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f + (float)t.sink / t.h),
                    border = Vector4.zero,
                    spriteID = existing.TryGetValue(t.name, out var id) ? id : GUID.Generate(),
                });
            }
            provider.SetSpriteRects(rects.ToArray());

            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

            var physics = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            if (physics != null)
            {
                for (int i = 0; i < rects.Count; i++)
                {
                    var outlines = manifest.tiles[i].collision
                        .Where(p => p.points.Length >= 3)
                        .Select(p => p.points)
                        .ToList();
                    physics.SetOutlines(rects[i].spriteID, outlines);
                }
            }

            provider.Apply();
        }

        // A changed manifest re-slices its texture.
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (!path.StartsWith(TileManifest.ArtRoot) || !path.EndsWith(TileManifest.Suffix)) continue;
                var texture = TileManifest.TexturePathFor(path);
                if (AssetDatabase.LoadMainAssetAtPath(texture) != null)
                    AssetDatabase.ImportAsset(texture, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
