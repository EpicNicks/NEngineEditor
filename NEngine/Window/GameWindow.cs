using SFML.Graphics;
using SFML.System;

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
    public Color WindowBackgroundColor { get; set; } = CORNFLOWER_BLUE;
    private static readonly Color CORNFLOWER_BLUE = new(147, 204, 234);

    public GameWindow(RenderWindow renderWindow)
    {
        RenderWindow = renderWindow;
        MainView = new View(new FloatRect(0, 0, renderWindow.Size.X, renderWindow.Size.Y))
        {
            Viewport = new FloatRect(0, 0, 1, 1)
        };
        UiView = RenderWindow.DefaultView;
    }

    /// <summary>
    /// Adds all standard events the window would use to the RenderWindow such as "Resized"
    /// </summary>
    public void InitStandardWindowEvents()
    {
        RenderWindow.Resized += (sender, sizeEvent) =>
        {
            // Update the viewport of the main view to maintain aspect ratio or full scale
            MainView.Size = new Vector2f(sizeEvent.Width, sizeEvent.Height);  // Optional: maintain aspect ratio
            MainView.Viewport = new FloatRect(0, 0, 1, 1);  // Always render the world view across the entire window

            // Update the UI view to match the new window size for direct screen space mapping
            UiView.Reset(new FloatRect(0, 0, sizeEvent.Width, sizeEvent.Height));

            // Update the window's view to the modified main view after resizing
            RenderWindow.SetView(MainView);
        };
    }

    public void Render(List<(RenderLayer, GameObject)> layeredGameObjects, Action onDrawAction, bool drawBefore)
    {
        RenderWindow.Clear(WindowBackgroundColor);
        if (drawBefore)
        {
            onDrawAction();
        }
        foreach ((RenderLayer renderLayer, GameObject gameObject) in layeredGameObjects)
        {
            if (renderLayer == RenderLayer.NONE)
            {
                continue;
            }
            foreach (var drawable in gameObject.Drawables)
            {
                if (renderLayer != RenderLayer.UI)
                {
                    // convert screen space to world space on non-UI objects?
                    RenderWindow.SetView(MainView);
                }
                else
                {
                    RenderWindow.SetView(UiView);
                }
                RenderWindow.Draw(drawable);
            }
        }
        if (!drawBefore)
        {
            onDrawAction();
        }
        RenderWindow.Display();
    }

    public void Render(List<(RenderLayer, GameObject)> layeredGameObjects)
    {
        RenderWindow.Clear(WindowBackgroundColor);
        foreach ((RenderLayer renderLayer, GameObject gameObject) in layeredGameObjects)
        {
            if (renderLayer == RenderLayer.NONE)
            {
                continue;
            }
            foreach (var drawable in gameObject.Drawables)
            {
                if (renderLayer != RenderLayer.UI)
                {
                    // convert screen space to world space on non-UI objects?
                    RenderWindow.SetView(MainView);
                }
                else
                {
                    RenderWindow.SetView(UiView);
                }
                RenderWindow.Draw(drawable);
            }
        }
        RenderWindow.Display();
    }
}
