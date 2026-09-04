using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// This is a script containing 2 different classes, Wheel and Wheels, 
// where Wheels is basically an array containing 4 instances of Wheel.
// Wheels also contains utility functions.

[Serializable]
public class Wheel
{
    public enum Axel
    {
        Front,
        Rear
    }

    public GameObject model;
    public WheelCollider collider;

    public GameObject effectobj;
    public ParticleSystem smokeParticle;
    public Axel axel;
    public TrailRenderer trailRenderer;

    public bool IsGrounded()
    {
        return collider.GetGroundHit(out _);
    }

    public void Brake(float BrakeAcceleration)
    {
        collider.brakeTorque = BrakeAcceleration;
    }

    public void SetTorque(float TargetTorque)
    {
        collider.motorTorque = TargetTorque;
    }
}


[Serializable]
public class Wheels
{
    public Wheels(WheelCollider[] colliders, Transform meshes, Transform effects)
    {
        wheels = new Wheel[colliders.Count()];
        for (int i = 0; i < wheels.Count(); i++)
        {
            Wheel wheel = new()
            {
                collider = colliders[i]
            };

            Transform Mesh = meshes.Find(wheel.collider.name);

            wheel.model = Mesh != null ? Mesh.gameObject : null;

            Transform Effect = effects.transform.Find(wheel.collider.name);

            wheel.effectobj = Effect != null ? Effect.gameObject : null;
            TrailRenderer trailRenderer = wheel.effectobj != null ? wheel.effectobj.GetComponentInChildren<TrailRenderer>(true) : null;
            if (trailRenderer != null && (trailRenderer.sharedMaterial == null || trailRenderer.sharedMaterial.shader == null || !trailRenderer.sharedMaterial.shader.isSupported))
            {
                trailRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            trailRenderer.enabled = true;
            wheel.trailRenderer = trailRenderer;
            wheel.smokeParticle = wheel.effectobj != null
                ? wheel.effectobj.GetComponentInChildren<ParticleSystem>(true)
                : wheel.collider.transform.GetComponentInChildren<ParticleSystem>(true);

            wheel.axel = wheel.collider.name.IndexOf("front", StringComparison.OrdinalIgnoreCase) >= 0 ? Wheel.Axel.Front : Wheel.Axel.Rear;
            
            wheels[i] = wheel;
        }

        FrontWheels = wheels.Where(w => w.axel == Wheel.Axel.Front).ToArray();
        RearWheels = wheels.Where(w => w.axel == Wheel.Axel.Rear).ToArray();
        grassLayer = LayerMask.NameToLayer("grass");
    }

    public Wheel this[int index]
    {
        get
        {
            return wheels[index];
        }

        set
        {
            wheels[index] = value;
        }
    }
    private LayerMask grassLayer;
    [SerializeField] protected Wheel[] wheels = new Wheel[4];
    public Wheel[] FrontWheels { get; private set; } = new Wheel[2];
    public Wheel[] RearWheels { get; private set; } = new Wheel[2];
    public float MotorTorque {
        get
        {
            return wheels[0].collider.motorTorque;
        }
        set
        {
            foreach (Wheel wheel in wheels)
            {
                wheel.SetTorque(value);
            }
        }
    }

    public float BrakeTorque
    {
        get
        {
            return wheels[0].collider.brakeTorque;
        }
        set
        {
            foreach (Wheel wheel in wheels)
            {
                wheel.Brake(value);
            }
        }
    }

    public float SteerAngle
    {
        get
        {
            return FrontWheels[0].collider.steerAngle;
        }
        set
        {
            foreach (Wheel wheel in FrontWheels)
            {
                wheel.collider.steerAngle = value;
            }
        }
    }

    public IEnumerator GetEnumerator()
    {
        return wheels.GetEnumerator();
    }

    public bool TouchingGrass()
    {
        foreach (Wheel w in wheels)
        {
            if (
                w.collider.GetGroundHit(out WheelHit hit) 
                && hit.collider.gameObject.layer == grassLayer
            ) return true;
        }
        return false;
    }

    public bool IsTouchingGround()
    {
        foreach (Wheel w in wheels)
        {
            if (w.collider.isGrounded) return true;
        }
        return false;
    }
}