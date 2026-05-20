using Sandbox;

namespace IronDome;

// Port of lua/entities/iron_dome_missile/init.lua
// Runs predictive intercept logic each physics tick and detonates on proximity.
public sealed class IronDomeMissile : Component
{
    public IronDome Dome { get; set; }

    private GameObject _target;
    private bool _launched;
    private SoundHandle _loopHandle;

    public void SetTarget( GameObject target )
    {
        if ( target is null || !target.IsValid ) return;
        _target = target;
        _launched = true;

        Sound.Play( IronDomeConsts.MissileLaunchSoundPath, WorldPosition );
        _loopHandle = Sound.Play( IronDomeConsts.MissileLoopSoundPath, WorldPosition );

        var rb = Components.Get<Rigidbody>();
        if ( rb is null ) return;

        var targetVel = _target.Components.Get<Rigidbody>()?.Velocity ?? Vector3.Zero;
        var dir = (_target.WorldPosition + targetVel * 0.2f - WorldPosition).Normal;

        rb.Velocity = dir * IronDomeConVars.MissileSpeed;
        WorldRotation = Rotation.LookAt( dir, Vector3.Up );
    }

    protected override void OnFixedUpdate()
    {
        if ( !_launched || _target is null || !_target.IsValid )
        {
            GameObject.Destroy();
            return;
        }

        var rb = Components.Get<Rigidbody>();
        if ( rb is null ) return;

        var targetVel = _target.Components.Get<Rigidbody>()?.Velocity ?? Vector3.Zero;
        var interceptPoint = InterceptPredictor.PredictInterceptExact(
            WorldPosition,
            IronDomeConVars.MissileSpeed,
            _target.WorldPosition,
            targetVel );

        var dir = (interceptPoint - WorldPosition).Normal;
        rb.Velocity = dir * IronDomeConVars.MissileSpeed;
        WorldRotation = Rotation.LookAt( dir, Vector3.Up );

        if ( _loopHandle != null )
            _loopHandle.Position = WorldPosition;

        if ( WorldPosition.Distance( interceptPoint ) < IronDomeConsts.MissileExplosionDist )
            Detonate();
    }

    private void Detonate()
    {
        var close   = Sound.Play( IronDomeConsts.ExplosionCloseSoundPath,   WorldPosition );
        var distant = Sound.Play( IronDomeConsts.ExplosionDistantSoundPath, WorldPosition );
        if ( close   != null ) close.Pitch   = Game.Random.Float( 0.95f, 1.05f );
        if ( distant != null ) distant.Pitch = Game.Random.Float( 0.95f, 1.05f );

        if ( _target is not null && _target.IsValid )
            _target.Destroy();

        // TODO: spawn explosion VFX — assign an ExplosionPrefab [Property] and clone it here

        GameObject.Destroy();
    }

    protected override void OnDestroy()
    {
        _loopHandle?.Stop();

        // Clean up registry even if target was destroyed first — the C# reference
        // is still a valid dictionary key.
        if ( Dome is not null && _target is not null )
        {
            Dome.ActiveMissiles.Remove( _target );
            TargetRegistry.Unclaim( Dome, _target );
        }
    }
}
