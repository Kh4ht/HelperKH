using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KH.EditorTools
{
    [CustomEditor(typeof(KHCameraController))]
    [CanEditMultipleObjects]
    public class KHCameraControllerEditor : Editor
    {
        private const string StatePrefix = "KHCameraControllerEditor.";
        private static readonly Color BoundsColor = new(1f, 0.25f, 0.25f, 1f);

        private GUIStyle boldFoldout;
        private bool hasEventSystem = true;

        // Editor-only tool settings.
        private float fitMargin = 1f;
        private float shakeMagnitude = 0.3f;
        private float shakeDuration = 0.25f;
        private SerializedObject sceneSO; // used only by OnSceneGUI

        private SerializedObject pixelSO;   // cached, for reading KHPixelCamera settings
        private bool showZoomPreview = true;
        private const string PreviewKey = StatePrefix + "ZoomPreview";

        #region LIFECYCLE

        private void OnEnable()
        {
            sceneSO = new SerializedObject(target);
            showZoomPreview = SessionState.GetBool(PreviewKey, true);

            RefreshEventSystemCheck();
            EditorApplication.hierarchyChanged += RefreshEventSystemCheck;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= RefreshEventSystemCheck;

            sceneSO?.Dispose();
            sceneSO = null;

            pixelSO?.Dispose();
            pixelSO = null;
        }

        private void RefreshEventSystemCheck()
        {
            hasEventSystem = FindAnyObjectByType<EventSystem>() != null;
        }

        public override bool RequiresConstantRepaint() =>
            Application.isPlaying && !serializedObject.isEditingMultipleObjects;

        #endregion
        #region INSPECTOR

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(P("m_Script"));

            DrawWarnings();

            DrawGeneral();
            DrawBounds();
            DrawPan();
            DrawZoom();
            DrawSmoothing();
            DrawFollow();

            DrawRuntimePanel();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneral()
        {
            bool open = BeginSection("General");
            if (open)
            {
                EditorGUILayout.PropertyField(P("ignoreWhenOverUI"));

                if (P("ignoreWhenOverUI").boolValue && !hasEventSystem)
                    EditorGUILayout.HelpBox("No EventSystem in the scene, so 'Ignore When Over UI' has no effect.", MessageType.Info);
            }
            EndSection(open);
        }

        private void DrawBounds()
        {
            SerializedProperty use = P("useBounds");

            bool open = BeginSection("Bounds", use);
            if (open)
            {
                using (new EditorGUI.DisabledScope(Off(use)))
                {
                    EditorGUILayout.PropertyField(P("worldBounds"));
                    EditorGUILayout.PropertyField(P("limitZoomOutToBounds"));

                    if (!serializedObject.isEditingMultipleObjects)
                        DrawBoundsTools();
                }
            }
            EndSection(open);
        }

        private void DrawBoundsTools()
        {
            SerializedProperty rect = P("worldBounds");

            if (!rect.hasMultipleDifferentValues && (rect.rectValue.width <= 0f || rect.rectValue.height <= 0f))
                EditorGUILayout.HelpBox("Bounds have no size, so they are ignored at runtime.", MessageType.Warning);

            fitMargin = EditorGUILayout.FloatField(new GUIContent("Fit Margin", "Extra space added around the sprites when fitting."), fitMargin);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Fit To Sprites", "Set the bounds to enclose every active SpriteRenderer in the scene.")))
                    FitBoundsToSprites();

                if (GUILayout.Button(new GUIContent("Center On Camera", "Move the bounds so their center is at the camera position.")))
                {
                    Rect r = rect.rectValue;
                    Vector2 cam = ((Component)target).transform.position;
                    r.center = cam;
                    rect.rectValue = r;
                }

                if (GUILayout.Button(new GUIContent("Frame", "Show the bounds in the Scene view.")))
                {
                    Rect r = rect.rectValue;
                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.Frame(new Bounds(r.center, new Vector3(r.width, r.height, 1f)), false);
                }
            }
        }

        private void FitBoundsToSprites()
        {
            SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);

            bool found = false;
            Bounds total = default;

            foreach (SpriteRenderer r in renderers)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r.sprite == null)
                    continue;

                if (!found)
                {
                    total = r.bounds;
                    found = true;
                }
                else
                    total.Encapsulate(r.bounds);
            }

            if (!found)
            {
                Debug.LogWarning("No active SpriteRenderers found in the scene to fit the bounds to.", target);
                return;
            }

            total.Expand(fitMargin * 2f);
            P("worldBounds").rectValue = new Rect(total.min.x, total.min.y, total.size.x, total.size.y);
        }

        private void DrawPan()
        {
            // Drag
            SerializedProperty canDrag = P("canDrag");
            bool open = BeginSection("Drag Pan", canDrag);
            if (open)
            {
                using (new EditorGUI.DisabledScope(Off(canDrag)))
                {
                    DrawDragButtons();
                }
            }
            EndSection(open);

            // Keyboard
            SerializedProperty keyboard = P("keyboardPan");
            open = BeginSection("Keyboard Pan", keyboard);
            if (open)
            {
                using (new EditorGUI.DisabledScope(Off(keyboard)))
                    EditorGUILayout.PropertyField(P("keyboardPanSpeed"));
            }
            EndSection(open);

            // Edge
            SerializedProperty edge = P("edgePan");
            open = BeginSection("Edge Pan", edge);
            if (open)
            {
                using (new EditorGUI.DisabledScope(Off(edge)))
                {
                    EditorGUILayout.PropertyField(P("edgeSizePixels"));
                    EditorGUILayout.PropertyField(P("edgePanSpeed"));
                }
            }
            EndSection(open);
        }

        private void DrawDragButtons()
        {
            Rect row = EditorGUILayout.GetControlRect();
            row = EditorGUI.PrefixLabel(row, new GUIContent("Drag Buttons", "Mouse buttons that pan the camera."));

            int oldIndent = EditorGUI.indentLevel;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 42f;

            float w = row.width / 3f;
            EditorGUI.PropertyField(new Rect(row.x, row.y, w, row.height), P("dragWithRightButton"), new GUIContent("Right"));
            EditorGUI.PropertyField(new Rect(row.x + w, row.y, w, row.height), P("dragWithMiddleButton"), new GUIContent("Middle"));
            EditorGUI.PropertyField(new Rect(row.x + w * 2f, row.y, w, row.height), P("dragWithLeftButton"), new GUIContent("Left"));

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUI.indentLevel = oldIndent;

            bool none = !P("dragWithRightButton").boolValue && !P("dragWithMiddleButton").boolValue && !P("dragWithLeftButton").boolValue
                        && !P("dragWithRightButton").hasMultipleDifferentValues
                        && !P("dragWithMiddleButton").hasMultipleDifferentValues
                        && !P("dragWithLeftButton").hasMultipleDifferentValues;

            if (none)
                EditorGUILayout.HelpBox("Drag Pan is on but no mouse button is selected.", MessageType.Info);
        }

        private void DrawZoom()
        {
            SerializedProperty canZoom = P("canZoom");

            bool open = BeginSection("Zoom", canZoom);
            if (open)
            {
                DrawZoomRange();

                if (!serializedObject.isEditingMultipleObjects)
                    DrawZoomDiagram();

                using (new EditorGUI.DisabledScope(Off(canZoom)))
                {
                    EditorGUILayout.PropertyField(P("zoomStep"));
                    EditorGUILayout.PropertyField(P("zoomTowardsCursor"));
                    EditorGUILayout.PropertyField(P("invertScroll"));
                }

                EditorGUI.BeginChangeCheck();
                showZoomPreview = EditorGUILayout.Toggle(new GUIContent("Show In Scene View", "Draw the zoom rectangles around the camera in the Scene view."), showZoomPreview);
                if (EditorGUI.EndChangeCheck())
                {
                    SessionState.SetBool(PreviewKey, showZoomPreview);
                    SceneView.RepaintAll();
                }
            }
            EndSection(open);
        }

        /// <summary>Size of the view at zoom 1, in world units. Safe to call from OnSceneGUI.</summary>
        private bool TryGetBaseView(out float baseHeight, out float aspect)
        {
            baseHeight = 0f;
            aspect = 1f;

            var comp = target as Component;
            if (comp == null)
                return false;

            if (comp.TryGetComponent(out KHPixelCamera pixelCam))
            {
                if (pixelSO == null || pixelSO.targetObject != pixelCam)
                {
                    pixelSO?.Dispose();
                    pixelSO = new SerializedObject(pixelCam);
                }

                pixelSO.Update();

                Vector2Int reference = pixelSO.FindProperty("referenceResolution").vector2IntValue;
                int ppu = Mathf.Max(1, pixelSO.FindProperty("assetsPPU").intValue);
                if (reference.y <= 0)
                    return false;

                baseHeight = (float)reference.y / ppu;
                aspect = (float)reference.x / reference.y;
                return true;
            }

            if (comp.TryGetComponent(out Camera cam))
            {
                // In Play mode the camera size already includes the current zoom, so undo it.
                float zoom = Application.isPlaying ? ((KHCameraController)target).Zoom : 1f;
                baseHeight = cam.orthographicSize * 2f * (zoom > 0f ? zoom : 1f);
                aspect = cam.aspect;
                return true;
            }

            return false;
        }

        private static readonly Color StartColor = Color.white;
        private static readonly Color MinColor = new(0.3f, 0.85f, 1f);
        private static readonly Color MaxColor = new(1f, 0.6f, 0.15f);
        private static readonly Color EffectiveColor = new(1f, 0.9f, 0.2f);
        private static readonly Color CurrentColor = new(0.4f, 1f, 0.4f);

        private void DrawZoomDiagram()
        {
            if (!TryGetBaseView(out float baseH, out float aspect))
                return;

            float minZ = P("minZoom").floatValue;
            float maxZ = P("maxZoom").floatValue;
            if (minZ <= 0f || maxZ <= 0f)
                return;

            Rect area = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 110f));
            EditorGUI.DrawRect(area, new Color(0f, 0f, 0f, 0.25f));

            // Scale so the biggest rectangle (the min zoom) fits the box.
            float biggestH = baseH / Mathf.Min(minZ, 1f);
            float scale = Mathf.Min((area.height - 16f) / biggestH, (area.width - 16f) / (biggestH * aspect));

            // Largest first, so smaller ones draw on top.
            DrawZoomBox(area, baseH, aspect, scale, minZ, MinColor, $"Min {minZ:0.##}x");
            DrawZoomBox(area, baseH, aspect, scale, 1f, StartColor, "Start 1x");
            DrawZoomBox(area, baseH, aspect, scale, maxZ, MaxColor, $"Max {maxZ:0.##}x");

            var wrap = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            EditorGUILayout.LabelField(
                $"Sees:  Min {ViewText(baseH, aspect, minZ)}   |   Start {ViewText(baseH, aspect, 1f)}   |   Max {ViewText(baseH, aspect, maxZ)}",
                wrap);
        }

        private static string ViewText(float baseH, float aspect, float zoom)
        {
            float h = baseH / zoom;
            return $"{h * aspect:0.#} x {h:0.#} units";
        }

        private static void DrawZoomBox(Rect area, float baseH, float aspect, float scale, float zoom, Color color, string label)
        {
            float h = baseH / zoom * scale;
            float w = h * aspect;
            var r = new Rect(area.center.x - w * 0.5f, area.center.y - h * 0.5f, w, h);

            EditorGUI.DrawRect(r, new Color(color.r, color.g, color.b, 0.07f));

            // Outline.
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), color);

            var style = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = color } };
            GUI.Label(new Rect(r.x + 3f, r.y + 1f, 110f, 14f), label, style);
        }

        private void DrawZoomScene()
        {
            if (!showZoomPreview || Event.current.type != EventType.Repaint)
                return;

            if (!TryGetBaseView(out float baseH, out float aspect))
                return;

            var c = (KHCameraController)target;
            Vector2 center = c.transform.position;

            float minZ = sceneSO.FindProperty("minZoom").floatValue;
            float maxZ = sceneSO.FindProperty("maxZoom").floatValue;
            if (minZ <= 0f || maxZ <= 0f)
                return;

            DrawSceneView(center, baseH, aspect, minZ, MinColor, $"Min zoom {minZ:0.##}x (furthest out)");
            DrawSceneView(center, baseH, aspect, 1f, StartColor, "Start view 1x");
            DrawSceneView(center, baseH, aspect, maxZ, MaxColor, $"Max zoom {maxZ:0.##}x (closest in)");

            // The bounds can force a higher minimum than the one set.
            if (sceneSO.FindProperty("useBounds").boolValue && sceneSO.FindProperty("limitZoomOutToBounds").boolValue)
            {
                Rect bounds = sceneSO.FindProperty("worldBounds").rectValue;
                if (bounds.width > 0f && bounds.height > 0f)
                {
                    float effective = Mathf.Max(minZ, baseH / bounds.height, baseH * aspect / bounds.width);
                    if (effective > minZ + 0.001f)
                        DrawSceneView(center, baseH, aspect, effective, EffectiveColor, $"Real min {effective:0.##}x (limited by bounds)");
                }
            }

            if (Application.isPlaying && c.Zoom > 0f)
                DrawSceneView(center, baseH, aspect, c.Zoom, CurrentColor, $"Current {c.Zoom:0.##}x");
        }

        private static void DrawSceneView(Vector2 center, float baseH, float aspect, float zoom, Color color, string label)
        {
            float h = baseH / zoom;
            float w = h * aspect;
            float x0 = center.x - w * 0.5f, x1 = center.x + w * 0.5f;
            float y0 = center.y - h * 0.5f, y1 = center.y + h * 0.5f;

            Vector3[] corners = { new(x0, y0), new(x0, y1), new(x1, y1), new(x1, y0) };
            Handles.DrawSolidRectangleWithOutline(corners, new Color(color.r, color.g, color.b, 0.03f), color);

            var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = color } };
            Handles.Label(new Vector3(x0, y1), "  " + label, style);
        }

        private void DrawZoomRange()
        {
            SerializedProperty minP = P("minZoom");
            SerializedProperty maxP = P("maxZoom");

            // Slider limits: the pixel camera's own range if there is one.
            float lo = 0.1f, hi = 8f;
            KHPixelCamera pixelCam = null;

            if (!serializedObject.isEditingMultipleObjects)
            {
                ((Component)target).TryGetComponent(out pixelCam);
                if (pixelCam != null)
                {
                    Vector2 range = new SerializedObject(pixelCam).FindProperty("zoomRange").vector2Value;
                    lo = range.x;
                    hi = range.y;
                }
            }

            Rect row = EditorGUILayout.GetControlRect();
            row = EditorGUI.PrefixLabel(row, new GUIContent("Zoom Range", "Min (zoomed out) to Max (zoomed in). 1 = the starting view."));

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float fieldW = 44f;
            Rect minRect = new(row.x, row.y, fieldW, row.height);
            Rect maxRect = new(row.xMax - fieldW, row.y, fieldW, row.height);
            Rect sliderRect = new(minRect.xMax + 4f, row.y, maxRect.x - minRect.xMax - 8f, row.height);

            float min = minP.floatValue, max = maxP.floatValue;

            EditorGUI.showMixedValue = minP.hasMultipleDifferentValues || maxP.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            min = EditorGUI.FloatField(minRect, min);
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, lo, hi);
            max = EditorGUI.FloatField(maxRect, max);

            if (EditorGUI.EndChangeCheck())
            {
                // Typed values may exceed the slider limits; they can never cross each other.
                min = Mathf.Max(0.01f, min);
                max = Mathf.Max(min, max);
                minP.floatValue = min;
                maxP.floatValue = max;
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.indentLevel = oldIndent;

            if (!serializedObject.isEditingMultipleObjects)
                DrawEffectiveMinZoom(minP.floatValue, pixelCam);
        }

        private void DrawEffectiveMinZoom(float minZoom, KHPixelCamera pixelCam)
        {
            if (!P("useBounds").boolValue || !P("limitZoomOutToBounds").boolValue)
                return;

            Rect bounds = P("worldBounds").rectValue;
            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            // Same math as the runtime: the view at zoom 1.
            float baseHeight, aspect;
            if (pixelCam != null)
            {
                var so = new SerializedObject(pixelCam);
                Vector2Int reference = so.FindProperty("referenceResolution").vector2IntValue;
                float ppu = Mathf.Max(1, so.FindProperty("assetsPPU").intValue);
                baseHeight = reference.y / ppu;
                aspect = (float)reference.x / reference.y;
            }
            else
            {
                var cam = ((Component)target).GetComponent<Camera>();
                baseHeight = cam.orthographicSize * 2f;
                aspect = cam.aspect;
            }

            float effective = Mathf.Max(minZoom, baseHeight / bounds.height, baseHeight * aspect / bounds.width);

            if (effective > minZoom + 0.001f)
            {
                DrawFix($"The bounds limit zooming out to {effective:0.##}x, not {minZoom:0.##}x. " +
                        "Zooming out further would show outside the map.",
                        MessageType.Info, "Allow Past Bounds",
                        () => P("limitZoomOutToBounds").boolValue = false);
            }
        }

        private void DrawSmoothing()
        {
            bool open = BeginSection("Smoothing (0 = instant)");
            if (open)
            {
                EditorGUILayout.PropertyField(P("panSmoothTime"));
                EditorGUILayout.PropertyField(P("zoomSmoothTime"));
            }
            EndSection(open);
        }

        private void DrawFollow()
        {
            bool open = BeginSection("Follow");
            if (open)
            {
                EditorGUILayout.PropertyField(P("followTarget"));

                using (new EditorGUI.DisabledScope(P("followTarget").objectReferenceValue == null && !P("followTarget").hasMultipleDifferentValues))
                    EditorGUILayout.PropertyField(P("followOffset"));

                EditorGUILayout.PropertyField(P("releaseFollowOnPan"));
            }
            EndSection(open);
        }

        #endregion
        #region WARNINGS

        private void DrawWarnings()
        {
            SerializedProperty minZoom = P("minZoom");
            SerializedProperty maxZoom = P("maxZoom");

            if (!minZoom.hasMultipleDifferentValues && !maxZoom.hasMultipleDifferentValues && minZoom.floatValue > maxZoom.floatValue)
            {
                DrawFix("Min Zoom is larger than Max Zoom.", MessageType.Error, "Swap", () =>
                {
                    float temp = minZoom.floatValue;
                    minZoom.floatValue = maxZoom.floatValue;
                    maxZoom.floatValue = temp;
                });
            }

            if (serializedObject.isEditingMultipleObjects)
                return;

            var controller = (KHCameraController)target;
            controller.TryGetComponent(out KHPixelCamera pixelCam);

            if (pixelCam == null && controller.TryGetComponent(out Camera cam) && !cam.orthographic)
            {
                DrawFix("This controller is built for orthographic cameras.", MessageType.Warning, "Make Orthographic", () =>
                {
                    Undo.RecordObject(cam, "Make Camera Orthographic");
                    cam.orthographic = true;
                });
            }

            if (pixelCam != null)
            {
                Vector2 range = new SerializedObject(pixelCam).FindProperty("zoomRange").vector2Value;

                if (minZoom.floatValue < range.x || maxZoom.floatValue > range.y)
                {
                    DrawFix($"KHPixelCamera only allows zoom {range.x:0.##} to {range.y:0.##}. Values outside this range make the controller and the pixel camera disagree.",
                            MessageType.Warning, "Match Pixel Camera", () =>
                            {
                                minZoom.floatValue = Mathf.Max(minZoom.floatValue, range.x);
                                maxZoom.floatValue = Mathf.Min(maxZoom.floatValue, range.y);
                            });
                }
            }
        }

        private static void DrawFix(string message, MessageType type, string buttonLabel, Action fix)
        {
            EditorGUILayout.HelpBox(message, type);

            Rect r = EditorGUILayout.GetControlRect(false);
            r.xMin = r.xMax - 150f;

            if (GUI.Button(r, buttonLabel))
                fix();
        }

        #endregion
        #region RUNTIME PANEL

        private void DrawRuntimePanel()
        {
            if (!Application.isPlaying || serializedObject.isEditingMultipleObjects)
                return;

            var c = (KHCameraController)target;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector2Field("Position", c.Position);
                EditorGUILayout.FloatField("Zoom", c.Zoom);
                EditorGUILayout.FloatField("Target Zoom", c.TargetZoom);
                EditorGUILayout.ObjectField("Following", c.FollowTarget, typeof(Transform), true);
            }

            c.InputEnabled = EditorGUILayout.Toggle("Input Enabled", c.InputEnabled);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset View")) c.ResetView();
                if (GUILayout.Button("Reset View (Instant)")) c.ResetView(true);

                using (new EditorGUI.DisabledScope(c.FollowTarget == null))
                {
                    if (GUILayout.Button("Stop Following")) c.StopFollowing();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                shakeMagnitude = EditorGUILayout.FloatField("Shake Magnitude", shakeMagnitude);
                shakeDuration = EditorGUILayout.FloatField("Duration", shakeDuration);

                if (GUILayout.Button("Shake", GUILayout.Width(60f)))
                    c.Shake(shakeMagnitude, shakeDuration);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion
        #region SCENE VIEW

        private void OnSceneGUI()
        {
            if (sceneSO == null || sceneSO.targetObject == null)
                return;

            sceneSO.Update();

            DrawZoomScene();

            if (!sceneSO.FindProperty("useBounds").boolValue)
                return;

            SerializedProperty prop = sceneSO.FindProperty("worldBounds");
            Rect b = prop.rectValue;

            if (b.width <= 0f || b.height <= 0f)
                return;

            Vector3[] corners =
            {
        new(b.xMin, b.yMin), new(b.xMin, b.yMax),
        new(b.xMax, b.yMax), new(b.xMax, b.yMin)
    };

            Handles.DrawSolidRectangleWithOutline(corners, new Color(BoundsColor.r, BoundsColor.g, BoundsColor.b, 0.05f), BoundsColor);
            Handles.Label(new Vector3(b.xMin, b.yMax), $"  Camera Bounds  {b.width:0.##} x {b.height:0.##}");

            float xMin = b.xMin, xMax = b.xMax, yMin = b.yMin, yMax = b.yMax;
            Vector2 c = b.center;

            Handles.color = BoundsColor;
            EditorGUI.BeginChangeCheck();

            // Edges: each moves one side.
            xMin += Slide(new Vector3(b.xMin, c.y), Vector3.left).x - b.xMin;
            xMax += Slide(new Vector3(b.xMax, c.y), Vector3.right).x - b.xMax;
            yMin += Slide(new Vector3(c.x, b.yMin), Vector3.down).y - b.yMin;
            yMax += Slide(new Vector3(c.x, b.yMax), Vector3.up).y - b.yMax;

            // Corners: each moves two sides.
            Vector2 d = Corner(new Vector3(b.xMin, b.yMin)); xMin += d.x; yMin += d.y;
            d = Corner(new Vector3(b.xMin, b.yMax)); xMin += d.x; yMax += d.y;
            d = Corner(new Vector3(b.xMax, b.yMax)); xMax += d.x; yMax += d.y;
            d = Corner(new Vector3(b.xMax, b.yMin)); xMax += d.x; yMin += d.y;

            // Center: moves everything.
            float size = HandleUtility.GetHandleSize(c) * 0.12f;
            Vector3 movedCenter = Handles.FreeMoveHandle(c, size, Vector3.zero, Handles.RectangleHandleCap);
            Vector2 move = (Vector2)movedCenter - c;
            xMin += move.x; xMax += move.x; yMin += move.y; yMax += move.y;

            if (EditorGUI.EndChangeCheck())
            {
                const float minSize = 0.5f;
                xMax = Mathf.Max(xMax, xMin + minSize);
                yMax = Mathf.Max(yMax, yMin + minSize);

                prop.rectValue = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                sceneSO.ApplyModifiedProperties(); // records undo + prefab overrides
            }
        }

        private static Vector3 Slide(Vector3 position, Vector3 direction)
        {
            float size = HandleUtility.GetHandleSize(position) * 0.06f;
            return Handles.Slider(position, direction, size, Handles.DotHandleCap, 0f);
        }

        private static Vector2 Corner(Vector3 position)
        {
            float size = HandleUtility.GetHandleSize(position) * 0.08f;
            Vector3 moved = Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.DotHandleCap);
            return moved - position;
        }

        #endregion
        #region HELPERS

        private SerializedProperty P(string name) => serializedObject.FindProperty(name);

        // True when a toggle is off for every selected object (mixed values keep the fields enabled).
        private static bool Off(SerializedProperty toggle) => !toggle.boolValue && !toggle.hasMultipleDifferentValues;

        private bool BeginSection(string title, SerializedProperty toggle = null)
        {
            boldFoldout ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect row = EditorGUILayout.GetControlRect(false);
            Rect fold = row;
            Rect toggleRect = new(row.xMax - 18f, row.y, 18f, row.height);

            if (toggle != null)
                fold.xMax = toggleRect.x - 4f;

            string key = StatePrefix + title;
            bool open = EditorGUI.Foldout(fold, SessionState.GetBool(key, true), title, true, boldFoldout);
            SessionState.SetBool(key, open);

            if (toggle != null)
            {
                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 0f;

                // BeginProperty keeps prefab-override styling and undo working.
                EditorGUI.BeginProperty(toggleRect, GUIContent.none, toggle);
                EditorGUI.showMixedValue = toggle.hasMultipleDifferentValues;

                EditorGUI.BeginChangeCheck();
                bool value = EditorGUI.Toggle(toggleRect, toggle.boolValue);
                if (EditorGUI.EndChangeCheck())
                    toggle.boolValue = value;

                EditorGUI.showMixedValue = false;
                EditorGUI.EndProperty();

                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUI.indentLevel = oldIndent;
            }

            if (open)
                EditorGUI.indentLevel++;

            return open;
        }

        private static void EndSection(bool open)
        {
            if (open)
                EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }

        #endregion
    }
}