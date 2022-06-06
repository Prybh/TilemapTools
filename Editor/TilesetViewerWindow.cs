using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace TilemapTools
{
    public class TilesetViewerWindow : EditorWindow
    {
        private static TilesetViewerWindow instance = null;

        private Texture2D texture;
        private int textureWidth;
        private int textureHeight;

        private float zoom;
        private Vector2 scrollPosition;
        private Rect textureViewRect;
        private Rect textureRect;

        private Rect selectedSpriteRect;
        private Sprite selectedSprite;
		private bool selectionEnabled;

		private const float k_ScrollbarSize = 16f;

        public delegate void OnSpriteSelected(Sprite sprite);
        private OnSpriteSelected onSpriteSelected;

        public static void ShowTileset(Texture2D texture, bool selectionEnabled)
		{
            instance = EditorWindow.GetWindow<TilesetViewerWindow>();
            instance.texture = texture;
            GetTextureRealWidthAndHeight(texture, ref instance.textureWidth, ref instance.textureHeight);
            instance.titleContent = new GUIContent(texture.name);
            instance.minSize = new Vector2(200, 200);
            instance.selectionEnabled = selectionEnabled;
            instance.zoom = 1.0f;

            // First zoom
            Matrix4x4 previousMatrix = Handles.matrix;
            instance.Initialize();
            instance.Zoom(1.0f);
            Handles.matrix = previousMatrix;

            instance.Show();
        }

        public static void CloseTileset()
        {
            if (instance != null)
            {
                instance.Close();
            }
            instance = null;
        }

        public static Texture2D GetCurrentTexture()
        {
            if (instance != null)
            {
                return EditorWindow.GetWindow<TilesetViewerWindow>().texture;
            }
            return null;
        }

        public static void SetSpriteSelectionCallback(OnSpriteSelected spriteSelectedDelegate)
        {
            if (instance != null)
            {
                instance.onSpriteSelected = spriteSelectedDelegate;
            }
        }

        public static Sprite GetSelectedSprite()
        {
            if (instance != null && instance.selectionEnabled && instance.texture != null)
            {
                string spriteSheet = AssetDatabase.GetAssetPath(instance.texture);
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheet).OfType<Sprite>().ToArray();
                foreach (Sprite s in sprites)
                {
                    if (s.textureRect == instance.selectedSpriteRect)
                    {
                        return s;
                    }
                }
            }
            return null;
        }

        private void OnGUI()
        {
            if (texture == null)
                return;

            Matrix4x4 previousMatrix = Handles.matrix;

			Initialize();
			RenderTileset();
			HandleControls();

			Handles.matrix = previousMatrix;
        }

        private void Initialize()
        {
            // Compute rects
            textureViewRect = new Rect(0f, 0f, position.width - k_ScrollbarSize, position.height - k_ScrollbarSize);
            textureRect = new Rect(0.5f * (textureViewRect.width - textureWidth * zoom), 0.5f * (textureViewRect.height - textureHeight * zoom), textureWidth * zoom, textureHeight * zoom);

            // Matrix setup
            Vector3 handlesPos = new Vector3(textureRect.x, textureRect.yMax, 0f);
            Vector3 handlesScale = new Vector3(zoom, -zoom, 1f);
            Handles.matrix = Matrix4x4.TRS(handlesPos, Quaternion.identity, handlesScale);
        }

		private void RenderTileset()
        {
            Reflect.InvokeMethod("UnityEngine.GUIClip, UnityEngine", "Push", BindingFlags.Static | BindingFlags.NonPublic, null, new object[] { textureViewRect, -scrollPosition, Vector2.zero, false });
            {
                EditorGUI.DrawTextureTransparent(textureRect, texture, ScaleMode.StretchToFill, 0);

                // Draw white boxes
                TextureImporter textureImporter = (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter);
                if (textureImporter != null)
                {
                    BeginLines(new Color(1f, 1f, 1f, 0.3f));
                    foreach (SpriteMetaData spriteData in textureImporter.spritesheet)
                    {
                        DrawBox(spriteData.rect);
                    }
                    EndLines();
                }

				// Draw selected sprite
                if (selectionEnabled && selectedSpriteRect.width > 0 && selectedSpriteRect.height > 0)
                {
                    BeginLines(new Color(0f, 1f, 1f, 0.9f));
						DrawBox(selectedSpriteRect);
                    EndLines();

                    // With offsets to make it more visible
                    BeginLines(new Color(0f, 0f, 1f, 0.4f));
						DrawBox(new Rect(selectedSpriteRect.xMin + 1f / zoom, selectedSpriteRect.yMin + 1f / zoom, selectedSpriteRect.width, selectedSpriteRect.height));
                    EndLines();
                    BeginLines(new Color(0f, 1f, 0f, 0.3f));
						DrawBox(new Rect(selectedSpriteRect.xMin - 1f / zoom, selectedSpriteRect.yMin - 1f / zoom, selectedSpriteRect.width, selectedSpriteRect.height));
                    EndLines();
                }
            }
            Reflect.InvokeMethod("UnityEngine.GUIClip, UnityEngine", "Pop", BindingFlags.Static | BindingFlags.NonPublic, null, null);
        }

		private void HandleControls()
        {
            // Scrollbars
            Rect maxScroll = new Rect(-textureWidth * 0.5f * zoom, -textureHeight * 0.5f * zoom, textureViewRect.width + textureWidth * zoom, textureViewRect.height + textureHeight * zoom);
            scrollPosition.x = GUI.HorizontalScrollbar(new Rect(textureViewRect.xMin, textureViewRect.yMax, textureViewRect.width, k_ScrollbarSize), scrollPosition.x, textureViewRect.width, maxScroll.xMin, maxScroll.xMax);
            scrollPosition.y = GUI.VerticalScrollbar(new Rect(textureViewRect.xMax, textureViewRect.yMin, k_ScrollbarSize, textureViewRect.height), scrollPosition.y, textureViewRect.height, maxScroll.yMin, maxScroll.yMax);

            // Zoom
            if (Event.current.type == EventType.ScrollWheel)
            {
                float zoomMultiplier = 1f - Event.current.delta.y * 0.025f;
                Zoom(zoomMultiplier);
                Event.current.Use();
            }

            // Panning
            if (Event.current.button > 0 && Event.current.type == EventType.MouseDrag && GUIUtility.hotControl == 0)
            {
                scrollPosition -= Event.current.delta;
                Event.current.Use();
            }

            // Selection
			if (selectionEnabled && Event.current.type == EventType.MouseDown && Event.current.button == 0 && GUIUtility.hotControl == 0)
            {
                Vector2 mousePosition = Handles.inverseMatrix.MultiplyPoint3x4(Event.current.mousePosition + scrollPosition);

                selectedSpriteRect = new Rect();
                selectedSprite = null;
                bool clickedSprite = false;

                string texturePath = AssetDatabase.GetAssetPath(texture);
                TextureImporter textureImporter = (AssetImporter.GetAtPath(texturePath) as TextureImporter);
                if (textureImporter != null)
                {
                    foreach (SpriteMetaData spriteData in textureImporter.spritesheet)
                    {
                        if (spriteData.rect.Contains(mousePosition))
                        {
                            clickedSprite = true;
                            selectedSpriteRect = spriteData.rect;
                        }
                    }

                    if (clickedSprite)
                    {
                        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToArray();
                        foreach (Sprite s in sprites)
                        {
                            if (s.rect == selectedSpriteRect)
                            {
                                selectedSprite = s;
                                break;
                            }
                        }

                        // Callback
                        if (onSpriteSelected != null && selectedSprite != null)
                        {
                            onSpriteSelected(selectedSprite);
                        }
                    }
                }

                Event.current.Use();
            }
        }

        private void Zoom(float multiplier)
        {
            float minZoom = Mathf.Min(textureViewRect.width / textureWidth, textureViewRect.height / textureHeight) * 0.75f;
            float newZoom = Mathf.Clamp(zoom * multiplier, minZoom, 50.0f);
            if (newZoom != zoom)
            {
                zoom = newZoom;

                Vector2 mouseGlobal = Event.current != null ? Event.current.mousePosition : Vector2.zero;
                Vector2 mousePosition = Handles.inverseMatrix.MultiplyPoint3x4(mouseGlobal + scrollPosition);
                Vector2 delta = (mousePosition - 0.5f * new Vector2(textureWidth, textureHeight)) * (multiplier - 1f);
                scrollPosition += (Vector2)Handles.matrix.MultiplyVector(delta);
            }
        }

		private static void GetTextureRealWidthAndHeight(Texture2D texture, ref int width, ref int height)
		{
			width = texture ? texture.width : 0;
			height = texture ? texture.height : 0;

			TextureImporter textureImporter = (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter);
			if (textureImporter != null)
            {
                var args = new object[] { width, height };
                Reflect.InvokeMethod(typeof(TextureImporter), "GetWidthAndHeight", BindingFlags.Instance | BindingFlags.NonPublic, textureImporter, args);
                width = (int)args[0];
                height = (int)args[1];
            }
		}

		private static void DrawBox(Rect position)
		{
			Vector3[] array = new Vector3[4];
			int num = 0;
			array[num++] = new Vector3(position.xMin, position.yMin, 0f);
			array[num++] = new Vector3(position.xMax, position.yMin, 0f);
			array[num++] = new Vector3(position.xMax, position.yMax, 0f);
			array[num++] = new Vector3(position.xMin, position.yMax, 0f);
			DrawLine(array[0], array[1]);
			DrawLine(array[1], array[2]);
			DrawLine(array[2], array[3]);
			DrawLine(array[3], array[0]);
		}

		private static void DrawLine(Vector3 p1, Vector3 p2)
		{
			GL.Vertex(p1);
			GL.Vertex(p2);
		}

		private static void BeginLines(Color color)
		{
            // HandleUtility.ApplyWireMaterial();
			// A little harder to call because it has a default parameter... So I made a custom function for it
            ApplyWireMaterial();

			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(GL.LINES);
			GL.Color(color);
		}

		private static void EndLines()
		{
			GL.End();
			GL.PopMatrix();
		}

		private static void ApplyWireMaterial()
		{
			string HandleUtilty = "UnityEditor.HandleUtility, UnityEditor";

			Material handleWireMaterial = (Material)Reflect.GetProperty(HandleUtilty, "handleWireMaterial", BindingFlags.Static | BindingFlags.NonPublic, null, null);
			handleWireMaterial.SetInt("_HandleZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
			handleWireMaterial.SetPass(0);

			int textureIndex2D = (int)Reflect.GetField(HandleUtilty, "s_HandleWireTextureIndex2D", BindingFlags.Static | BindingFlags.NonPublic, null);
			int textureIndexNot2D = (int)Reflect.GetField(HandleUtilty, "s_HandleWireTextureIndex", BindingFlags.Static | BindingFlags.NonPublic, null);

			int samplerIndex2D = (int)Reflect.GetField(HandleUtilty, "s_HandleWireTextureSamplerIndex2D", BindingFlags.Static | BindingFlags.NonPublic, null);
			int samplerIndexNot2D = (int)Reflect.GetField(HandleUtilty, "s_HandleWireTextureSamplerIndex", BindingFlags.Static | BindingFlags.NonPublic, null);

			int textureIndex = (!Camera.current) ? textureIndex2D : textureIndexNot2D;
			int samplerIndex = (!Camera.current) ? samplerIndex2D : samplerIndexNot2D;

			Reflect.InvokeMethod(HandleUtilty, "Internal_SetHandleWireTextureIndex", BindingFlags.Static | BindingFlags.NonPublic, null, new object[] { textureIndex, samplerIndex });
		}
	}
}