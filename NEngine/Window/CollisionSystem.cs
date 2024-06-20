using SFML.System;

using NEngine.CoreLibs.Physics;
using NEngine.GameObjects;

namespace NEngine.Window;
public class CollisionSystem
{
    // not performant to n^2 check every gameobject for colliders but oh well
    // also this is a discrete collision system because it's just righting objects which are inside other colliders
    public void HandleCollisions(List<GameObject> gameObjects)
    {
        bool BothAreTriggers(Collider2D col1, Collider2D col2) => col1.IsTrigger && col2.IsTrigger;
        bool OneIsTrigger(Collider2D col1, Collider2D col2) => col1.IsTrigger ^ col2.IsTrigger;

        void PushCollidersApart(Collider2D col1, Collider2D col2)
        {
            if (col1.IsStatic && col2.IsStatic)
            {
                return; // neither moves
            }
            if (col1.IsStatic)
            {
                // move col2
                col2.RepositionFromCollision(col1.Bounds);
            }
            else if (col2.IsStatic)
            {
                // move col1
                col1.RepositionFromCollision(col2.Bounds);
            }
            else
            {
                Vector2f originalPos = col1.PositionableGameObject.Position;
                col1.RepositionFromCollision(col2.Bounds);
                col1.PositionableGameObject.Position -= originalPos / 2;
                col2.PositionableGameObject.Position = originalPos;
                // move both evenly
                //Vector2f distanceToMove = col1.PositionableGameObject.Position - col1.PositionableGameObject.Position.PosOfNearestEdge(col2.Bounds);
                //col1.PositionableGameObject.Position += distanceToMove / 2;
                //col2.PositionableGameObject.Position += distanceToMove / 2;
            }
        }

        gameObjects.Where((firstGameObject) => firstGameObject.Collider != null && firstGameObject.Collider.IsActive).ToList().ForEach((firstGameObject) =>
        {
            gameObjects.Where(otherGameObject => otherGameObject != firstGameObject && otherGameObject.Collider != null && otherGameObject.Collider.IsActive).ToList().ForEach((otherGameObject) =>
            {
                // if no collision between this object and another but the other is in the first object's colliding with list, call on collision exit and pop the collider

                if (firstGameObject.Collider!.Bounds.Intersects(otherGameObject.Collider!.Bounds))
                {
                    if (BothAreTriggers(firstGameObject.Collider, otherGameObject.Collider))
                    {
                        return;
                    }
                    if (firstGameObject.Collider.CollidingWith.Contains(otherGameObject))
                    {
                        if (OneIsTrigger(firstGameObject.Collider, otherGameObject.Collider))
                        {
                            firstGameObject.OnTriggerStay2D(otherGameObject.Collider);
                        }
                        else // already handled the both are triggers case at the top in the early return
                        {
                            firstGameObject.OnCollisionStay2D(otherGameObject.Collider);
                            otherGameObject.OnCollisionStay2D(firstGameObject.Collider);
                            PushCollidersApart(firstGameObject.Collider, otherGameObject.Collider);
                        }
                    }
                    else
                    {
                        if (OneIsTrigger(firstGameObject.Collider, otherGameObject.Collider))
                        {
                            firstGameObject.OnTriggerEnter2D(otherGameObject.Collider);
                            firstGameObject.Collider.CollidingWith.Add(otherGameObject.Collider.PositionableGameObject);
                        }
                        else // already handled the both are triggers case at the top in the early return
                        {
                            firstGameObject.OnCollisionEnter2D(otherGameObject.Collider);
                            otherGameObject.OnCollisionEnter2D(firstGameObject.Collider);
                            firstGameObject.Collider.CollidingWith.Add(otherGameObject.Collider.PositionableGameObject);
                            otherGameObject.Collider.CollidingWith.Add(firstGameObject.Collider.PositionableGameObject);
                            PushCollidersApart(firstGameObject.Collider, otherGameObject.Collider);
                        }
                    }
                }
                else
                {
                    if (firstGameObject.Collider.CollidingWith.Contains(otherGameObject))
                    {
                        // we don't care if only one is a trigger, if both became triggers then we need to pop all collisions and trigger exit
                        if (!otherGameObject.Collider.IsTrigger)
                        {
                            firstGameObject.OnCollisionExit2D(otherGameObject.Collider);
                            otherGameObject.OnCollisionExit2D(firstGameObject.Collider);
                        }
                        else
                        {
                            firstGameObject.OnTriggerExit2D(otherGameObject.Collider);
                        }
                        firstGameObject.Collider.CollidingWith.Remove(otherGameObject.Collider.PositionableGameObject);
                    }
                }
            });
        });
    }
}
