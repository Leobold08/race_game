using System;
using System.Reflection;
using UnityEngine;

public class MoveInputAction: AbstractInputAction
{
    new public static string type = "MoveAction";
    public float drive;
    public float steer;

    public MoveInputAction(Vector2 movement) {
        StartTime = Time.time;
        drive = movement.y;
        steer = movement.x;
    }

    public override string Serialize()
    {
        return $"action: {{\n   type: {type},\n    startTime: {StartTime},\n    heldTime: {HeldTime},\n    drive: {drive},\n    steer: {steer}\n}}";
    }
}