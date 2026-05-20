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
}
