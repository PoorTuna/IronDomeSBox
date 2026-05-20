using Sandbox;

namespace IronDome;

// Server-authoritative ConVars. s&box saves+replicates these automatically.
// Equivalent to GMod's FCVAR_ARCHIVE | FCVAR_REPLICATED.
public static class IronDomeConVars
{
    private const ConVarFlags Flags = ConVarFlags.Replicated | ConVarFlags.Saved;

    [ConVar( "iron_dome_max_health", Flags, Help = "Health points the dome starts with before exploding." )]
    public static float MaxHealth { get; set; } = 1000f;

    [ConVar( "iron_dome_siren_volume", Flags, Help = "Volume multiplier for the dome alarm siren (0-1)." )]
    public static float SirenVolume { get; set; } = 1f;

    [ConVar( "iron_dome_reload_time", Flags, Help = "Seconds between magazines once a dome runs dry." )]
    public static float ReloadTime { get; set; } = 10f;

    [ConVar( "iron_dome_missiles_per_reload", Flags, Help = "Missiles fired per magazine before a reload kicks in." )]
    public static int MissilesPerReload { get; set; } = 20;

    // Tuned so the quadratic predictor always has a positive solution against
    // physgun-launched props (~1000-3000 u/s) and typical GMod-style rockets.
    [ConVar( "iron_dome_missile_speed", Flags, Help = "Interceptor speed in units/second. Must exceed target speed." )]
    public static float MissileSpeed { get; set; } = 6000f;

    [ConVar( "iron_dome_detection_radius", Flags, Help = "Engagement radius in units for non-admin domes." )]
    public static float DetectionRadius { get; set; } = 8000f;

    // Fallback when no real missile addons present: engage any GameObject with a Rigidbody.
    // Untagged props still need to be above the dome and visible (LOS).
    [ConVar( "iron_dome_target_all_rigidbodies", Flags, Help = "Engage any rigidbody prop overhead, not just tagged projectiles." )]
    public static bool TargetAllRigidbodies { get; set; } = true;

    // Off by default — when enabled, dome will also engage PlayerController-driven pawns.
    [ConVar( "iron_dome_target_players", Flags, Help = "Also engage players. Players take a hit but are not deleted." )]
    public static bool TargetPlayers { get; set; } = false;

    // Admin mode: infinite missiles, no reload, extended detection radius.
    [ConVar( "iron_dome_admin_mode", Flags, Help = "Admin mode: infinite missiles, no reload, extended detection radius." )]
    public static bool AdminMode { get; set; } = false;
}
