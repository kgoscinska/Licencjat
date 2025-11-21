// BuildingSystem.cs (ca?y skrypt z wszystkimi poprzednimi modyfikacjami)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    public const float CellSize = 1f;

    [SerializeField] private BuildingData buildingData1;
    [SerializeField] private BuildingData buildingData2;
    [SerializeField] private BuildingData buildingData3;

    [SerializeField] private BuildingPreview previewPrefab;
    [SerializeField] private Building buildingPrefab;
    [SerializeField] private BuildingGrid grid;

    private BuildingPreview preview;
    private bool isMovingBuilding = false;

    // Pola do przechowywania danych oryginalnego budynku podczas przenoszenia
    private BuildingData oldData;
    private float oldRotation;
    private Vector3 oldCenterPos;
    private List<Vector3> oldPositions;

    private BuildingEQ eq;  // zamiast starego inventory

    private void Start()
    {
        eq = FindObjectOfType<BuildingEQ>();
        if (eq != null)
        {
            eq.Initialize(new List<BuildingData> { buildingData1, buildingData2, buildingData3 });
        }
    }

    private void Update()
    {
        Vector3 mousePos = GetMouseWorldPosition();

        if (preview != null)
        {
            HandlePreview(mousePos);

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelCurrentPreview();
            }
            else if (isMovingBuilding)
            {
                // Dla trybu drag & drop (przenoszenie): postaw na MouseUp, je?li valid, inaczej cancel (przywró?)
                if (Input.GetMouseButtonUp(0))
                {
                    if (preview.State == BuildingPreview.BuildingPreviewState.POSITIVE)
                    {
                        List<Vector3> finalBuildPosition = preview.BuildingModels.GetRotatedShapeUnitOffsets()
                            .Select(offset => preview.transform.position + offset).ToList();
                        PlaceBuilding(finalBuildPosition);
                    }
                    else
                    {
                        CancelCurrentPreview();
                    }
                }
            }
            else
            {
                // Dla normalnego budowania: postaw na MouseDown, je?li valid
                if (Input.GetMouseButtonDown(0))
                {
                    if (preview.State == BuildingPreview.BuildingPreviewState.POSITIVE)
                    {
                        List<Vector3> finalBuildPosition = preview.BuildingModels.GetRotatedShapeUnitOffsets()
                            .Select(offset => preview.transform.position + offset).ToList();
                        PlaceBuilding(finalBuildPosition);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                preview.AddRotation(90);
            }
        }
    }

    public void CancelCurrentPreview()
    {
        if (isMovingBuilding)
        {
            // Przywró? oryginalny budynek w starej pozycji
            Building restoredBuilding = Instantiate(buildingPrefab, oldCenterPos, Quaternion.identity);
            restoredBuilding.Setup(oldData, oldRotation);
            grid.SetBuilding(restoredBuilding, oldPositions);
            isMovingBuilding = false;
        }

        if (preview != null)
        {
            Destroy(preview.gameObject);
            preview = null;
        }
    }

    private void HandlePreview(Vector3 mouseWorldPosition)
    {
        List<Vector3> rotatedOffsets = preview.BuildingModels.GetRotatedShapeUnitOffsets();

        List<Vector3> worldPositionsBasedOnMouse = rotatedOffsets.Select(offset => mouseWorldPosition + offset).ToList();

        bool canBuild = grid.CanBuild(worldPositionsBasedOnMouse);

        if (canBuild)
        {
            Vector3 snappedCenterPosition = GetSnappedCenterPosition(worldPositionsBasedOnMouse);

            preview.transform.position = snappedCenterPosition;
            preview.ChangeState(BuildingPreview.BuildingPreviewState.POSITIVE);
        }
        else
        {
            preview.transform.position = mouseWorldPosition;
            preview.ChangeState(BuildingPreview.BuildingPreviewState.NEGATIVE);
        }
    }

    private void PlaceBuilding(List<Vector3> buildingPositions)
    {
        Building building = Instantiate(buildingPrefab, preview.transform.position, Quaternion.identity);
        building.Setup(preview.Data, preview.BuildingModels.Rotation);
        grid.SetBuilding(building, buildingPositions);

        Destroy(preview.gameObject);
        preview = null;
        isMovingBuilding = false;
    }

    private Vector3 GetSnappedCenterPosition(List<Vector3> allBuildingPosition)
    {
        List<int> xs = allBuildingPosition.Select(p => Mathf.FloorToInt(p.x)).ToList();
        List<int> zs = allBuildingPosition.Select(p => Mathf.FloorToInt(p.z)).ToList();

        float centerX = (xs.Min() + xs.Max()) / 2f + CellSize / 2f;
        float centerZ = (zs.Min() + zs.Max()) / 2f + CellSize / 2f;

        return new Vector3(centerX, grid.transform.position.y, centerZ);
    }

    public Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private BuildingPreview CreatePreview(BuildingData data, Vector3 position)
    {
        BuildingPreview buildingPreview = Instantiate(previewPrefab, position, Quaternion.identity);
        buildingPreview.Setup(data);
        isMovingBuilding = false; // Dla normalnego preview
        return buildingPreview;
    }

    public bool HasActivePreview() => preview != null;

    public void CancelPreview()
    {
        CancelCurrentPreview();
    }

    public void StartMovingBuilding(Building buildingToMove, BuildingGrid grid)
    {
        if (preview != null) return;

        // Zapami?taj oryginalne pozycje i dane przed zniszczeniem
        oldPositions = buildingToMove.Data.Model.GetAllBuldingPosition(); // Zak?adam, ?e to statyczna metoda lub dost?pna
        oldRotation = buildingToMove.Rotation;
        oldCenterPos = buildingToMove.transform.position;
        oldData = buildingToMove.Data;

        // Wyczy?? komórki
        List<BuildingGridCell> cellsToClear = new List<BuildingGridCell>();
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                var cell = grid.GetCell(x, y);
                if (cell.GetBuilding() == buildingToMove)
                {
                    cellsToClear.Add(cell);
                }
            }
        }

        foreach (var cell in cellsToClear)
        {
            cell.Clear();
        }

        // Zniszcz stary budynek
        Destroy(buildingToMove.gameObject);

        // Stwórz preview w pozycji myszy dla p?ynnego drag
        Vector3 mousePos = GetMouseWorldPosition();
        preview = CreatePreview(oldData, mousePos);
        preview.SetRotation(oldRotation);

        // Ustaw flag? drag & drop
        isMovingBuilding = true;
    }

    public BuildingPreview CreatePreviewFromInventory(BuildingData data, Vector3 position)
    {
        if (preview != null) Destroy(preview.gameObject);
        preview = CreatePreview(data, position);
        return preview;
    }
}