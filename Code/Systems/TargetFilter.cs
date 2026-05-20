using Sandbox;

namespace IronDome;

// Port of lua/iron_dome/target/target_filter.lua
// Tag-based approach: projectiles must carry tag "interceptable" to be engaged.
// Protective mode skips targets owned by the same connection as the dome.
public static class TargetFilter
{
    public static bool IsValidTarget( IronDome dome, GameObject target )
    {
        if ( target is null || !target.IsValid ) return false;
        if ( target == dome.GameObject ) return false;

        // Must be an interceptable projectile
        if ( !target.Tags.Has( "interceptable" ) ) return false;

        // Never engage our own interceptors
        if ( target.Components.TryGet<IronDomeMissile>( out _ ) ) return false;

        // Protective mode: ignore owner's own projectiles.
        // Requires both target and dome to have a valid network owner to compare.
        if ( dome.Mode == IronDomeMode.Protective )
        {
            var domeOwner   = dome.GameObject.Network?.OwnerConnection;
            var targetOwner = target.Network?.OwnerConnection;
            if ( domeOwner is not null && targetOwner is not null && domeOwner == targetOwner )
                return false;
        }

        // Target must be above the dome (same as GMod height check)
        if ( target.WorldPosition.z <= dome.WorldPosition.z ) return false;

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
