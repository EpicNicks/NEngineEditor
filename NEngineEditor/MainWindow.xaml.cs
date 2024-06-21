using System;
using SFML.Graphics;
using SFML.Window;
using SFML.System;

namespace NEngineEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    private RenderWindow _renderWindow;

    public MainWindow()
    {
        InitializeComponent();

        //need to use this to prevent base.OnPaint and base.OnPaintBackground from erasing contents
        var mysurf = new MyDrawingSurface();
        sfmlHost.Child = mysurf;
        SetdoubleBuffered(mysurf); //same results whether or not i do this.

        var context = new ContextSettings { DepthBits = 24 };
        _renderWindow = new RenderWindow(mysurf.Handle);
        _renderWindow.MouseButtonPressed += _renderWindow_MouseButtonPressed;
        _renderWindow.MouseWheelScrolled += _renderWindow_MouseWheelScrolled;

        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60);
        timer.Tick += timer_Tick;
        timer.Start();
    }

    private void _renderWindow_MouseWheelScrolled(object? sender, MouseWheelScrollEventArgs e)
    {
        // TODO: Zoom the RenderWindow views
        //  duplicate the UI View and Main View
        //  inspector level GameObjects will have a pseudo-field for the RenderLayer to use to add it to the scene with
        //  thereby, the RenderWindow will store a List<(RenderLayer, GameObject)> to suppy to generated Scene objects's constructors.
    }

    public static void SetdoubleBuffered(System.Windows.Forms.Control c)
    {
        System.Reflection.PropertyInfo? aProp = typeof(System.Windows.Forms.Control)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        aProp?.SetValue(c, true, null);
    }

    void _renderWindow_MouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        // handle cast to select object in scene
        // for now, selection in the scene editor should be good enough
    }

    void timer_Tick(object? sender, EventArgs e)
    {
        _renderWindow.DispatchEvents();
        _renderWindow.Clear(Color.Blue);
        
        // Draw all objects in scene
        //  handle culling the invisible ones
        // Draw Debug Lines with transparency to indicate scale on top


        _renderWindow.Display();
    }

    public class MyDrawingSurface : System.Windows.Forms.Control
    {
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }
    }
}
