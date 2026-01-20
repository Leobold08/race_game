
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class NetworkCam : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

        if (!isOwner && playerCamera != null)
        {
            playerCamera.enabled = false;
            var audioListener = playerCamera.GetComponent<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

}