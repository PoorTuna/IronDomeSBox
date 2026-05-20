using Sandbox;
using System.Collections.Generic;

namespace IronDome;

// Global claim table — one dome per target, same role as IronDome_GlobalTargets in GMod.
public static class TargetRegistry
{
    private static readonly Dictionary<GameObject, IronDome> _claimed = new();

    public static void Clear() => _claimed.Clear();

    public static void Claim( IronDome dome, GameObject target )
    {
        _claimed[target] = dome;
    }

    public static void Unclaim( IronDome dome, GameObject target )
    {
        if ( _claimed.TryGetValue( target, out var owner ) && owner == dome )
            _claimed.Remove( target );
    }

    public static bool IsClaimed( GameObject target )
    {
        if ( !_claimed.TryGetValue( target, out var dome ) ) return false;
        if ( dome is null || !dome.IsValid )
        {
            _claimed.Remove( target );
            return false;
        }
        // Self-heal: claim is stale if the dome no longer has a live missile
        // tracking this target (e.g. missile destroyed without firing OnDestroy).
        if ( !dome.ActiveMissiles.TryGetValue( target, out var missile )
             || missile is null || !missile.IsValid )
        {
            _claimed.Remove( target );
            dome.ActiveMissiles.Remove( target );
            return false;
        }
        return true;
    }
}
