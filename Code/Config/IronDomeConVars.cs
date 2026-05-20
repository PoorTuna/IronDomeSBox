using Sandbox;

namespace IronDome;

// Server-authoritative ConVars. s&box saves+replicates these automatically.
// Equivalent to GMod's FCVAR_ARCHIVE | FCVAR_REPLICATED.
public static class IronDomeConVars
{
    [ConVar( "iron_dome_max_health" )]
    public static float MaxHealth { get; set; } = 1000f;

    [ConVar( "iron_dome_siren_volume" )]
    public static float SirenVolume { get; set; } = 1f;

    [ConVar( "iron_dome_reload_time" )]
    public static float ReloadTime { get; set; } = 10f;

    [ConVar( "iron_dome_missiles_per_reload" )]
    public static int MissilesPerReload { get; set; } = 20;

    [ConVar( "iron_dome_missile_speed" )]
    public static float MissileSpeed { get; set; } = 3500f;

    [ConVar( "iron_dome_detection_radius" )]
    public static float DetectionRadius { get; set; } = 8000f;
}
