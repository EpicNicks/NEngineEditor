using System.Reflection;
using System.Windows.Forms;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

using NEngine.GameObjects;
using NEngine.Window;
using NEngineEditor.ViewModel;
using NEngine.CoreLibs.Debugging;
using NEngineEditor.Managers;

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
        // TODO: Zoom the RenderWindow views
        //  inspector level GameObjects will have a pseudo-field for the RenderLayer to use to add it to the scene with
        //  thereby, the RenderWindow will store a List<(RenderLayer, GameObject)> to suppy to generated Scene objects's constructors.

        // hard values for now
        // e.Delta will be 1 for a scroll up, -1 for a scroll down
        float zoomDelta = e.Delta == 1 ? 0.5f : 2.0f;
        _nengineApplication.GameWindow.MainView.Zoom(zoomDelta);
        _curZoom *= zoomDelta;
    }

    Vector2i? initialDragPoint = null;
    private const float DoubleClickTimeThresholdSeconds = 0.5f;
    private DateTime lastClickTime = DateTime.MinValue;
    private Mouse.Button? lastClickButton = null;
    private bool doubleClickProcessed = false;
    void _renderWindow_MouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        DateTime currentTime = DateTime.Now;
        bool isDoubleClick = e.Button == lastClickButton && (currentTime - lastClickTime).TotalSeconds <= DoubleClickTimeThresholdSeconds && !doubleClickProcessed;

        if (e.Button == Mouse.Button.Left)
        {
            // try check for what is on screen at that point, for multiple hits, cycle in a predictable order if Z component is 0 (Vector2f cast to 3f works here too)
            Vector2f clickPos = _nengineApplication.GameWindow.RenderWindow.MapPixelToCoords(new(e.X, e.Y));
            Vector2f clickCastSize = new(0.1f, 0.1f);
            MainViewModel.LayeredGameObject? selectedLgo = MainViewModel.Instance.SceneGameObjects
                .FirstOrDefault(sgo => sgo.GameObject is Positionable p && p.Collider is not null && p.Collider.Bounds.Intersects(new FloatRect(clickPos, clickCastSize)));
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
        if (initialDragPoint is not null)
        {
            Vector2i currentMousePosition = new Vector2i(e.X, e.Y);
            Vector2f delta = (Vector2f)(initialDragPoint - currentMousePosition);

            _nengineApplication.GameWindow.MainView.Center += delta;

            initialDragPoint = currentMousePosition;
        }
    }

    private void _renderWindow_MouseButtonReleased(object? sender, MouseButtonEventArgs e)
    {
        if (e.Button == Mouse.Button.Right)
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

        _nengineApplication.GameWindow.Render(gameObjectsToRender, DrawGrid, true);

        //  handle culling the invisible ones
        // Draw Debug Lines with transparency to indicate scale on top
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
}