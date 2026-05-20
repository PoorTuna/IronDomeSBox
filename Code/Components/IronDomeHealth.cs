using Sandbox;

namespace IronDome;

// Port of lua/iron_dome/core/health.lua
// Call TakeBlastDamage() to damage the dome — only blast hits count.
// s&box has no built-in IDamageable; wire external explosions to call this directly.
public sealed class IronDomeHealth : Component
{
    [Property] public float Health { get; private set; }
    private bool _exploded;

    protected override void OnStart()
    {
        Health = IronDomeConVars.MaxHealth;
    }

    public void TakeBlastDamage( float amount )
    {
        if ( _exploded ) return;
        Health -= amount;
        if ( Health <= 0f )
            Explode();
    }

    private void Explode()
    {
        if ( _exploded ) return;
        _exploded = true;

        var pos = WorldPosition;

        // Blast-damage any other domes in radius
        foreach ( var other in Scene.GetAllComponents<IronDomeHealth>() )
        {
            if ( other.GameObject == GameObject ) continue;
            float dist = other.WorldPosition.Distance( pos );
            if ( dist > IronDomeConsts.ExplosionRadius ) continue;
            float falloff = 1f - dist / IronDomeConsts.ExplosionRadius;
            other.TakeBlastDamage( IronDomeConsts.ExplosionMagnitude * falloff );
        }

        // TODO: spawn explosion VFX here — e.g. assign an ExplosionPrefab [Property]
        //       and call ExplosionPrefab.Clone(new Transform(pos));

        GameObject.Destroy();
    }
}
