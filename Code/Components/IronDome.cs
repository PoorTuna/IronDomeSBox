using Sandbox;
using System.Collections.Generic;

namespace IronDome;

// Merged port of lua/entities/iron_dome{,_protective,_admin}/init.lua
// Mode enum controls variant behavior; set via [Property] in prefab.
public sealed class IronDome : Component
{
    [Property] public IronDomeMode Mode { get; set; } = IronDomeMode.Neutral;

    // Assign the iron_dome_missile prefab in editor
    [Property] public GameObject MissilePrefab { get; set; }

    public Dictionary<GameObject, IronDomeMissile> ActiveMissiles { get; } = new();

    private IronDomeSiren _siren;
    private int _missilesLeft;
    private bool _reloading;
    private TimeUntil _reloadComplete;
    private TimeUntil _nextScan;

    private float EffectiveDetectionRadius =>
        Mode == IronDomeMode.Admin
            ? IronDomeConsts.AdminDetectionRadius
            : IronDomeConVars.DetectionRadius;

    protected override void OnStart()
    {
        _siren = Components.GetOrCreate<IronDomeSiren>();
        _missilesLeft = Mode == IronDomeMode.Admin ? int.MaxValue : IronDomeConVars.MissilesPerReload;
    }

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;
        if ( !_nextScan ) return;
        _nextScan = IronDomeConsts.ScanInterval;

        if ( !HandleReload() ) return;

        var hasTarget = false;
        foreach ( var rb in Scene.GetAllComponents<Rigidbody>() )
        {
            if ( !rb.IsValid ) continue;
            var go = rb.GameObject;
            if ( WorldPosition.Distance( go.WorldPosition ) > EffectiveDetectionRadius ) continue;
            if ( !TargetFilter.IsValidTarget( this, go ) ) continue;

            hasTarget = true;
            MissileFactory.CreateInterceptor( this, go );
            if ( Mode != IronDomeMode.Admin )
                _missilesLeft--;
            break;
        }

        _siren.UpdateSiren( hasTarget );
    }

    private bool HandleReload()
    {
        if ( _reloading )
        {
            if ( !_reloadComplete ) return false;
            _missilesLeft = IronDomeConVars.MissilesPerReload;
            _reloading = false;
            return true;
        }

        if ( Mode != IronDomeMode.Admin && _missilesLeft <= 0 )
        {
            _reloading = true;
            if ( !string.IsNullOrEmpty( IronDomeConsts.LeverSoundPath ) )
                Sound.Play( IronDomeConsts.LeverSoundPath, WorldPosition );
            _reloadComplete = IronDomeConVars.ReloadTime;
            return false;
        }

        return true;
    }
}
