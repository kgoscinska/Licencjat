using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class BuildingModels : MonoBehaviour
{
    [SerializeField] private Transform Wrapper;
    public float Rotation => Wrapper.transform.eulerAngles.y;

    private BuildingShapeUnit[] shapeUnits;

    private void Awake()
    {
        shapeUnits = GetComponentsInChildren<BuildingShapeUnit>();
    }

    public void Rotate(float rotationStep)
    {
        Wrapper.Rotate(new(0, rotationStep, 0));
    }
    public List<Vector3> GetAllBuldingPosition()
    {
        return shapeUnits.Select(unit => unit.transform.position).ToList();
    }
}
