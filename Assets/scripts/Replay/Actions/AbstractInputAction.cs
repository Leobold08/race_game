using System;
using UnityEngine;

public abstract class AbstractInputAction: AbstractAction
{
    public float StartTime { get; set; }
    public float HeldTime { get; set; }
    public void StopInputting()
    {
        HeldTime = Time.time - StartTime;
    }
}