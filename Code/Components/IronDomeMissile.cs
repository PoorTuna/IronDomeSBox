using Sandbox;
using System.Linq;

namespace IronDome;

// Port of lua/entities/iron_dome_missile/init.lua
// Runs predictive intercept logic each physics tick and detonates on proximity.
public sealed class IronDomeMissile : Component
{
    public IronDome Dome { get; set; }

    [Property] public SoundEvent LaunchSound { get; set; }
    [Property] public SoundEvent LoopSound { get; set; }
    [Property] public SoundEvent ExplosionCloseSound { get; set; }
    [Property] public SoundEvent ExplosionDistantSound { get; set; }

    private GameObject _target;
    private bool _launched;
    private SoundHandle _loopHandle;

    private const float DefaultMissileSpeed = 6000f;
    private static float SafeMissileSpeed => IronDomeConVars.MissileSpeed > 0f ? IronDomeConVars.MissileSpeed : DefaultMissileSpeed;

    public void SetTarget( GameObject target )
    {
        if ( target is null || !target.IsValid ) return;
        _target = target;
        _launched = true;

        if ( LaunchSound is not null ) Sound.Play( LaunchSound, WorldPosition );
        if ( LoopSound is not null ) _loopHandle = Sound.Play( LoopSound, WorldPosition );

        var rb = Components.Get<Rigidbody>();
        if ( rb is null ) return;

        var targetVel = _target.Components.Get<Rigidbody>()?.Velocity ?? Vector3.Zero;
        var dir = (_target.WorldPosition + targetVel * 0.2f - WorldPosition).Normal;

        rb.Velocity = dir * SafeMissileSpeed;
        WorldRotation = Rotation.LookAt( dir, Vector3.Up );
    }

    protected override void OnFixedUpdate()
    {
        if ( !_launched || _target is null || !_target.IsValid )
        {
            DestroyMissile();
            return;
        }

        var rb = Components.Get<Rigidbody>();
        if ( rb is null ) return;

        var targetVel = _target.Components.Get<Rigidbody>()?.Velocity ?? Vector3.Zero;
        var interceptPoint = InterceptPredictor.PredictInterceptExact(
            WorldPosition,
            SafeMissileSpeed,
            _target.WorldPosition,
            targetVel );

        var dir = (interceptPoint - WorldPosition).Normal;
        rb.Velocity = dir * SafeMissileSpeed;
        WorldRotation = Rotation.LookAt( dir, Vector3.Up );

        if ( _loopHandle != null )
            _loopHandle.Position = WorldPosition;

        if ( WorldPosition.Distance( interceptPoint ) < IronDomeConsts.MissileExplosionDist )
            Detonate();
    }

    private void Detonate()
    {
        if ( ExplosionCloseSound is not null )
        {
            var close = Sound.Play( ExplosionCloseSound, WorldPosition );
            if ( close != null ) close.Pitch = Game.Random.Float( 0.95f, 1.05f );
        }
        if ( ExplosionDistantSound is not null )
        {
            var distant = Sound.Play( ExplosionDistantSound, WorldPosition );
            if ( distant != null ) distant.Pitch = Game.Random.Float( 0.95f, 1.05f );
        }

        // Players: route through IDamageable so respawn flow stays intact.
        // Props: hard delete the GameObject so they don't dribble out gibs that
        // re-enter the scan set and burn missiles forever.
        if ( _target is not null && _target.IsValid )
        {
            if ( _target.Components.TryGet<PlayerController>( out _ )
                 && _target.Components.TryGet<Component.IDamageable>( out var damageable, FindMode.EverythingInSelfAndChildren ) )
            {
                var dmg = new DamageInfo( 9999, Dome?.GameObject, null );
                damageable.OnDamage( dmg );
            }
            else
            {
                _target.Destroy();
            }
        }

        // TODO: spawn explosion VFX — assign an ExplosionPrefab [Property] and clone it here

        DestroyMissile();
    }

    // Single shutdown path: detach particle trails so they fade naturally,
    // then destroy the missile GameObject. Doing the detach here (while the
    // missile is still alive) keeps the orphan in the active scene; running
    // it from OnDestroy parks it in the System scene and crashes OnAwake on
    // any newly-added component.
    private void DestroyMissile()
    {
        DetachTrailChildren();
        GameObject.Destroy();
    }

    // Reparents particle-emitting children to the scene root and stops their
    // emitters so already-spawned particles fade out naturally instead of
    // vanishing the instant the missile is destroyed. A DelayedDestroy
    // component cleans up each orphaned trail after the particle lifetime.
    private void DetachTrailChildren()
    {
        foreach ( var child in GameObject.Children.ToList() )
        {
            child.SetParent( null, true );

            foreach ( var c in child.Components.GetAll<Component>() )
            {
                if ( c.GetType().Name.EndsWith( "Emitter" ) )
                    c.Enabled = false;
            }

            var killer = child.Components.GetOrCreate<DelayedDestroy>();
            killer.Delay = 10f;
        }
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
