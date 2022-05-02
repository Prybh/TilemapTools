using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Tilemaps;
using System.Reflection;

namespace TilemapTools
{
    public class AnimatedTileCreator : EditorWindow
    {
        private class AnimatedTileData
        {
            public string name;
            public float minSpeed = 1.0f;
            public float maxSpeed = 1.0f;
            public float startTime = 0.0f;
            public int startFrame = 0;
            public Tile.ColliderType colliderType = Tile.ColliderType.None;
            public List<Sprite> sprites = new List<Sprite>();

            public bool foldout = true;
        }

        private Texture2D tileset;
        private string exportFolder;

        private Vector2 scrollPosition;
        private Vector2Int currentPickingTile = new Vector2Int(-1, -1);

        private List<AnimatedTileData> animatedTiles = new List<AnimatedTileData>();

        [MenuItem("Window/Tilemap Tools/Animated Tile Creator")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<AnimatedTileCreator>().Show();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent("Animated Tile Creator");
            minSize = new Vector2(300, 500);
            maxSize = new Vector2(300, 1200);

			/*
            if (GUILayout.Button("Default test values"))
            {
                tileset = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Tilesets/Windows/Windows.png", typeof(Texture2D));
                exportFolder = Directory.GetParent(AssetDatabase.GetAssetPath(tileset)).ToString().Replace('\\', '/') + "/AnimatedTileAssets";

                AnimatedTileData data1 = new AnimatedTileData();
                data1.name = "DoorA";
                data1.minSpeed = 1.0f;
                data1.maxSpeed = 2.0f;
                data1.startTime = 3.0f;
                data1.startFrame = 4;
                data1.colliderType = Tile.ColliderType.Sprite;
                data1.sprites.Add(null);
                data1.sprites.Add(null);
                animatedTiles.Add(data1);

                AnimatedTileData data2 = new AnimatedTileData();
                data2.name = "DoorB";
                data2.minSpeed = 5.0f;
                data2.maxSpeed = 6.0f;
                data2.startTime = 7.0f;
                data2.startFrame = 8;
                data2.colliderType = Tile.ColliderType.Grid;
                data2.sprites.Add(null);
                data2.sprites.Add(null);
                data2.sprites.Add(null);
                animatedTiles.Add(data2);
            }
			*/

            string tilesetName = tileset == null ? "Select an animated tileset" : "Tileset: " + tileset.name;
            Texture2D previousTileset = tileset;
            tileset = (Texture2D)EditorGUILayout.ObjectField(tilesetName, tileset, typeof(Texture2D), false);

            bool tilesetChanged = previousTileset == tileset;
            if (tilesetChanged && tileset != null && (previousTileset == null || exportFolder == null || exportFolder == ""))
            {
                exportFolder = Directory.GetParent(AssetDatabase.GetAssetPath(tileset)).ToString().Replace('\\', '/') + "/AnimatedTileAssets";
            }
            exportFolder = TilesetImporterWindow.FolderField(exportFolder, new GUIContent("Export", "Output folder for AnimatedTileAssets"), 80, 262);

            EditorGUILayout.Separator();

            if (tileset != null && GUILayout.Button("Open tileset"))
            {
                TilesetViewerWindow.ShowTileset(tileset, false);
                currentPickingTile = new Vector2Int(-1, -1); // As we disable selection, disable picking
            }

            if (tileset != null)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                EditorGUILayout.BeginVertical("Box");

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Animated tiles: " + animatedTiles.Count);
                if (GUILayout.Button("+"))
                {
                    animatedTiles.Add(new AnimatedTileData());
                }
                if (animatedTiles.Count > 0 && GUILayout.Button("-"))
                {
                    animatedTiles.RemoveAt(animatedTiles.Count - 1);
                }
                GUILayout.FlexibleSpace(); 
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < animatedTiles.Count; ++i)
                {
                    GuiLine();

                    EditorGUILayout.BeginVertical("Box");

                    if (animatedTiles[i].sprites.Count > 0 && animatedTiles[i].sprites[0] != null)
                    {
                        DrawOnGUISprite(animatedTiles[i].sprites[0]);
                    }

                    animatedTiles[i].foldout = EditorGUILayout.Foldout(animatedTiles[i].foldout, animatedTiles[i].name);
                    if (animatedTiles[i].foldout)
                    {
                        animatedTiles[i].name = EditorGUILayout.TextField("Name", animatedTiles[i].name);

                        animatedTiles[i].minSpeed = EditorGUILayout.FloatField("MinSpeed", animatedTiles[i].minSpeed);
                        if (animatedTiles[i].minSpeed < 0.0f)
                        {
                            animatedTiles[i].minSpeed = 0.0f;
                        }

                        animatedTiles[i].maxSpeed = EditorGUILayout.FloatField("MaxSpeed", animatedTiles[i].maxSpeed);
                        if (animatedTiles[i].maxSpeed < animatedTiles[i].minSpeed)
                        {
                            animatedTiles[i].maxSpeed = animatedTiles[i].minSpeed;
                        }

                        animatedTiles[i].startTime = EditorGUILayout.FloatField("StartTime", animatedTiles[i].startTime);
                        if (animatedTiles[i].startTime < 0.0f)
                        {
                            animatedTiles[i].startTime = 0.0f;
                        }

                        animatedTiles[i].startFrame = EditorGUILayout.IntField("StartFrame", animatedTiles[i].startFrame);
                        if (animatedTiles[i].startFrame < 0)
                        {
                            animatedTiles[i].startFrame = 0;
                        }

                        animatedTiles[i].colliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Collider", animatedTiles[i].colliderType);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Sprites: " + animatedTiles[i].sprites.Count);
                        if (GUILayout.Button("+"))
                        {
                            animatedTiles[i].sprites.Add(null);
                        }
                        if (animatedTiles[i].sprites.Count > 0 && GUILayout.Button("-"))
                        {
                            animatedTiles[i].sprites.RemoveAt(animatedTiles[i].sprites.Count - 1);
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        for (int j = 0; j < animatedTiles[i].sprites.Count; ++j)
                        {
                            EditorGUILayout.BeginHorizontal();

                            animatedTiles[i].sprites[j] = (Sprite)EditorGUILayout.ObjectField("Sprite " + j.ToString(), animatedTiles[i].sprites[j], typeof(Sprite), allowSceneObjects: true);

                            if (currentPickingTile == new Vector2Int(i, j) && TilesetViewerWindow.GetCurrentTexture() == tileset)
                            {
                                if (GUILayout.Button(new GUIContent("Picking...", "Pick on Tileset")))
                                {
                                    currentPickingTile = new Vector2Int(i, -1);
                                }
                            }
                            else
                            {
                                if (GUILayout.Button(new GUIContent("Pick", "Pick on Tileset")))
                                {
                                    TilesetViewerWindow.ShowTileset(tileset, true);
                                    TilesetViewerWindow.SetSpriteSelectionCallback(TilesetViewerSpriteSelectedCallback);
                                    currentPickingTile = new Vector2Int(i, j);
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
                GUILayout.EndScrollView();

                if (GUILayout.Button("Create"))
                {
                    EditorApplication.delayCall += DelayedCall;
                }
            }
        }

        private void DelayedCall()
        {
            EditorApplication.delayCall -= DelayedCall;
            DoCreate();
        }

        private void DoCreate()
        {
            bool generatedAnimatedTile = false;
            ScriptableObject lastTile = null;

            string exportFolderTemp = exportFolder;

            for (int i = 0; i < animatedTiles.Count; ++i)
            {
                AnimatedTileData animTileData = animatedTiles[i];
                ScriptableObject tile = ScriptableObject.CreateInstance("AnimatedTile");

                string name = animTileData.name;
                if (name == null || name.Length == 0)
                {
                    name = "AnimatedTile" + i;
                }

                tile.name = name;
                Reflect.SetField("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras", "m_MinSpeed", BindingFlags.Public | BindingFlags.Instance, tile, animTileData.minSpeed);
                Reflect.SetField("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras", "m_MaxSpeed", BindingFlags.Public | BindingFlags.Instance, tile, animTileData.maxSpeed);
                Reflect.SetField("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras", "m_AnimationStartTime", BindingFlags.Public | BindingFlags.Instance, tile, animTileData.startTime);
                Reflect.SetField("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras", "m_AnimationStartFrame", BindingFlags.Public | BindingFlags.Instance, tile, animTileData.startFrame);
                Reflect.SetField("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras", "m_TileColliderType", BindingFlags.Public | BindingFlags.Instance, tile, animTileData.colliderType);
                Reflect.SetField("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras", "m_AnimatedSprites", BindingFlags.Public | BindingFlags.Instance, tile, animTileData.sprites.ToArray());

                exportFolderTemp = exportFolderTemp.Replace('\\', '/');
                if (exportFolderTemp.EndsWith('/'))
                {
                    exportFolderTemp.Substring(0, exportFolderTemp.Length - 1);
                }
                if (exportFolderTemp.StartsWith(Application.dataPath))
                {
                    exportFolderTemp = exportFolderTemp.Substring(Application.dataPath.Length + 1);
                }
                exportFolderTemp = exportFolderTemp.Replace('\\', '/');

                TilesetImporter.CreateAssetFolderIfMissing(exportFolderTemp, true);

                AssetDatabase.CreateAsset(tile, "Assets/" + exportFolderTemp + '/' + tile.name + ".asset");

                generatedAnimatedTile = true;

                if (animatedTiles.Count == 1)
                {
                    lastTile = tile;
                }
            }

            if (generatedAnimatedTile)
            {
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            if (lastTile != null)
            {
                Selection.activeObject = lastTile;
            }
        }

        public static void DrawOnGUISprite(Sprite sprite)
        {
            Rect c = sprite.rect;
            float spriteW = c.width;
            float spriteH = c.height;
            Rect rect = GUILayoutUtility.GetRect(spriteW, spriteH, GUILayout.Width(spriteW), GUILayout.Height(spriteH));

            var tex = sprite.texture;
            c.xMin /= tex.width;
            c.xMax /= tex.width;
            c.yMin /= tex.height;
            c.yMax /= tex.height;
            GUI.DrawTextureWithTexCoords(rect, tex, c);
        }

        private void TilesetViewerSpriteSelectedCallback(Sprite selectedSprite)
        {
            if (TilesetViewerWindow.GetCurrentTexture() == tileset
                && currentPickingTile.x >= 0 && currentPickingTile.x < animatedTiles.Count
                && currentPickingTile.y >= 0 && currentPickingTile.y < animatedTiles[currentPickingTile.x].sprites.Count)
            {
                animatedTiles[currentPickingTile.x].sprites[currentPickingTile.y] = selectedSprite;
                Repaint();
                currentPickingTile = new Vector2Int(-1, -1);
            }
            TilesetViewerWindow.CloseTileset();
        }

        private void GuiLine(int height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}
