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

    // Tuned so the quadratic predictor always has a positive solution against
    // physgun-launched props (~1000-3000 u/s) and typical GMod-style rockets.
    [ConVar( "iron_dome_missile_speed" )]
    public static float MissileSpeed { get; set; } = 6000f;

    [ConVar( "iron_dome_detection_radius" )]
    public static float DetectionRadius { get; set; } = 8000f;

    // Fallback when no real missile addons present: engage any GameObject with a Rigidbody.
    // Untagged props still need to be above the dome and visible (LOS).
    [ConVar( "iron_dome_target_all_rigidbodies" )]
    public static bool TargetAllRigidbodies { get; set; } = true;

    // Off by default — when enabled, dome will also engage PlayerController-driven pawns.
    [ConVar( "iron_dome_target_players" )]
    public static bool TargetPlayers { get; set; } = false;
}
