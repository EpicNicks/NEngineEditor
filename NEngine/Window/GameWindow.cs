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

    /// <summary>
    /// Constructs the GameWindow with a SFML RenderWindow to render to.
    /// Assigns an SFML View MainView for GameObjects to be rendered in world-space and an SFML View UIView from the RenderWindow's DefaultView
    /// to render GameObjects in screen-space.
    /// </summary>
    /// <param name="renderWindow">The RenderWindow the GameWindow should manage</param>
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
    /// <summary>
    /// Clears the RenderWindow with the set WindowBackgroundColor, calls beforeDrawAction, 
    /// RenderWindow.Draw each GameObject on its corresponding view, calls afterDrawAction, and then Display on the RenderWindow
    /// </summary>
    /// <param name="layeredGameObjects">GameObjects to render and their layers to render them on</param>
    /// <param name="beforeDrawAction">An optional action to call before the GameObjects are drawn (Draws will always be drawn underneath)</param>
    /// <param name="afterDrawAction">An optional action to call after the GameObjects are drawn (Draws will always be drawn on top)</param>
    public void Render(List<(RenderLayer, GameObject)> layeredGameObjects, Action? beforeDrawAction, Action? afterDrawAction)
    {
        RenderWindow.Clear(WindowBackgroundColor);
        beforeDrawAction?.Invoke();
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
        afterDrawAction?.Invoke();
        RenderWindow.Display();
    }

    /// <summary>
    /// Clears the RenderWindow with the set WindowBackgroundColor, 
    /// RenderWindow.Draw each GameObject on its corresponding view,
    /// and then Display on the RenderWindow
    /// </summary>
    /// <param name="layeredGameObjects">GameObjects to render and their layers to render them on</param>
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
