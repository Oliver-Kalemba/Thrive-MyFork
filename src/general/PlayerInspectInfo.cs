﻿using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   A system that manages detecting what the player is pointing with the cursor.
/// </summary>
public class PlayerInspectInfo : Node
{
    /// <summary>
    ///   The distance for detection.
    /// </summary>
    [Export]
    public float RaycastDistance = 1000;

    private readonly PhysicsRayWithUserData[] hits = new PhysicsRayWithUserData[Constants.MAX_RAY_HITS_FOR_INSPECT];
    private readonly HashSet<Entity> previousHits = new();

    private int validHits;

    /// <summary>
    ///   Needs to be set to the physical world to use
    /// </summary>
    public PhysicalWorld? PhysicalWorld { get; set; }

    /// <summary>
    ///   All (physics) entities the player is pointing at.
    /// </summary>
    public IEnumerable<Entity> Entities =>
        hits.Take(validHits).Where(h => h.BodyEntity != default).Select(h => h.BodyEntity);

    public virtual void Process(float delta)
    {
        var viewport = GetViewport();
        var mousePos = viewport.GetMousePosition();
        var camera = viewport.GetCamera();

        // Safety check to disable this node when there's no active camera
        if (camera == null)
            return;

        if (PhysicalWorld == null)
        {
            GD.PrintErr($"{nameof(PlayerInspectInfo)} doesn't have physics world set");
            return;
        }

        var from = camera.ProjectRayOrigin(mousePos);
        var offsetToEnd = camera.ProjectRayNormal(mousePos) * RaycastDistance;

        validHits = PhysicalWorld.CastRayGetAllHits(from, offsetToEnd, hits);

        previousHits.RemoveWhere(m =>
        {
            if (hits.Take(validHits).All(h => h.BodyEntity != m))
            {
                // Hit removed
                if (m.IsAlive && m.Has<Selectable>())
                {
                    ref var selectable = ref m.Get<Selectable>();
                    selectable.Selected = false;
                }

                return true;
            }

            return false;
        });

        foreach (var hit in hits.Take(validHits))
        {
            if (!previousHits.Add(hit.BodyEntity))
                continue;

            // New hit added

            if (hit.BodyEntity.IsAlive && hit.BodyEntity.Has<Selectable>())
            {
                ref var selectable = ref hit.BodyEntity.Get<Selectable>();
                selectable.Selected = true;
            }
        }
    }

    /// <summary>
    ///   Returns the raycast data of the given raycast hit entity.
    /// </summary>
    /// <param name="entity">Entity to get the data for</param>
    /// <param name="rayData">Where to put the found ray data, initialized to default if not found</param>
    /// <returns>True when the data was found</returns>
    public bool GetRaycastData(Entity entity, out PhysicsRayWithUserData rayData)
    {
        for (int i = 0; i < validHits; ++i)
        {
            if (hits[i].BodyEntity == entity)
            {
                rayData = hits[i];
                return true;
            }
        }

        rayData = default;
        return false;
    }
}
