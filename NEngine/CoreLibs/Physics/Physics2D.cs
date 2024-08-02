using SFML.Graphics;
using SFML.System;

namespace NEngine.CoreLibs.Physics;
/// <summary>
/// A static API for accessing 2D Physics and World-Space related queries
/// </summary>
public static class Physics2D
{
    public static bool LineCast(Vector2f positionFrom, Vector2f direction, float maxDistance)
    {
        return LineCast(positionFrom, direction, maxDistance, out Collider2D? _);
    }

    public static bool LineCast(Vector2f positionFrom, Vector2f direction, float maxDistance, out Collider2D? firstCollidedWith)
    {
        firstCollidedWith = null;
        if (Application.Instance is null)
        {
            // called from outside a running game
            return false;
        }
        const int steps = 100;
        float stepSize = maxDistance / steps;

        for (int i = 0; i <= steps; i++)
        {
            Vector2f currentPosition = positionFrom + direction * (i * stepSize);

            foreach (Collider2D? collider in Application.Instance.ActiveGameObjects.Select(go => go.Collider))
            {
                if (collider is null)
                {
                    continue;
                }
                if (collider.Bounds.Contains(currentPosition))
                {
                    firstCollidedWith = collider;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool BoxCast(Vector2f positionFrom, Vector2f direction, float maxDistance, float width)
    {
        return BoxCast(positionFrom, direction, maxDistance, width, out Collider2D? _);
    }

    public static bool BoxCast(Vector2f positionFrom, Vector2f direction, float maxDistance, float width, out Collider2D? firstCollidedWith)
    {
        firstCollidedWith = null;
        if (Application.Instance is null)
        {
            // called from outside a running game
            return false;
        }
        const int steps = 100; // Number of steps for the iteration
        float stepSize = maxDistance / steps;
        float angle = (float)Math.Atan2(direction.Y, direction.X); // Calculate the angle from the direction

        for (int i = 0; i <= steps; i++)
        {
            Vector2f currentPosition = positionFrom + direction * (i * stepSize);
            Vector2f[] rotatedRectangle = CreateRotatedRectangle(currentPosition, width, stepSize, angle);

            foreach (Collider2D? collider in Application.Instance.ActiveGameObjects.Select(go => go.Collider))
            {
                if (collider is null)
                {
                    continue;
                }
                if (CheckCollision(rotatedRectangle, collider))
                {
                    firstCollidedWith = collider;
                    return true;
                }
            }
        }

        return false;
    }


    private static Vector2f[] CreateRotatedRectangle(Vector2f position, float width, float height, float angle)
    {
        Vector2f[] corners = new Vector2f[4];

        float halfWidth = width / 2;
        float halfHeight = height / 2;

        corners[0] = new Vector2f(-halfWidth, -halfHeight);
        corners[1] = new Vector2f(halfWidth, -halfHeight);
        corners[2] = new Vector2f(halfWidth, halfHeight);
        corners[3] = new Vector2f(-halfWidth, halfHeight);

        for (int i = 0; i < 4; i++)
        {
            float x = corners[i].X;
            float y = corners[i].Y;

            corners[i].X = x * (float)Math.Cos(angle) - y * (float)Math.Sin(angle) + position.X;
            corners[i].Y = x * (float)Math.Sin(angle) + y * (float)Math.Cos(angle) + position.Y;
        }

        return corners;
    }
    private static bool CheckCollision(Vector2f[] rectangle, Collider2D collider)
    {
        foreach (Vector2f point in rectangle)
        {
            if (collider.Bounds.Intersects(new FloatRect(point.X, point.Y, 1, 1)))
            {
                return true;
            }
        }
        return false;
    }
}
