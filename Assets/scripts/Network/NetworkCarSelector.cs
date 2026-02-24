using UnityEngine;
using PurrNet;
using System.Text;

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
        if (!asServer && isOwner)
        {
            int selectedIndex = ResolveSelectedCarIndex();
            RequestSpawnCar(selectedIndex);
            return;
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
            string normalizedSelected = NormalizeCarName(selectedCarName);
            for (int i = 0; i < carPrefabs.Length; i++)
            {
                if (carPrefabs[i] == null)
                    continue;

                string normalizedPrefab = NormalizeCarName(carPrefabs[i].name);
                if (string.Equals(normalizedPrefab, normalizedSelected, System.StringComparison.OrdinalIgnoreCase)
                    || normalizedPrefab.Contains(normalizedSelected)
                    || normalizedSelected.Contains(normalizedPrefab))
                    return i;
            }
        }

        if (PlayerPrefs.HasKey("CarIndex"))
            selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt("CarIndex", selectedIndex), 0, carPrefabs.Length - 1);

        return selectedIndex;
    }

    private string NormalizeCarName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string sanitized = value.Replace("(Clone)", string.Empty, System.StringComparison.OrdinalIgnoreCase);
        StringBuilder builder = new StringBuilder(sanitized.Length);

        for (int i = 0; i < sanitized.Length; i++)
        {
            char character = sanitized[i];
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private void HandleCarIndexChanged(int index)
    {
        // Kept only to satisfy SyncVar subscription; spawning is driven explicitly from owner via RPC.
    }

    [ServerRpc]
    private void RequestSpawnCar(int carIndex)
    {
        if (carPrefabs == null || carPrefabs.Length == 0)
            return;

        int clampedIndex = Mathf.Clamp(carIndex, 0, carPrefabs.Length - 1);

        if (_currentCar)
            Destroy(_currentCar);

        this.carIndex.value = clampedIndex;

        var root = carRoot ? carRoot : transform;
        _currentCar = Instantiate(carPrefabs[clampedIndex], root.position, root.rotation, root);
        
        // Get the NetworkIdentity and give ownership to the requesting player
        var carNetwork = _currentCar.GetComponent<NetworkIdentity>();
        if (carNetwork != null && owner.HasValue)
        {
            carNetwork.GiveOwnership(owner.Value);
        }
    }
}
