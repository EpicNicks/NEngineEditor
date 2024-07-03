using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using NEngine.GameObjects;
using NEngine.Window;
using NEngineEditor.Managers;
using NEngineEditor.Model;
using NEngineEditor.ViewModel;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditViewUserControl.xaml
/// </summary>
public partial class SceneEditViewUserControl : System.Windows.Controls.UserControl
{
    private readonly GameWindow _gameWindow;

    public static SceneEditViewUserControl? LazyInstance
    {
        get;
        private set;
    }

    public SceneEditViewUserControl()
    {
        InitializeComponent();
        LazyInstance = this;
        //need to use this to prevent base.OnPaint and base.OnPaintBackground from erasing contents
        var mysurf = new MyDrawingSurface();
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
        _gameWindow.MainView.Center = new(x, y);
    }
    public void MoveCameraToPositionable(Positionable positionable)
    {
        _gameWindow.MainView.Center = positionable.Position;
    }

    public class MyDrawingSurface : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            // base.OnPaint(e);
        }

        protected override void OnPaintBackground(PaintEventArgs pEvent)
        {
            // base.OnPaintBackground(pEvent);
        }
    }

    private void _renderWindow_MouseWheelScrolled(object? sender, MouseWheelScrollEventArgs e)
    {
        // TODO: Zoom the RenderWindow views
        //  inspector level GameObjects will have a pseudo-field for the RenderLayer to use to add it to the scene with
        //  thereby, the RenderWindow will store a List<(RenderLayer, GameObject)> to suppy to generated Scene objects's constructors.

        // hard values for now
        // e.Delta will be 1 for a scroll up, -1 for a scroll down
        _gameWindow.MainView.Zoom(e.Delta == 1 ? 0.5f : 2.0f);
    }

    Vector2i? initialDragPoint = null;
    void _renderWindow_MouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        if (e.Button == Mouse.Button.Left)
        {
            // try check for what is on screen at that point, for multiple hits, cycle in a predictable order if Z component is 0 (Vector2f cast to 3f works here too)
            MessageBox.Show("Clicked Render Window");
        }
        else if (e.Button == Mouse.Button.Right)
        {
            initialDragPoint = new(e.X, e.Y);
        }
    }

    private void _renderWindow_MouseMoved(object? sender, MouseMoveEventArgs e)
    {
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

    private static readonly string[] _specialProperties = ["Position", "Rotation"];
    void timer_Tick(object? sender, EventArgs e)
    {
        _gameWindow.RenderWindow.DispatchEvents();

        // Draw all objects in scene
        List<(RenderLayer, GameObject)> gameObjectsToRender = [];
        foreach ((RenderLayer renderLayer, GameObject gameObject) in MainViewModel.Instance.SceneGameObjects)
        {
            if (gameObject is not null)
            {
                gameObjectsToRender.Add((renderLayer, gameObject));
                // TODO: modify this to use a strong representation which doesn't use a string dictionary as properties,
                //  but a managed instance with an interface to edit the properties of that instance
                //  Collect the public fields and special properties and make them editable in the INSPECTOR (NOT HERE)

                // assign each GameObject in scene a Guid and replace each GameObject script reference with the matching Guid at SERIALIZATION TIME
            }
        }
        Logger.LogInfo($"Rendering {gameObjectsToRender.Count} objects in scene");
        // MainViewModel.Instance.SceneGameObjects = new(MainViewModel.Instance.SceneGameObjects);
        _gameWindow.Render(gameObjectsToRender);

        //  handle culling the invisible ones
        // Draw Debug Lines with transparency to indicate scale on top
    }
}
