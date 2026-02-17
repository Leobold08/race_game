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

        var root = carRoot ? carRoot : transform;
        _currentCar = Instantiate(carPrefabs[clamped], root.position, root.rotation, root);
    }
}
