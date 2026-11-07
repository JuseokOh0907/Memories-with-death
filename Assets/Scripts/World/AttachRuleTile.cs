using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MetroidVania
{
    /// <summary>
    /// Rule Tile for Solid that also looks at its sibling tilemaps (same Grid parent) named Hazard* / Deco*.
    /// Where a prop sits on its open top or hangs under its open bottom, the matched sprite is swapped for the
    /// "flush" sprite of the same blob mask: the bottom face runs to the cell edge, the top face goes flat with no tufts.
    /// The flush sprite brings its own physics shape, so the collider follows the picture.
    /// Built by Tools ▸ Art ▸ Build Tiles From Manifests from &lt;png&gt;_Flush{N,S,NS}.tiles.json.
    /// </summary>
    public class AttachRuleTile : RuleTile
    {
        [Tooltip("이 이름으로 시작하는 형제 타일맵(같은 Grid 아래)에 타일이 붙으면 면 모양을 바꾼다")]
        public string[] attachLayers = { "Hazard", "Deco" };

        [Serializable]
        public struct FlushSet
        {
            public int mask;   // blob mask N=1 NE=2 E=4 SE=8 S=16 SW=32 W=64 NW=128
            public Sprite n, s, ns;
        }

        public FlushSet[] flush = Array.Empty<FlushSet>();

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);
            if (flush.Length == 0) return;

            int mask = Mask(position, tilemap);
            bool n = (mask & 1) == 0 && Attached(position + Vector3Int.up, tilemap);
            bool s = (mask & 16) == 0 && Attached(position + Vector3Int.down, tilemap);
            if (!n && !s) return;

            foreach (var f in flush)
            {
                if (f.mask != mask) continue;
                var sprite = n && s ? f.ns : n ? f.n : f.s;
                if (sprite != null) tileData.sprite = sprite;
                return;
            }
        }

        int Mask(Vector3Int p, ITilemap tilemap)
        {
            bool Has(int dx, int dy) => tilemap.GetTile(p + new Vector3Int(dx, dy, 0)) == this;
            bool north = Has(0, 1), east = Has(1, 0), south = Has(0, -1), west = Has(-1, 0);
            int m = (north ? 1 : 0) | (east ? 4 : 0) | (south ? 16 : 0) | (west ? 64 : 0);
            if (north && east && Has(1, 1)) m |= 2;
            if (south && east && Has(1, -1)) m |= 8;
            if (south && west && Has(-1, -1)) m |= 32;
            if (north && west && Has(-1, 1)) m |= 128;
            return m;
        }

        bool Attached(Vector3Int cell, ITilemap tilemap)
        {
            var own = tilemap.GetComponent<Tilemap>();
            var parent = own != null ? own.transform.parent : null;
            if (parent == null) return false;
            foreach (Transform child in parent)
            {
                if (child == own.transform || !IsAttachLayer(child.name)) continue;
                if (child.TryGetComponent(out Tilemap other) && other.HasTile(cell)) return true;
            }
            return false;
        }

        bool IsAttachLayer(string name)
        {
            foreach (var prefix in attachLayers)
                if (!string.IsNullOrEmpty(prefix) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // A tile painted or erased on Hazard/Deco changes the face of the Solid tile above and below it,
        // but Rule Tiles only refresh neighbours on their own tilemap. Refresh across siblings here.
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            Tilemap.tilemapTileChanged -= OnTilemapTileChanged;
            Tilemap.tilemapTileChanged += OnTilemapTileChanged;
        }

        static void OnTilemapTileChanged(Tilemap changed, Tilemap.SyncTile[] tiles)
        {
            var parent = changed != null ? changed.transform.parent : null;
            if (parent == null) return;
            foreach (Transform child in parent)
            {
                if (child == changed.transform || !child.TryGetComponent(out Tilemap other)) continue;
                foreach (var t in tiles)
                {
                    var above = t.position + Vector3Int.up;
                    var below = t.position + Vector3Int.down;
                    if (other.GetTile(above) is AttachRuleTile) other.RefreshTile(above);
                    if (other.GetTile(below) is AttachRuleTile) other.RefreshTile(below);
                }
            }
        }
    }
}
