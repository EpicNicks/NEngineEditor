using SFML.Graphics;
using SFML.System;
using SFML.Window;

using NEngine.GameObjects;

namespace NEngine.Window;

public class GameWindow
{
    /// <summary>
    /// Used by GameObjects not written to the UI or NONE RenderLayers.
    /// Move around to move the "main camera".
    /// </summary>
    public View MainView { get; private set; }
    /// <summary>
    /// Used by GameObjects written to the UI RenderLayer.
    /// Not really meant to be moved about like MainView but can be.
    /// </summary>
    public View UiView { get; private set; }

    /// <summary>
    /// The underlying SFML.NET RenderWindow object
    /// </summary>
    public RenderWindow RenderWindow { get; private set; }
    /// <summary>
    /// The Color that is set when the window is cleared
    /// </summary>
    public static Color WindowBackgroundColor { get; set; } = CORNFLOWER_BLUE;
    private static readonly Color CORNFLOWER_BLUE = new(147, 204, 234);

    private static string windowTitle;
    public static string WindowTitle
    {
        get => windowTitle;
        set => Instance.RenderWindow.SetTitle(windowTitle = value);
    }
    /// <summary>
    /// A shortener for the common Instance.RenderWindow.Size get
    /// </summary>
    public static Vector2u Size { get => Instance.RenderWindow.Size; set => Instance.RenderWindow.Size = value; }
    /// <summary>
    /// The Aspect Ratio of the window (Width / Height)
    /// </summary>
    public static float AspectRatio => Size.X / Size.Y;

    private static GameWindow? instance;
    public static GameWindow Instance => instance ??= new GameWindow();

    static GameWindow()
    {
        windowTitle = "My Window";
    }

    private GameWindow()
    {
        (uint width, uint height) = (1200, 800);
        RenderWindow = new RenderWindow(new VideoMode(width, height), windowTitle);
        MainView = new View(new FloatRect(0, 0, width, height))
        {
            Viewport = new FloatRect(0, 0, 1, 1)
        };
        UiView = RenderWindow.DefaultView;
    }

    public static void InitStandardWindowEvents()
    {
        Instance.RenderWindow.Resized += (sender, sizeEvent) =>
        {
            // Update the viewport of the main view to maintain aspect ratio or full scale
            Instance.MainView.Size = new Vector2f(sizeEvent.Width, sizeEvent.Height);  // Optional: maintain aspect ratio
            Instance.MainView.Viewport = new FloatRect(0, 0, 1, 1);  // Always render the world view across the entire window

            // Update the UI view to match the new window size for direct screen space mapping
            Instance.UiView.Reset(new FloatRect(0, 0, sizeEvent.Width, sizeEvent.Height));

            // Update the window's view to the modified main view after resizing
            Instance.RenderWindow.SetView(Instance.MainView);
        };
    }


    public static void Render(List<(RenderLayer, GameObject)> layeredGameObjects)
    {
        Instance.RenderWindow.Clear(WindowBackgroundColor);
        foreach ((RenderLayer renderLayer, GameObject gameObject) in layeredGameObjects)
        {
            foreach (var drawable in gameObject.Drawables)
            {
                if (renderLayer == RenderLayer.NONE)
                {
                    return;
                }
                else if (renderLayer != RenderLayer.UI)
                {
                    // convert screen space to world space on non-UI objects?
                    Instance.RenderWindow.SetView(Instance.MainView);
                }
                else
                {
                    Instance.RenderWindow.SetView(Instance.UiView);
                }
                Instance.RenderWindow.Draw(drawable);
            }
        }
        Instance.RenderWindow.Display();
    }
}
