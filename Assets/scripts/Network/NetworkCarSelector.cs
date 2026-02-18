using UnityEngine;
using PurrNet;

public class NetworkCarSelector : NetworkBehaviour
{
    [SerializeField] private Transform carRoot;
    [SerializeField] private GameObject[] carPrefabs;
    [SerializeField] private SyncVar<int> carIndex = new SyncVar<int>(0, 0f, true);

    private GameObject _currentCar;

    protected override void OnInitializeModules()
    {
        carIndex.onChanged += HandleCarIndexChanged;
    }

    protected override void OnDespawned()
    {
        carIndex.onChanged -= HandleCarIndexChanged;
        if (_currentCar)
            Destroy(_currentCar);
    }

    protected override void OnSpawned(bool asServer)
    {
        HandleCarIndexChanged(carIndex.value);

        if (!asServer && isOwner)
        {
            int selectedIndex = GameManager.SelectedCarIndex;
            if (carPrefabs != null && carPrefabs.Length > 0)
                selectedIndex = Mathf.Clamp(selectedIndex, 0, carPrefabs.Length - 1);
            else selectedIndex = 0;

            carIndex.value = selectedIndex;
        }
    }

    private void HandleCarIndexChanged(int index)
    {
        if (carPrefabs == null || carPrefabs.Length == 0)
            return;

        int clamped = Mathf.Clamp(index, 0, carPrefabs.Length - 1);

        if (_currentCar)
            Destroy(_currentCar);

        // Only the owner requests the spawn (which sends it to server)
        if (isOwner)
        {
            RequestSpawnCar(clamped);
        }
    }

    [ServerRpc]
    private void RequestSpawnCar(int carIndex)
    {
        var root = carRoot ? carRoot : transform;
        _currentCar = Instantiate(carPrefabs[carIndex], root.position, root.rotation, root);
        
        // Get the NetworkIdentity and give ownership to the requesting player
        var carNetwork = _currentCar.GetComponent<NetworkIdentity>();
        if (carNetwork != null && owner.HasValue)
        {
            carNetwork.GiveOwnership(owner.Value);
        }
    }
}
