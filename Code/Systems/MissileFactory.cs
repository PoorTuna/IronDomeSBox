using Sandbox;

namespace IronDome;

// Port of lua/iron_dome/missile/missile_factory.lua
public static class MissileFactory
{
    public static IronDomeMissile CreateInterceptor( IronDome dome, GameObject target )
    {
        if ( target is null || !target.IsValid ) { Log.Info( "MissileFactory: target null/invalid" ); return null; }
        if ( dome.MissilePrefab is null ) { Log.Info( "MissileFactory: MissilePrefab is null" ); return null; }

        var spawnPos = dome.WorldPosition + Vector3.Up * IronDomeConsts.MissileSpawnHeight;
        Log.Info( $"MissileFactory: cloning missile at {spawnPos}" );
        var missileGo = dome.MissilePrefab.Clone( new Transform( spawnPos ) );
        if ( missileGo is null ) { Log.Info( "MissileFactory: Clone returned null" ); return null; }

        var missile = missileGo.Components.Get<IronDomeMissile>();
        if ( missile is null )
        {
            Log.Info( "MissileFactory: IronDomeMissile component not found on clone" );
            missileGo.Destroy();
            return null;
        }

        missile.Dome = dome;
        missile.SetTarget( target );

        TargetRegistry.Claim( dome, target );
        dome.ActiveMissiles[target] = missile;

        return missile;
    }
}
