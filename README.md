# Iron Dome — s&box Port

s&box port of the [IronDomeGmod](https://github.com/PoorTuna/IronDomeGmod) addon. Brings the IDF Iron Dome air defense system to s&box, intercepting incoming projectiles with predictive guidance.

## Features

- Predictive intercept using a quadratic solver for accurate target leading
- Three variants: Neutral, Protective, Admin
- Coordinated target claiming across multiple domes so two units never engage the same target
- 20-missile magazine with a 10-second reload cycle
- Authentic IDF Red Alert siren with hold-off timer
- Layered close/distant explosion audio
- Blast-damage-only health pool with chain explosions
- Runtime-tunable via `ConVar`s

## Usage

1. Open the project in s&box.
2. Drop `iron_dome_neutral.prefab`, `iron_dome_protective.prefab`, or `iron_dome_admin.prefab` into a scene.
3. Tag any threat GameObject with `interceptable`. The dome scans for `Rigidbody` components carrying the tag above its own Z position.

### Variants

| Prefab | Behavior |
|---|---|
| `iron_dome_neutral.prefab` | Engages every `interceptable` target |
| `iron_dome_protective.prefab` | Skips projectiles owned by the dome's network owner |
| `iron_dome_admin.prefab` | Infinite missiles, no reload, 15000-unit radius |

### ConVars

| Name | Default | Purpose |
|---|---|---|
| `iron_dome_max_health` | 1000 | Dome health (blast damage only) |
| `iron_dome_siren_volume` | 1.0 | Siren volume |
| `iron_dome_reload_time` | 10 | Seconds per reload cycle |
| `iron_dome_missiles_per_reload` | 20 | Missiles per reload |
| `iron_dome_missile_speed` | 3500 | Interceptor speed (units/s) |
| `iron_dome_detection_radius` | 8000 | Scan radius (Admin variant overrides to 15000) |

## Project Layout

```
Code/
  Components/   IronDome, IronDomeMissile, IronDomeSiren, IronDomeHealth
  Systems/      TargetRegistry, TargetFilter, MissileFactory, InterceptPredictor
  Config/       IronDomeConsts, IronDomeConVars
Assets/
  models/       iron_dome mesh + ModelDoc
  materials/    PBR material + textures
  sounds/       Siren, launch, loop, explosions, reload
  prefabs/      Dome variants + missile
tools/          One-off Source asset extraction scripts (Python)
```

## Credits

- Iron Dome model — **Chenzoss**
- Red Alert siren — **ShalevDZN**
- Original GMod addon and s&box port — **PoorTuna**
