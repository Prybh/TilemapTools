using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace TilemapTools
{
    public static class TilesetImporter
    {
        public struct TilesetImportParameters
        {
            public string name;
            public string sourcePath;
            public string tilesetDir;
            public int pixelsPerUnit;

            public Vector2Int gridCellSize;
            public Vector2Int gridCellOffset;
            public Vector2Int gridCellPadding;

            public bool createTileAssets;
            public bool createTilePalette;

            public bool addToAtlas;
            public SpriteAtlas atlas;

            public bool IsValid()
            {
                if (name == null || name.Length <= 0)
                    return false;

                if (sourcePath == null || sourcePath.Length <= 0 || !File.Exists(sourcePath))
                    return false;

                if (tilesetDir == null || tilesetDir.Length <= 0 || !tilesetDir.StartsWith(Application.dataPath) || tilesetDir.Length <= Application.dataPath.Length)
                    return false;

                if (pixelsPerUnit <= 0.0f)
                    return false;

                if (gridCellSize == Vector2Int.zero)
                    return false;

                return true;
            }
        }

        public static bool Import(TilesetImportParameters parameters)
        {
            if (!parameters.IsValid())
                return false;

            string assetsRelativeTilesetDir = parameters.tilesetDir.Substring(Application.dataPath.Length + 1);

            if (!CreateAssetFolderIfMissing(assetsRelativeTilesetDir, true))
            {
                return false;
            }

            string tilesetSpriteTargetDir = Path.Combine(assetsRelativeTilesetDir, parameters.name);
            if (!CreateAssetFolderIfMissing(tilesetSpriteTargetDir, false))
            {
                return false;
            }

            string tilesetTilesTargetDir = Path.Combine(tilesetSpriteTargetDir, "TileAssets");
            if (!CreateAssetFolderIfMissing(tilesetTilesTargetDir, false))
            {
                return false;
            }

            string imageTargetPath = tilesetSpriteTargetDir + Path.DirectorySeparatorChar + parameters.name + ".png";
            if (!File.Exists(imageTargetPath) || (!File.GetLastWriteTime(parameters.sourcePath).Equals(File.GetLastWriteTime(imageTargetPath))))
            {
                File.Copy(parameters.sourcePath, Application.dataPath + '/' + imageTargetPath, true);
                AssetDatabase.ImportAsset("Assets/" + imageTargetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                Debug.LogError("Cannot find file at " + imageTargetPath);
                return false;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath("Assets" + '/' + imageTargetPath) as TextureImporter;
            if (textureImporter == null)
            {
                Debug.LogError("Cannot find texture file at " + imageTargetPath);
                return false;
            }

            // Tileset texture settings
            TextureImporterSettings textureSettings = new TextureImporterSettings();
            textureImporter.isReadable = true; // Temporarily needed for GenerateGridSpriteRectangles

            textureImporter.ReadTextureSettings(textureSettings);
            textureSettings.spriteMode = (int)SpriteImportMode.Multiple;
            textureSettings.spritePixelsPerUnit = (float)parameters.pixelsPerUnit;
            textureSettings.spriteGenerateFallbackPhysicsShape = true; // TODO : Option ?
            textureSettings.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed; // Needed for GenerateGridSpriteRectangles
            textureImporter.SetTextureSettings(textureSettings);

            EditorUtility.SetDirty(textureImporter);
            AssetDatabase.ImportAsset("Assets/" + imageTargetPath, ImportAssetOptions.ForceSynchronousImport);

            // Slice tiles sprites
            Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>("Assets" + '/' + imageTargetPath);
            if (texture != null)
            {
                if (!texture.isReadable)
                {
                    Debug.LogError("Texture not readable");
                    return false;
                }

                Rect[] rects = UnityEditorInternal.InternalSpriteUtility.GenerateGridSpriteRectangles(texture, Vector2.zero, parameters.gridCellSize, Vector2.zero, false);
                List<SpriteMetaData> spritesheet = new List<SpriteMetaData>();
                for (int i = 0; i < rects.Length; ++i)
                {
                    SpriteMetaData sprite = new SpriteMetaData();
                    sprite.pivot = new Vector2(0.5f, 0.5f);
                    sprite.alignment = (int)SpriteAlignment.Center;
                    sprite.name = texture.name + "_" + i;
                    sprite.rect = rects[i];

                    spritesheet.Add(sprite);
                }

                textureImporter.spritesheet = spritesheet.ToArray();
                textureImporter.isReadable = false;
            }

            EditorUtility.SetDirty(textureImporter);
            AssetDatabase.ImportAsset("Assets/" + imageTargetPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            // Tile assets
            if (parameters.createTileAssets)
            {
                AssetDatabase.StartAssetEditing();
                Tile.ColliderType colliderType = Tile.ColliderType.Grid; // TODO : Option ?
                Sprite[] subSprites = AssetDatabase.LoadAllAssetsAtPath("Assets" + '/' + imageTargetPath).OfType<Sprite>().ToArray();
                List<Tile> tileAssets = new List<Tile>(subSprites.Length);
                foreach (Sprite tileSprite in subSprites)
                {
                    string tilePath = tilesetTilesTargetDir + Path.DirectorySeparatorChar + tileSprite.name + ".asset";

                    Tile tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePath);
                    if (tile == null)
                    {
                        tile = Tile.CreateInstance<Tile>();
                        tile.sprite = tileSprite;
                        tile.colliderType = colliderType;

                        AssetDatabase.CreateAsset(tile, "Assets/" + tilePath);
                    }
                    else if (tile.sprite != tileSprite || tile.colliderType != colliderType)
                    {
                        tile.sprite = tileSprite;
                        tile.colliderType = colliderType;
                        EditorUtility.SetDirty(tile);
                    }

                    tileAssets.Add(tile);
                }
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();

                // Palette
                if (parameters.createTilePalette)
                {
                    GameObject newPaletteGO = new GameObject(texture.name + "_Palette", typeof(Grid));
                    newPaletteGO.GetComponent<Grid>().cellSize = new Vector3(1.0f, 1.0f, 0.0f);
                    GameObject paletteTilemapGO = new GameObject("Layer1", typeof(Tilemap), typeof(TilemapRenderer));
                    paletteTilemapGO.transform.SetParent(newPaletteGO.transform);

                    Tilemap paletteTilemap = paletteTilemapGO.GetComponent<Tilemap>();
                    paletteTilemap.tileAnchor = new Vector2(0.5f, 0.5f);
                    foreach (Tile tile in tileAssets)
                    {
                        if (tile == null || tile.sprite == null)
                        {
                            continue;
                        }
                        Rect rect = tile.sprite.rect;
                        int x = (int)rect.x / parameters.gridCellSize.x;
                        int y = (int)rect.y / parameters.gridCellSize.y;
                        paletteTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    }
                    string palettePath = (tilesetSpriteTargetDir + Path.DirectorySeparatorChar + texture.name + "_Palette.prefab").Replace('\\', '/');
                    bool createdPrefab;
                    GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(newPaletteGO, "Assets/" + palettePath, out createdPrefab);
                    if (!createdPrefab)
                    {
                        Debug.LogError("Failed to create tile palette asset at " + palettePath);
                        return false;
                    }

                    AssetDatabase.SaveAssets();

                    UnityEngine.GameObject.DestroyImmediate(newPaletteGO);

                    // Clear out any old subassets
                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/" + palettePath);
                    for (int i = 0; i < assets.Length; i++)
                    {
                        UnityEngine.Object asset = assets[i];
                        if (!AssetDatabase.IsMainAsset(asset) && asset is GridPalette)
                        {
                            UnityEngine.Object.DestroyImmediate(asset, true);
                        }
                    }

                    GridPalette gridPalette = ScriptableObject.CreateInstance<GridPalette>();
                    gridPalette.cellSizing = GridPalette.CellSizing.Automatic;
                    gridPalette.name = "Palette Settings";
                    AssetDatabase.AddObjectToAsset(gridPalette, newPrefab);
                    AssetDatabase.SaveAssets();
                }
            }

            // Atlas
            if (parameters.addToAtlas && parameters.atlas != null)
            {
                // SpriteAtlasExtensions.Add();
                Reflect.InvokeMethod("UnityEditor.U2D.SpriteAtlasExtensions, UnityEditor", "Add", BindingFlags.Public | BindingFlags.Static, null, new object[] { parameters.atlas, new UnityEngine.Object[] { texture } });

                // SpriteAtlasUtility.PackAtlases()
                Reflect.InvokeMethod("UnityEditor.U2D.SpriteAtlasUtility, UnityEditor", "PackAtlases", BindingFlags.NonPublic | BindingFlags.Static, null, new object[] { new[] { parameters.atlas }, EditorUserBuildSettings.activeBuildTarget });
            }

            return true;
        }

        public static bool CreateAssetFolderIfMissing(string path, bool askPermission)
        {
            string fullPath = Path.Combine(Application.dataPath, path).Replace('\\', '/');
            string projectRelativePath = Path.Combine("Assets", path).Replace('\\', '/');

            if (Directory.Exists(fullPath) && !AssetDatabase.IsValidFolder(projectRelativePath))
            {
                AssetDatabase.ImportAsset(projectRelativePath, ImportAssetOptions.ForceSynchronousImport);
            }

            if (!AssetDatabase.IsValidFolder(projectRelativePath))
            {
                bool ok = (askPermission ? EditorUtility.DisplayDialog(path + " not found", "Create new directory?", "OK", "Cancel") : true);
                if (ok)
                {
                    ok = CreateAssetFolder(path);
                    if (!ok)
                    {
                        Debug.LogError("Target directory " + path + " could not be created!");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("Permission not given to create folder by user");
                    return false;
                }
            }
            return true;
        }

        public static bool CreateAssetFolder(string path)
        {
            path = Path.Combine("Assets/", path);

            string parent = Directory.GetParent(path).ToString().Replace('\\', '/');
            if (parent == Application.dataPath)
            {
                parent = "Assets";
            }
            else if (parent.StartsWith(Application.dataPath))
            {
                parent = "Assets/" + parent.Substring(Application.dataPath.Length + 1);
            }

            string folderName = Path.GetFileName(path);
            string guid = AssetDatabase.CreateFolder(parent.ToString(), folderName);
            return guid != null && guid.Length > 0;
        }
    }
}

