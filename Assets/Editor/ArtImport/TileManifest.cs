using System;
using System.IO;
using UnityEngine;

namespace MetroidVania.EditorTools
{
    /// <summary>
    /// <c>&lt;texture&gt;.tiles.json</c> written by Tools/ArtSkills/scripts/atlas_to_unity.py from a Pixelbolt atlas.
    /// Rects use texture pixels with a bottom-left origin; collision points are pixels relative to the
    /// sprite rect centre, y up (the space ISpritePhysicsOutlineDataProvider expects).
    /// </summary>
    [Serializable]
    public class TileManifest
    {
        public int ppu = 32;
        public string autotile = "";   // "blob" = 47-tile blob set → Rule Tile, "" = one Tile per sprite
        public string collider = "sprite"; // "sprite" | "none"
        public string flushOf = "";    // blob PNG (file name) whose faces this set replaces; no tile asset of its own
        public string flushSide = "";  // "n" | "s" | "ns": the face(s) this set makes plain
        public TileEntry[] tiles = Array.Empty<TileEntry>();

        public const string Suffix = ".tiles.json";
        public const string ArtRoot = "Assets/Art/";

        public static string PathForTexture(string texturePath) =>
            Path.ChangeExtension(texturePath, null) + Suffix;

        public static string TexturePathFor(string manifestPath) =>
            manifestPath.Substring(0, manifestPath.Length - Suffix.Length) + ".png";

        public static TileManifest Load(string manifestPath)
        {
            if (!File.Exists(manifestPath)) return null;
            return JsonUtility.FromJson<TileManifest>(File.ReadAllText(manifestPath));
        }
    }

    [Serializable]
    public class TileEntry
    {
        public string name;
        public int x, y, w, h;
        public int mask = -1;
        public string variant = "";
        public int weight = 1;
        public int sink;               // pixels the sprite is drawn lower than its cell (pivot moves up, collision follows)
        public Polygon[] collision = Array.Empty<Polygon>();
    }

    [Serializable]
    public class Polygon
    {
        public Vector2[] points = Array.Empty<Vector2>();
    }
}
