using System;
using Sandbox;

namespace IronDome;

// Direct port of lua/iron_dome/missile/intercept_predict.lua
// Solves the quadratic for minimum positive intercept time, returns 3D aim point.
public static class InterceptPredictor
{
    public static Vector3 PredictInterceptExact(
        Vector3 shooterPos, float shooterSpeed,
        Vector3 targetPos, Vector3 targetVel )
    {
        var dir = targetPos - shooterPos;
        float a = Vector3.Dot( targetVel, targetVel ) - shooterSpeed * shooterSpeed;
        float b = 2f * Vector3.Dot( dir, targetVel );
        float c = Vector3.Dot( dir, dir );

        float discriminant = b * b - 4f * a * c;
        if ( discriminant < 0f )
            return targetPos;

        float sqrtDisc = MathF.Sqrt( discriminant );
        float t1 = (-b + sqrtDisc) / (2f * a);
        float t2 = (-b - sqrtDisc) / (2f * a);

        float t = MathF.Min( t1, t2 );
        if ( t < 0f ) t = MathF.Max( t1, t2 );
        if ( t < 0f ) return targetPos;

        return targetPos + targetVel * t;
    }
}
