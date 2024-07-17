using System.Reflection;
using System.Windows.Forms;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

using NEngine.GameObjects;
using NEngine.Window;
using NEngineEditor.ViewModel;
using NEngineEditor.Managers;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditViewUserControl.xaml
/// </summary>
public partial class SceneEditViewUserControl : System.Windows.Controls.UserControl
{
    private GameWindow? _gameWindow;
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

        var context = new ContextSettings { DepthBits = 24 };
        _gameWindow = new GameWindow(new RenderWindow(mysurf.Handle));
        _gameWindow.InitStandardWindowEvents();
        _gameWindow.RenderWindow.SetFramerateLimit(120);
        _gameWindow.RenderWindow.MouseButtonPressed += _renderWindow_MouseButtonPressed;
        _gameWindow.RenderWindow.MouseMoved += _renderWindow_MouseMoved;
        _gameWindow.RenderWindow.MouseButtonReleased += _renderWindow_MouseButtonReleased;
        _gameWindow.RenderWindow.MouseWheelScrolled += _renderWindow_MouseWheelScrolled;

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
        if (_gameWindow is null)
        {
            return;
        }
        _gameWindow.MainView.Center = new(x, y);
    }
    public void MoveCameraToPositionable(Positionable positionable)
    {
        if (_gameWindow is null)
        {
            return;
        }
        _gameWindow.MainView.Center = positionable.Position;
    }

    private void _renderWindow_MouseWheelScrolled(object? sender, MouseWheelScrollEventArgs e)
    {
        // TODO: Zoom the RenderWindow views
        //  inspector level GameObjects will have a pseudo-field for the RenderLayer to use to add it to the scene with
        //  thereby, the RenderWindow will store a List<(RenderLayer, GameObject)> to suppy to generated Scene objects's constructors.

        // hard values for now
        // e.Delta will be 1 for a scroll up, -1 for a scroll down
        _gameWindow?.MainView.Zoom(e.Delta == 1 ? 0.5f : 2.0f);
    }

    Vector2i? initialDragPoint = null;
    private const float DoubleClickTimeThresholdSeconds = 0.5f;
    private DateTime lastClickTime = DateTime.MinValue;
    private Mouse.Button? lastClickButton = null;
    private bool doubleClickProcessed = false;
    void _renderWindow_MouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        if (_gameWindow is null)
        {
            return;
        }
        DateTime currentTime = DateTime.Now;
        bool isDoubleClick = e.Button == lastClickButton && (currentTime - lastClickTime).TotalSeconds <= DoubleClickTimeThresholdSeconds && !doubleClickProcessed;

        if (e.Button == Mouse.Button.Left)
        {
            // try check for what is on screen at that point, for multiple hits, cycle in a predictable order if Z component is 0 (Vector2f cast to 3f works here too)
            Vector2f clickPos = _gameWindow.RenderWindow.MapPixelToCoords(new(e.X, e.Y));
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
        if (_gameWindow is null)
        {
            return;
        }
        if (initialDragPoint is not null)
        {
            Vector2i currentMousePosition = new Vector2i(e.X, e.Y);
            Vector2f delta = (Vector2f)(initialDragPoint - currentMousePosition);

            _gameWindow.MainView.Center += delta;

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
        if (_gameWindow is null)
        {
            return;
        }
        if (!ShouldRender)
        {
            return;
        }
        _gameWindow.RenderWindow.DispatchEvents();

        List<(RenderLayer, GameObject)>? gameObjectsToRender = MainViewModel.Instance.SceneGameObjects
                .Where(lgo => lgo.GameObject is not null)
                .Select(lgo => (lgo.RenderLayer, lgo.GameObject))
                .ToList();

        _gameWindow.Render(gameObjectsToRender);

        //  handle culling the invisible ones
        // Draw Debug Lines with transparency to indicate scale on top
    }
}