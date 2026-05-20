using Sandbox;

namespace IronDome;

// Destroys its GameObject after Delay seconds. Used to clean up orphaned
// VFX (e.g. a missile trail that was detached from its parent on detonation
// so existing particles can finish their lifetime).
public sealed class DelayedDestroy : Component
{
    [Property] public float Delay { get; set; } = 5f;

    private TimeUntil _due;

    protected override void OnStart()
    {
        _due = Delay;
    }

    protected override void OnUpdate()
    {
        if ( _due ) GameObject.Destroy();
    }
}
