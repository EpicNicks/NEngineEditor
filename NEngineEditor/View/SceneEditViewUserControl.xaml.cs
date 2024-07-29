using System.Reflection;
using System.Windows.Forms;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

using NEngine.GameObjects;
using NEngine.Window;
using NEngine.CoreLibs.GameObjects;
using NEngine.CoreLibs.StandardFonts;

using NEngineEditor.ViewModel;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditViewUserControl.xaml
/// </summary>
public partial class SceneEditViewUserControl : System.Windows.Controls.UserControl
{
    private float _curZoom = 1.0f;
    private NEngine.Application _nengineApplication;
    public bool ShouldRender { get; set; } = true;

    public static SceneEditViewUserControl? LazyInstance
    {
        get;
        private set;
    }

    public SceneEditViewUserControl()
    {
        InitializeComponent();
        DataContext = new SceneEditViewModel();
        LazyInstance = this;

        var mysurf = new Control();
        sfmlHost.Child = mysurf;
        SetDoubleBuffered(mysurf); //same results whether or not I do this.

        _nengineApplication = new NEngine.Application(new RenderWindow(mysurf.Handle));
        _nengineApplication.GameWindow.InitStandardWindowEvents();
        _nengineApplication.GameWindow.RenderWindow.SetFramerateLimit(120);
        _nengineApplication.GameWindow.RenderWindow.MouseButtonPressed += _renderWindow_MouseButtonPressed;
        _nengineApplication.GameWindow.RenderWindow.MouseMoved += _renderWindow_MouseMoved;
        _nengineApplication.GameWindow.RenderWindow.MouseButtonReleased += _renderWindow_MouseButtonReleased;
        _nengineApplication.GameWindow.RenderWindow.MouseWheelScrolled += _renderWindow_MouseWheelScrolled;

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = new TimeSpan(0, 0, 0, 0, 1000 / 120)
        };
        timer.Tick += timer_Tick;
        timer.Start();
    }

    public static void SetDoubleBuffered(Control c)
    {
        PropertyInfo? aProp = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
        aProp?.SetValue(c, true, null);
    }

    public void MoveCameraToPoint(float x, float y)
    {
        _nengineApplication.GameWindow.MainView.Center = new(x, y);
    }
    public void MoveCameraToPositionable(Positionable positionable)
    {
        _nengineApplication.GameWindow.MainView.Center = positionable.Position;
    }

    private void _renderWindow_MouseWheelScrolled(object? sender, MouseWheelScrollEventArgs e)
    {
        float zoomDelta = e.Delta == 1 ? 0.5f : 2.0f;
        if (_curZoom * zoomDelta < 100 && _curZoom * zoomDelta > 0.01)
        {
            _curZoom *= zoomDelta;
            _nengineApplication.GameWindow.MainView.Zoom(zoomDelta);
        }
    }

    Vector2i? initialDragPoint = null;
    private const float DoubleClickTimeThresholdSeconds = 0.5f;
    private DateTime lastClickTime = DateTime.MinValue;
    private Mouse.Button? lastClickButton = null;
    private bool doubleClickProcessed = false;

    private enum DraggingGizmo
    {
        X_POS,
        Y_POS,
        XY_POS,
        ROT,
        X_SCALE,
        Y_SCALE,
        XY_SCALE
    }
    private record SceneObjectDrag(Vector2i initialDragPoint, DraggingGizmo draggingGizmo);
    private SceneObjectDrag? sceneObjectDrag;

    void _renderWindow_MouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        DateTime currentTime = DateTime.Now;
        bool isDoubleClick = e.Button == lastClickButton && (currentTime - lastClickTime).TotalSeconds <= DoubleClickTimeThresholdSeconds && !doubleClickProcessed;

        static bool GizmoIntersects(Shape? shape, FloatRect castRect)
            => shape is not null && shape.GetGlobalBounds().Intersects(castRect);

        if (e.Button == Mouse.Button.Left)
        {
            // try check for what is on screen at that point, for multiple hits, cycle in a predictable order if Z component is 0 (Vector2f cast to 3f works here too)
            Vector2f clickPos = _nengineApplication.GameWindow.RenderWindow.MapPixelToCoords(new(e.X, e.Y));
            Vector2f clickCastSize = new(0.1f, 0.1f);
            FloatRect clickCastRect = new(clickPos, clickCastSize);

            // TODO: First check if click hit any drawn gizmos
            
            if (MainViewModel.Instance.SelectedGameObject is not null && MainViewModel.Instance.SelectedGameObject.GameObject is Positionable)
            {
                if (DataContext is SceneEditViewModel sevm)
                {
                    if (GizmoIntersects(positionSelectButton, clickCastRect))
                    {
                        sevm.ActivatePositionGizmoSet.Execute(null);
                    }
                    else if (GizmoIntersects(rotationSelectButton, clickCastRect))
                    {
                        sevm.ActivateRotationGizmoSet.Execute(null);
                    }
                    else if (GizmoIntersects(scaleSelectButton, clickCastRect))
                    {
                        sevm.ActivateScaleGizmoSet.Execute(null);
                    }
                }

                if (GizmoIntersects(xPositionGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.X_POS);
                }
                else if (GizmoIntersects(yPositionGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.Y_POS);
                }
                else if (GizmoIntersects(xyPositionGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.XY_POS);
                }
                else if (GizmoIntersects(xScaleGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.X_SCALE);
                }
                else if (GizmoIntersects(yScaleGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.Y_SCALE);
                }
                else if (GizmoIntersects(xyScaleGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.XY_SCALE);
                }
                else if (GizmoIntersects(rotationGizmo, clickCastRect))
                {
                    sceneObjectDrag = new(new(e.X, e.Y), DraggingGizmo.ROT);
                }
            }

            MainViewModel.LayeredGameObject? selectedLgo = MainViewModel.Instance.SceneGameObjects
                .FirstOrDefault(sgo => sgo.GameObject is Positionable p && p.Collider is not null && p.Collider.Bounds.Intersects(clickCastRect));
            if (selectedLgo is not null && selectedLgo.GameObject is Positionable selectedPositionable)
            {
                MainViewModel.Instance.SelectedGameObject = selectedLgo;
                if (isDoubleClick)
                {
                    MoveCameraToPositionable(selectedPositionable);
                    doubleClickProcessed = true;
                }
            }
        }
        else if (e.Button == Mouse.Button.Right)
        {
            initialDragPoint = new(e.X, e.Y);
        }
        if (!isDoubleClick)
        {
            lastClickTime = currentTime;
            lastClickButton = e.Button;
            doubleClickProcessed = false;
        }
    }

    private void _renderWindow_MouseMoved(object? sender, MouseMoveEventArgs e)
    {
        if (sceneObjectDrag is not null)
        {
            Vector2i currentMousePosition = new Vector2i(e.X, e.Y);
            Vector2f delta = (Vector2f)(sceneObjectDrag.initialDragPoint - currentMousePosition);
            float scaleScale = 0.1f;

            if (MainViewModel.Instance.SelectedGameObject is not null && MainViewModel.Instance.SelectedGameObject.GameObject is Positionable p)
            {
                if (sceneObjectDrag.draggingGizmo is DraggingGizmo.X_POS)
                {
                    p.Position = p.Position with { X = p.Position.X - delta.X * _curZoom };
                }
                else if (sceneObjectDrag.draggingGizmo is DraggingGizmo.Y_POS)
                {
                    p.Position = p.Position with { Y = p.Position.Y - delta.Y * _curZoom };
                }
                else if (sceneObjectDrag.draggingGizmo is DraggingGizmo.XY_POS)
                {
                    p.Position -= delta * _curZoom;
                }
                else if (sceneObjectDrag.draggingGizmo is DraggingGizmo.X_SCALE)
                {
                    p.Scale = p.Scale with { X = p.Scale.X - delta.X * scaleScale };
                }
                else if (sceneObjectDrag.draggingGizmo is DraggingGizmo.Y_SCALE)
                {
                    p.Scale = p.Scale with { Y = p.Scale.Y - delta.Y * scaleScale };
                }
                else if (sceneObjectDrag.draggingGizmo is DraggingGizmo.XY_SCALE)
                {
                    p.Scale -= delta * scaleScale;
                }
                else if (sceneObjectDrag.draggingGizmo is DraggingGizmo.ROT)
                {
                    p.Rotation -= delta.X;
                }
                // notify the SelectedGameObject was changed
                MainViewModel.Instance.SelectedGameObject = MainViewModel.Instance.SelectedGameObject;
            }

            sceneObjectDrag = sceneObjectDrag with { initialDragPoint = currentMousePosition };
        }
        if (initialDragPoint is not null)
        {
            Vector2i currentMousePosition = new Vector2i(e.X, e.Y);
            Vector2f delta = (Vector2f)(initialDragPoint - currentMousePosition);

            _nengineApplication.GameWindow.MainView.Center += delta * _curZoom;

            initialDragPoint = currentMousePosition;
        }
    }

    private void _renderWindow_MouseButtonReleased(object? sender, MouseButtonEventArgs e)
    {
        if (e.Button is Mouse.Button.Left)
        {
            sceneObjectDrag = null;
        }
        if (e.Button is Mouse.Button.Right)
        {
            initialDragPoint = null;
        }
    }

    void timer_Tick(object? sender, EventArgs e)
    {
        if (!ShouldRender)
        {
            return;
        }
        _nengineApplication.GameWindow.RenderWindow.DispatchEvents();

        List<(RenderLayer, GameObject)>? gameObjectsToRender = MainViewModel.Instance.SceneGameObjects
                .Where(lgo => lgo.GameObject is not null)
                .Select(lgo => (lgo.RenderLayer, lgo.GameObject))
                .ToList();

        _nengineApplication.GameWindow.Render(gameObjectsToRender, DrawGrid, DrawGizmos);

        //  handle culling the invisible ones
        // Draw Debug Lines with transparency to indicate scale on top
    }


    private RectangleShape? positionSelectButton;
    private Text? positionSelectText;
    private RectangleShape? rotationSelectButton;
    private Text? rotationSelectText;
    private RectangleShape? scaleSelectButton;
    private Text? scaleSelectText;

    private CircleShape? xPositionGizmo;
    private CircleShape? yPositionGizmo;
    private RectangleShape? xPositionGizmoRect;
    private RectangleShape? yPositionGizmoRect;
    private RectangleShape? xyPositionGizmo;

    private CircleShape? rotationGizmo;

    private RectangleShape? xScaleGizmo;
    private RectangleShape? yScaleGizmo;
    private RectangleShape? xyScaleGizmo;
    private void DrawGizmos()
    {
        MainViewModel.LayeredGameObject? selectedGameObject = MainViewModel.Instance.SelectedGameObject;
        if (selectedGameObject is null || selectedGameObject.GameObject is UIAnchored || selectedGameObject.GameObject is not Positionable selectedPositionable || DataContext is not SceneEditViewModel sevm)
        {
            return;
        }
        Vector2f selectedPositionableScreenSpacePosition = (Vector2f)_nengineApplication.GameWindow.RenderWindow.MapCoordsToPixel(selectedPositionable.Position, _nengineApplication.GameWindow.MainView);
        _nengineApplication.GameWindow.RenderWindow.SetView(_nengineApplication.GameWindow.UiView);

        List<Drawable> toDraw = [];

        // hot-reloading the text instances corrupts memory, probably due to failing to load the Resources from NEngine.CoreLibs.StandardFonts
        positionSelectButton = new RectangleShape(new Vector2f(20, 20))
        {
            Position = new(20, 20),
            FillColor = new Color(0, 255, 255, 64),
            OutlineThickness = 2,
            OutlineColor = sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.POSITION ? Color.Cyan : Color.White
        };
        positionSelectText = GizmosConstants.positionSelectText;
        positionSelectText.Position = positionSelectButton.Position - new Vector2f(0, 5);
        positionSelectText.FillColor = sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.POSITION ? Color.Cyan : Color.White;

        rotationSelectButton = new RectangleShape(new Vector2f(20, 20))
        {
            Position = positionSelectButton.Position with { X = positionSelectButton.Position.X + 30 },
            FillColor = new Color(0, 255, 255, 64),
            OutlineThickness = 2,
            OutlineColor = sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.ROTATION ? Color.Blue : Color.White
        };
        rotationSelectText = GizmosConstants.rotationSelectText;
        rotationSelectText.Position = rotationSelectButton.Position - new Vector2f(0, 5);
        rotationSelectText.FillColor = sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.ROTATION ? Color.Blue : Color.White;

        scaleSelectButton = new RectangleShape(new Vector2f(20, 20))
        {
            Position = rotationSelectButton.Position with { X = rotationSelectButton.Position.X + 30 },
            FillColor = new Color(0, 255, 255, 64),
            OutlineThickness = 2,
            OutlineColor = sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.SCALE ? Color.Green : Color.White
        };
        scaleSelectText = GizmosConstants.scaleSelectText;
        scaleSelectText.Position = scaleSelectButton.Position - new Vector2f(0, 5);
        scaleSelectText.FillColor = sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.SCALE ? Color.Green : Color.White;

        toDraw.AddRange([positionSelectButton, positionSelectText, rotationSelectButton, rotationSelectText, scaleSelectButton, scaleSelectText]);

        if (sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.POSITION)
        {
            xPositionGizmoRect = GizmosConstants.XPositionGizmoRect;
            xPositionGizmoRect.Position += selectedPositionableScreenSpacePosition;
            yPositionGizmoRect = GizmosConstants.YPositionGizmoRect;
            yPositionGizmoRect.Position += selectedPositionableScreenSpacePosition;
            xPositionGizmo = GizmosConstants.XPositionGizmoTriangle;
            xPositionGizmo.Position = selectedPositionableScreenSpacePosition + new Vector2f(-15f + xPositionGizmoRect.Size.X, -5f);
            yPositionGizmo = GizmosConstants.YPositionGizmoTriangle;
            yPositionGizmo.Position = selectedPositionableScreenSpacePosition + new Vector2f(-15f, -20f + yPositionGizmoRect.Size.Y);
            xyPositionGizmo = GizmosConstants.XYPositionGizmo;
            xyPositionGizmo.Position += selectedPositionableScreenSpacePosition - new Vector2f(-2f, 2f + xyPositionGizmo.Size.Y);

            toDraw.AddRange([xPositionGizmo, yPositionGizmo, xPositionGizmoRect, yPositionGizmoRect, xyPositionGizmo]);
        }
        else
        {
            xPositionGizmo = null;
            yPositionGizmo = null;
            xPositionGizmoRect = null;
            yPositionGizmoRect = null;
            xyPositionGizmo = null;
        }
        if (sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.ROTATION)
        {
            // TODO: set rotation gizmos
            float radius = 20;
            float endAngle = 90;
            float startAngle = 0;
            Color circleColor = Color.Blue;
            rotationGizmo = new CircleShape(radius)
            {
                Position = selectedPositionableScreenSpacePosition - new Vector2f(radius, radius),
                Rotation = startAngle,
                FillColor = Color.Transparent,
                OutlineThickness = 3,
                OutlineColor = circleColor
            };

            float endRadians = endAngle * (float)Math.PI;
            Vector2f endPoint = selectedPositionableScreenSpacePosition + new Vector2f(
                (float)Math.Cos(endRadians) * radius,
                (float)Math.Sin(endRadians) * radius
            );

            toDraw.AddRange([rotationGizmo]);
        }
        else
        {
            rotationGizmo = null;
        }
        if (sevm.ActiveGizmos is SceneEditViewModel.ActiveGizmoSet.SCALE)
        {
            xScaleGizmo = GizmosConstants.XScaleGizmo;
            yScaleGizmo = GizmosConstants.YScaleGizmo;
            xyScaleGizmo = GizmosConstants.XYScaleGizmo;
            xScaleGizmo.Position += selectedPositionableScreenSpacePosition + new Vector2f(xyScaleGizmo.Size.X, 0);
            yScaleGizmo.Position += selectedPositionableScreenSpacePosition + new Vector2f(0, xyScaleGizmo.Size.Y + xScaleGizmo.Size.Y);
            xyScaleGizmo.Position += selectedPositionableScreenSpacePosition - new Vector2f(0, -xScaleGizmo.Size.Y);

            toDraw.AddRange([xScaleGizmo, yScaleGizmo, xyScaleGizmo]);
        }
        else
        {
            xScaleGizmo = null;
            yScaleGizmo = null;
            xyScaleGizmo = null;
        }

        toDraw.ForEach(_nengineApplication.GameWindow.RenderWindow.Draw);
    }

    private void DrawGrid()
    {
        const float gridSpacing = 100f;
        Color gridColor = new(128, 128, 128, 128);

        RenderWindow window = _nengineApplication.GameWindow.RenderWindow;
        SFML.Graphics.View view = _nengineApplication.GameWindow.MainView;
        window.SetView(_nengineApplication.GameWindow.MainView);

        Vector2f viewSize = view.Size;
        Vector2f viewCenter = view.Center;

        float left = viewCenter.X - viewSize.X / 2;
        float right = viewCenter.X + viewSize.X / 2;
        float top = viewCenter.Y - viewSize.Y / 2;
        float bottom = viewCenter.Y + viewSize.Y / 2;

        // Round to nearest gridSpacing
        float startX = (float)Math.Floor(left / gridSpacing) * gridSpacing;
        float startY = (float)Math.Floor(top / gridSpacing) * gridSpacing;

        // Logger.LogInfo(startX, right, gridSpacing, "number of vertical lines", (right - startX) / gridSpacing);

        // Vertical lines
        for (float x = startX; x <= right; x += gridSpacing)
        {
            Vertex[] line =
            [
                new Vertex(new Vector2f(x, top), gridColor),
                new Vertex(new Vector2f(x, bottom), gridColor)
            ];
            window.Draw(line, PrimitiveType.Lines);
        }

        // Horizontal lines
        for (float y = startY; y <= bottom; y += gridSpacing)
        {
            Vertex[] line =
            [
                new Vertex(new Vector2f(left, y), gridColor),
                new Vertex(new Vector2f(right, y), gridColor)
            ];
            window.Draw(line, PrimitiveType.Lines);
        }
    }

    private static class GizmosConstants
    {
        public static Text positionSelectText = new Text("P", Fonts.Arial) { CharacterSize = 24 };
        public static Text rotationSelectText = new Text("R", Fonts.Arial) { CharacterSize = 24 };
        public static Text scaleSelectText = new Text("S", Fonts.Arial) { CharacterSize = 24 };

        #region Position Gizmos
        public static CircleShape XPositionGizmoTriangle => new(15, 3) { FillColor = Color.Red, Rotation = -30 };
        public static RectangleShape XPositionGizmoRect => new() { Size = new(40, 2), FillColor = Color.Red };
        public static CircleShape YPositionGizmoTriangle => new(15, 3) { FillColor = Color.Green };
        public static RectangleShape YPositionGizmoRect => new() { Size = new(2, -40), FillColor = Color.Green };
        public static RectangleShape XYPositionGizmo => new() { Size = new(20, 20), FillColor = new Color(0, 255, 255, 128) };
        #endregion

        #region Rotation Gizmos
        // TODO, more visually complex potentially
        #endregion

        #region ScaleGizmos
        public static RectangleShape XScaleGizmo => new() { Size = new(50, -6), FillColor = Color.Red };
        public static RectangleShape YScaleGizmo => new() { Size = new(6, 50), FillColor = Color.Green };
        public static RectangleShape XYScaleGizmo => new() { Size = new(20, 20), FillColor = Color.Magenta };
        #endregion
    }
}