using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace TilemapTools
{
    public class TilesetImporterWindow : EditorWindow
    {
        private TilesetImporter.TilesetImportParameters parameters;
        private bool cellSizeChanged = false;

        [MenuItem("Window/Tilemap Tools/Tileset Importer")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<TilesetImporterWindow>().Show();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent("Tileset Importer");
            minSize = new Vector2(350, 350);
            maxSize = new Vector2(350, 900);

            /*
            if (GUILayout.Button("Default test values"))
            {
                parameters.sourcePath = "C:/Users/x/Desktop/Tileset.png";
                parameters.tilesetDir = "C:/Users/x/Desktop/UnityProject/Assets/Tilesets";
                parameters.name = "Tileset";
                parameters.pixelsPerUnit = 32;
                parameters.gridCellSize = new Vector2Int(32, 32);
                parameters.createTileAssets = true;
                parameters.createTilePalette = true;
            }
            */

            // Source file
            {
                GUILayout.Space(5);
                string path = DropZone("Drop a tileset .png file here", 340, 80, ".png");
                if (path != null)
                {
                    parameters.sourcePath = path;
                }
                GUILayout.Space(5);
				
				string previousSourcePath = parameters.sourcePath;

                parameters.sourcePath = FileField(parameters.sourcePath, ".png", new GUIContent("Tileset path", "Input tileset path"), 72, 310);

                if (parameters.sourcePath != null && parameters.sourcePath.Length > 0 && (previousSourcePath != parameters.sourcePath || parameters.name == null || parameters.name.Length == 0))
                {
					parameters.name = Path.GetFileNameWithoutExtension(parameters.sourcePath);
                }	
            }

            EditorGUILayout.Separator();

            // Tileset dir
            parameters.tilesetDir = FolderField(parameters.tilesetDir, new GUIContent("Tilesets folder", "Output tilesets folder"), 85, 310);

            EditorGUILayout.Separator();

            // Name
            parameters.name = EditorGUILayout.TextField("Tileset", parameters.name);

            EditorGUILayout.Separator();

            parameters.pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", parameters.pixelsPerUnit);

            EditorGUILayout.Separator();

            bool previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            if (parameters.gridCellSize.x == parameters.pixelsPerUnit && parameters.gridCellSize.y == parameters.pixelsPerUnit)
            {
                cellSizeChanged = false;
            }
            if (!cellSizeChanged)
            {
                parameters.gridCellSize.x = parameters.pixelsPerUnit;
                parameters.gridCellSize.y = parameters.pixelsPerUnit;
            }

            Vector2Int prevCellSize = parameters.gridCellSize;
            parameters.gridCellSize = EditorGUILayout.Vector2IntField("Cell Size", parameters.gridCellSize);
            if (prevCellSize != parameters.gridCellSize)
            {
                cellSizeChanged = true;
            }

            parameters.gridCellOffset = EditorGUILayout.Vector2IntField("Offset", parameters.gridCellOffset);
            parameters.gridCellPadding = EditorGUILayout.Vector2IntField("Padding", parameters.gridCellPadding);

            EditorGUIUtility.wideMode = previousWideMode;

            EditorGUILayout.Separator();

            parameters.createTileAssets = EditorGUILayout.Toggle("Create Tile Assets", parameters.createTileAssets);
            parameters.createTilePalette = EditorGUILayout.Toggle("Create Tile Palette", parameters.createTilePalette);

            EditorGUILayout.Separator();

            parameters.addToAtlas = EditorGUILayout.Toggle("Add to atlas", parameters.addToAtlas);
            if (parameters.addToAtlas)
            {
                parameters.atlas = (SpriteAtlas)EditorGUILayout.ObjectField(new GUIContent("Atlas", "Atlas to pack the tileset into"), parameters.atlas, typeof(SpriteAtlas), false);
            }

            EditorGUILayout.Separator();

            bool pathExists = parameters.sourcePath != null && parameters.sourcePath.Length > 0 && File.Exists(parameters.sourcePath);
            bool tilesetDirValid = parameters.tilesetDir != null && parameters.tilesetDir.Length > 0 && parameters.tilesetDir.StartsWith(Application.dataPath) && parameters.tilesetDir.Length > Application.dataPath.Length;
            bool nameValid = parameters.name != null && parameters.name.Length > 0;

            // Import button
            {
                if (pathExists && tilesetDirValid && nameValid)
                {
                    GUI.backgroundColor = Color.green;
                    GUI.enabled = true;
                }
                else
                {
                    if (!pathExists)
                    {
                        EditorGUILayout.HelpBox("Drop a PNG file from your operating system", MessageType.Warning);
                    }
                    else if (!tilesetDirValid)
                    {
                        if (parameters.tilesetDir == null)
                        {
                            EditorGUILayout.HelpBox("Imported tilesets folder is unset", MessageType.Warning);
                        }
                        else if (parameters.tilesetDir.Length <= Application.dataPath.Length)
                        {
                            EditorGUILayout.HelpBox("Imported tileset folder must be a sub-directory of the project assets folder", MessageType.Warning);
                        }
                        else if (!parameters.tilesetDir.StartsWith(Application.dataPath))
                        {
                            EditorGUILayout.HelpBox("Imported tileset folder must be inside the project assets folder", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Select a folder to store the imported tilesets", MessageType.Warning);
                        }
                    }
                    else if (!nameValid)
                    {
                        EditorGUILayout.HelpBox("Invalid tileset name", MessageType.Warning);
                    }
                    GUI.enabled = false;
                    GUI.backgroundColor = Color.red;
                }
                if (GUILayout.Button(new GUIContent("Import")))
                {
                    EditorApplication.delayCall += DelayedCall;
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
            }
        }

        private void DelayedCall()
        {
            EditorApplication.delayCall -= DelayedCall;
            DoImport();
        }

        private void DoImport()
        {
            Debug.Log("Importing tileset (" + parameters.name + ") ...");

            if (TilesetImporter.Import(parameters))
            {
                EditorUtility.FocusProjectWindow();

                string assetsRelativeTilesetDir = parameters.tilesetDir.Substring(Application.dataPath.Length + 1);
                string tilesetSpriteTargetDir = Path.Combine(assetsRelativeTilesetDir, parameters.name);
                string imageTargetPath = tilesetSpriteTargetDir + Path.DirectorySeparatorChar + parameters.name + ".png";
                Texture2D tileset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + imageTargetPath);
                if (tileset)
                {
                    Debug.Log("Tileset (" + parameters.name + ") imported");
                    Selection.activeObject = tileset;

                    TilesetViewerWindow.ShowTileset(tileset, false);
                }
                else
                {
                    Debug.LogWarning("Couldn't find imported tileset ("  + parameters.name + "). Unhandled error might have occured...");
                }
            }
            else
            {
                Debug.LogError("Importation of tileset ("  + parameters.name + ") failed");
            }
        }

        public static string FileField(string currentValue, string fileExtension, GUIContent labelContent, float labelWidth, float textFieldWidth)
        {
            string output = null;
            using (var scope = new GUILayout.HorizontalScope())
            {
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;
                output = EditorGUILayout.TextField(labelContent, currentValue, GUILayout.Width(textFieldWidth));
                EditorGUIUtility.labelWidth = previousLabelWidth;

                if (GUILayout.Button(EditorGUIUtility.IconContent("Project")))
                {
                    if (fileExtension.StartsWith('.'))
                    {
                        fileExtension = fileExtension.Substring(1);
                    }

                    string newTilesetDir = EditorUtility.OpenFilePanel("Choose directory", "Assets", fileExtension);
                    if (newTilesetDir != null && newTilesetDir.Length > 0)
                    {
                        output = newTilesetDir;
                    }
                }
            }
            return output;
        }

        public static string FolderField(string currentValue, GUIContent labelContent, float labelWidth, float textFieldWidth)
        {
            string output = null;
            using (var scope = new GUILayout.HorizontalScope())
            {
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth;
                output = EditorGUILayout.TextField(labelContent, currentValue, GUILayout.Width(textFieldWidth));
                EditorGUIUtility.labelWidth = previousLabelWidth;

                if (GUILayout.Button(EditorGUIUtility.IconContent("Project")))
                {
                    if (output == null || output.Length == 0)
                    {
                        output = Application.dataPath;
                    }
                    string newDir = EditorUtility.OpenFolderPanel("Choose directory", output, "Tilesets");
                    if (newDir != null && newDir.Length > 0)
                    {
                        output = newDir;
                    }
                }
            }
            return output;
        }

        public static string DropZone(string title, int w, int h, string fileExtension)
        {
            string text = title;
            bool hasWarning = false;

            if ((DragAndDrop.paths == null || DragAndDrop.paths.Length == 0) && DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0) // Don't want this to block regular object dragging
            {
                text = "Drag from your operating system, not from your Unity Project";
                hasWarning = true;
            }
            else if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
            {
            }
            else if (DragAndDrop.paths.Length != 1)
            {
                text = "Drag a single file";
                hasWarning = true;
            }
            else if (Path.GetExtension(DragAndDrop.paths[0]) != fileExtension)
            {
                text = "Drag a file with the extension " + fileExtension;
                hasWarning = true;
            }

            if (hasWarning)
            {
                GUIContent content = EditorGUIUtility.IconContent("Warning");
                content.text = text;
                GUILayout.Box(content, GUILayout.Width(w), GUILayout.Height(h));
                return null;
            }
            else
            {
                GUILayout.Box(text, GUILayout.Width(w), GUILayout.Height(h));

                if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
                    return null;
            }

            EventType eventType = Event.current.type;
            bool isAccepted = false;

            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (eventType == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    isAccepted = true;
                }
                Event.current.Use();
            }

            return isAccepted ? DragAndDrop.paths[0] : null;
        }
    }
}

