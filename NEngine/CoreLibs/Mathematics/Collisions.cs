using SFML.Graphics;
using SFML.System;
using SFML_tutorial.BaseEngine.CoreLibs.Composed;
using static System.Math;
namespace NEngine.CoreLibs.Mathematics;

public class Collisions
{
    public enum SideHit
    {
        NONE,
        TOP,
        BOTTOM,
        LEFT,
        RIGHT
    }

    public static SideHit GetCollisionSide(Collider2D collider1, Collider2D collider2)
    {
        FloatRect rect1 = collider1.Bounds;
        FloatRect rect2 = collider2.Bounds;

        if (!rect1.Intersects(rect2))
        {
            return SideHit.NONE;
        }

        float rect1HalfW = rect1.Width / 2;
        float rect1HalfH = rect1.Height / 2;
        float rect2HalfW = rect2.Width / 2;
        float rect2HalfH = rect2.Height / 2;

        float rect1CenterX = rect1.Left + rect1HalfW;
        float rect1CenterY = rect1.Top + rect1HalfH;
        float rect2CenterX = rect2.Left + rect2HalfW;
        float rect2CenterY = rect2.Top + rect2HalfH;

        float diffX = rect1CenterX - rect2CenterX;
        float diffY = rect1CenterY - rect2CenterY;

        float minXDist = rect1HalfW + rect2HalfW;
        float minYDist = rect1HalfH + rect2HalfH;

        float depthX = diffX > 0 ? minXDist - diffX : -minXDist - diffX;
        float depthY = diffY > 0 ? minYDist - diffY : -minYDist - diffY;

        if (Abs(depthX) < Abs(depthY))
        {
            return depthX > 0 ? SideHit.LEFT : SideHit.RIGHT;
        }
        else
        {
            return depthY > 0 ? SideHit.TOP : SideHit.BOTTOM;
        }
    }

    /// <summary>
    /// Computes the min of all distances from "colliderBox" to "boundingBox" to get 
    /// the minimum distance to where "colliderBox" should be repositioned to in the event of a collision. 
    /// </summary>
    /// <param name="boundingBox">The bounding box of the collider of the GameObject that is being collided against</param>
    /// <param name="colliderBox">The bounding box of the collider of the GameObject to be repositioned</param>
    /// <returns></returns>
    public static Vector2f GetRepositionFromCollision(FloatRect boundingBox, FloatRect colliderBox, out SideHit sideHit)
    {
        // the collider's position is just the top-left of its bounds
        Vector2f originalPosition = new Vector2f(colliderBox.Left, colliderBox.Top);

        // Extract edges from the bounding box
        float left = boundingBox.Left;
        float top = boundingBox.Top;
        float right = boundingBox.Left + boundingBox.Width;
        float bottom = boundingBox.Top + boundingBox.Height;

        // Extract edges from the collider box
        float colliderLeft = colliderBox.Left;
        float colliderTop = colliderBox.Top;
        float colliderRight = colliderBox.Left + colliderBox.Width;
        float colliderBottom = colliderBox.Top + colliderBox.Height;

        // Calculate the distances to each edge
        float distanceToLeft = Abs(colliderRight - left);
        float distanceToRight = Abs(colliderLeft - right);
        float distanceToTop = Abs(colliderBottom - top);
        float distanceToBottom = Abs(colliderTop - bottom);

        // Find the minimum distance edge
        float minDistance = Min(Min(distanceToLeft, distanceToRight), Min(distanceToTop, distanceToBottom));
        
        // Determine which edge to snap to based on the velocity and minimum distance
        if (minDistance == distanceToLeft)
        {
            // Moving right and closest to the left edge
            originalPosition.X = left - colliderBox.Width;
            sideHit = SideHit.LEFT;
        }
        else if (minDistance == distanceToRight)
        {
            // Moving left and closest to the right edge
            originalPosition.X = right;
            sideHit = SideHit.RIGHT;
        }
        else if (minDistance == distanceToTop)
        {
            // Moving down and closest to the top edge
            originalPosition.Y = top - colliderBox.Height;
            sideHit = SideHit.TOP;
        }
        else // if (minDistance == distanceToBottom)
        {
            // Moving up and closest to the bottom edge
            originalPosition.Y = bottom;
            sideHit = SideHit.BOTTOM;
        }

        return originalPosition;
    }
}
