using UnityEngine;
using PurrNet;

public class CameraNetwork : NetworkBehaviour
{

    public Camera PCam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PCam != null)
        {
            PCam.enabled = isOwner;
        }
        
        // Optional: Also disable the AudioListener if present
        AudioListener listener = GetComponentInChildren<AudioListener>();
        if (listener != null)
        {
            listener.enabled = isOwner;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
