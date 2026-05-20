using Sandbox;
using System;
using System.Collections.Generic;

namespace IronDome;

// Port of lua/entities/iron_dome/init.lua. Admin behavior gated by
// the iron_dome_admin_mode ConVar instead of a per-entity property.
public sealed class IronDome : Component
{
    [Property] public GameObject MissilePrefab { get; set; }
    [Property] public SoundEvent LeverSound { get; set; }

    // Replicated so proxies can drive their local siren state.
    [Sync] public bool HasActiveTarget { get; set; }

    public Dictionary<GameObject, IronDomeMissile> ActiveMissiles { get; } = new();

    private IronDomeSiren _siren;
    private int _missilesLeft;
    private bool _reloading;
    private TimeUntil _reloadComplete;
    private TimeUntil _nextScan;
    private bool _wasAdmin;

    private const int   DefaultMissilesPerReload = 20;
    private const float DefaultDetectionRadius   = 8000f;
    private const float DefaultReloadTime        = 10f;

    private static int   SafeMissilesPerReload => Math.Max( 1, IronDomeConVars.MissilesPerReload > 0 ? IronDomeConVars.MissilesPerReload : DefaultMissilesPerReload );
    private static float SafeDetectionRadius   => IronDomeConVars.DetectionRadius > 0f ? IronDomeConVars.DetectionRadius : DefaultDetectionRadius;
    private static float SafeReloadTime        => IronDomeConVars.ReloadTime      > 0f ? IronDomeConVars.ReloadTime      : DefaultReloadTime;

    public static bool IsAdmin => IronDomeConVars.AdminMode;

    private float EffectiveDetectionRadius =>
        IsAdmin ? IronDomeConsts.AdminDetectionRadius : SafeDetectionRadius;

    protected override void OnStart()
    {
        _siren = Components.GetOrCreate<IronDomeSiren>();
        _wasAdmin = IsAdmin;
        _missilesLeft = IsAdmin ? int.MaxValue : SafeMissilesPerReload;
    }

    protected override void OnUpdate()
    {
        // Siren runs on every client off the replicated HasActiveTarget flag.
        _siren?.UpdateSiren( HasActiveTarget );

        if ( IsProxy ) return;

        SyncAdminState();

        if ( !_nextScan ) return;
        _nextScan = IronDomeConsts.ScanInterval;

        if ( !HandleReload() ) return;

        var hasTarget = false;
        foreach ( var go in EnumerateScanCandidates() )
        {
            if ( go is null || !go.IsValid ) continue;
            if ( WorldPosition.Distance( go.WorldPosition ) > EffectiveDetectionRadius ) continue;
            if ( !TargetFilter.IsValidTarget( this, go ) ) continue;

            hasTarget = true;
            MissileFactory.CreateInterceptor( this, go );
            if ( !IsAdmin )
                _missilesLeft--;
            break;
        }

        HasActiveTarget = hasTarget;
    }

    private void SyncAdminState()
    {
        var isAdmin = IsAdmin;
        if ( isAdmin == _wasAdmin ) return;
        _wasAdmin = isAdmin;
        _missilesLeft = isAdmin ? int.MaxValue : SafeMissilesPerReload;
        _reloading = false;
    }

    private IEnumerable<GameObject> EnumerateScanCandidates()
    {
        foreach ( var rb in Scene.GetAllComponents<Rigidbody>() )
            if ( rb.IsValid ) yield return rb.GameObject;

        if ( IronDomeConVars.TargetPlayers )
        {
            foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
                if ( pc.IsValid ) yield return pc.GameObject;
        }
    }

    private bool HandleReload()
    {
        if ( _reloading )
        {
            if ( !_reloadComplete ) return false;
            _missilesLeft = SafeMissilesPerReload;
            _reloading = false;
            return true;
        }

        if ( !IsAdmin && _missilesLeft <= 0 )
        {
            _reloading = true;
            BroadcastLeverSound();
            _reloadComplete = SafeReloadTime;
            return false;
        }

        return true;
    }

    [Rpc.Broadcast]
    private void BroadcastLeverSound()
    {
        if ( LeverSound is not null )
            Sound.Play( LeverSound, WorldPosition );
    }
}
