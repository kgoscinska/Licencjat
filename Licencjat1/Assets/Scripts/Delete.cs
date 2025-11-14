using UnityEngine;

[RequireComponent(typeof(BuildingSystem))]
public class BuildingRemover : MonoBehaviour
{
    [SerializeField] private KeyCode removeAllKey = KeyCode.Q;
    [SerializeField] private BuildingGrid grid;

    private BuildingSystem buildingSystem;

    private void Awake()
    {
        buildingSystem = GetComponent<BuildingSystem>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(removeAllKey))
        {
            RemoveAllBuildings();
        }
    }

    [ContextMenu("Remove All Buildings")]
    public void RemoveAllBuildings()
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var cell = grid.GetCell(x, y);
                if (cell != null && !cell.IsEmpty())
                {
                    var building = cell.GetBuilding();
                    if (building != null)
                    {
                        Destroy(building.gameObject);
                    }
                    cell.Clear();
                }
            }
        }

        if (buildingSystem.HasActivePreview())
        {
            buildingSystem.CancelPreview();
        }

        Debug.Log("Usuniete");
    }
}