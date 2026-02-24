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
            int selectedIndex = ResolveSelectedCarIndex();

            carIndex.value = selectedIndex;
        }
    }

    private int ResolveSelectedCarIndex()
    {
        if (carPrefabs == null || carPrefabs.Length == 0)
            return 0;

        int selectedIndex = Mathf.Clamp(GameManager.SelectedCarIndex, 0, carPrefabs.Length - 1);

        string selectedCarName = PlayerPrefs.GetString("SelectedCar", string.Empty);
        if (!string.IsNullOrWhiteSpace(selectedCarName))
        {
            for (int i = 0; i < carPrefabs.Length; i++)
            {
                if (carPrefabs[i] != null && string.Equals(carPrefabs[i].name, selectedCarName, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        if (PlayerPrefs.HasKey("CarIndex"))
            selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt("CarIndex", selectedIndex), 0, carPrefabs.Length - 1);

        return selectedIndex;
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
