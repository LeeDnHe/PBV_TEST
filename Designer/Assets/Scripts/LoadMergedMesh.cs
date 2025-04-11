using UnityEngine;

public class LoadMergedMesh : MonoBehaviour
{
    public Mesh mergedMesh; // Inspector에서 병합된 Mesh를 할당

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mergedMesh;
    }
}