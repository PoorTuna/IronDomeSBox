namespace IronDome;

public static class IronDomeConsts
{
    // Dome tick
    public const float ScanInterval = 0.25f;
    public const float SirenHoldTime = 10f;

    // Missile spawn
    public const float MissileSpawnHeight = 100f;

    // Interceptor detonation
    public const float MissileExplosionMag = 120f;
    public const float MissileExplosionDist = 100f;

    // Dome self-destruct explosion
    public const float ExplosionMagnitude = 500f;
    public const float ExplosionRadius = 300f;

    // Admin override detection radius (matches GMod hardcoded 15000)
    public const float AdminDetectionRadius = 15000f;

    // Sound paths (relative to addon root, no leading slash)
    public const string SirenSoundPath            = "sounds/iron_dome/iron_dome_alarm.sound";
    public const string LeverSoundPath            = "sounds/iron_dome/iron_dome_lever.sound";
    public const string MissileLaunchSoundPath    = "sounds/iron_dome/iron_dome_missile_launch.sound";
    public const string MissileLoopSoundPath      = "sounds/iron_dome/iron_dome_missile_loop.sound";
    public const string ExplosionCloseSoundPath   = "sounds/iron_dome/iron_dome_explosion_close.sound";
    public const string ExplosionDistantSoundPath = "sounds/iron_dome/iron_dome_explosion_distant.sound";

    // Particle paths — wire these up to real .vpcf in editor
    public const string SmallExplosionVfx = "particles/explosion_small.vpcf";
    public const string LargeExplosionVfx = "particles/explosion_large.vpcf";
}
