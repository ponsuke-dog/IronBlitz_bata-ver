using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class AutoFitBoxCollider : MonoBehaviour
{
    public Vector3 sizeOffset;
    public Vector3 centerOffset;

    public MeshFilter meshFilter;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Sync();
    }
#endif

    void Sync()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshFilter == null)
        {
            return; // ”O‚Ì‚½‚ß
        }

        var mesh = meshFilter.sharedMesh;

        if (mesh == null)
        {
            return; // ”O‚Ì‚½‚ß
        }

        var col = GetComponent<BoxCollider>();

        Vector3 scaleSize = Vector3.Scale(mesh.bounds.size, meshFilter.transform.lossyScale);

        col.size = scaleSize + sizeOffset;
        col.center = transform.InverseTransformPoint(meshFilter.transform.TransformPoint(mesh.bounds.center)) + centerOffset;

    }
}
