using Sandbox;

namespace IronDome;

// Port of lua/iron_dome/core/siren.lua
// Hold-time: siren keeps playing for SirenHoldTime seconds after last detected target.
public sealed class IronDomeSiren : Component
{
    [Property] public SoundEvent SirenSound { get; set; }

    private SoundHandle _handle;
    private bool _playing;
    private float _stopAt;

    public void UpdateSiren( bool hasTarget )
    {
        if ( hasTarget )
        {
            StartSiren();
        }
        else if ( _playing && Time.Now >= _stopAt )
        {
            StopSiren();
        }
    }

    private void StartSiren()
    {
        if ( !_playing )
        {
            if ( SirenSound is not null )
            {
                _handle = Sound.Play( SirenSound, WorldPosition );
                if ( _handle != null )
                    _handle.Volume = IronDomeConVars.SirenVolume;
            }
            _playing = true;
        }
        _stopAt = Time.Now + IronDomeConsts.SirenHoldTime;
    }

    private void StopSiren()
    {
        _handle?.Stop();
        _playing = false;
    }

    protected override void OnDestroy()
    {
        if ( _playing )
            _handle?.Stop();
    }
}
