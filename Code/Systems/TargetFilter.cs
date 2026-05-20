using Sandbox;

namespace IronDome;

// Port of lua/iron_dome/target/target_filter.lua
// Tag-based: projectiles tagged "interceptable" are always engaged.
// Fallback: TargetAllRigidbodies enables engagement of any rigidbody prop.
// Players engaged only when TargetPlayers ConVar is on.
public static class TargetFilter
{
    public static bool IsValidTarget( IronDome dome, GameObject target )
    {
        if ( target is null || !target.IsValid ) return false;
        if ( target == dome.GameObject ) return false;

        // Never engage our own interceptors
        if ( target.Components.TryGet<IronDomeMissile>( out _ ) ) return false;

        // Never engage other dome batteries (they now have a Rigidbody so they
        // show up in the scan set when TargetAllRigidbodies is on).
        if ( target.Components.TryGet<IronDome>( out _ ) ) return false;

        var isPlayer = target.Components.TryGet<PlayerController>( out _ );
        if ( isPlayer )
        {
            if ( !IronDomeConVars.TargetPlayers ) return false;
        }
        else
        {
            var hasInterceptableTag = target.Tags.Has( "interceptable" );
            var allowAnyRigidbody = IronDomeConVars.TargetAllRigidbodies
                && target.Components.TryGet<Rigidbody>( out _ );
            if ( !hasInterceptableTag && !allowAnyRigidbody ) return false;
        }

        // Target must be above the dome (skipped for players — they walk on the ground)
        if ( !isPlayer && target.WorldPosition.z <= dome.WorldPosition.z ) return false;

        // Line-of-sight: no solid world geometry in between
        var traceStart = dome.WorldPosition + Vector3.Up * 50f;
        var traceEnd   = target.WorldPosition + Vector3.Up * 20f;
        var tr = dome.Scene.Trace.Ray( traceStart, traceEnd )
            .IgnoreGameObjectHierarchy( dome.GameObject )
            .IgnoreGameObjectHierarchy( target )
            .Run();
        if ( tr.Hit ) return false;

        // Reject already-claimed targets
        if ( TargetRegistry.IsClaimed( target ) ) return false;

        return true;
    }

}
