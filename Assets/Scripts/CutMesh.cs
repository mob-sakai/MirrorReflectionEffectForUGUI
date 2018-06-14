using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// cubeなどの3Dオブジェクトにくっつけてmesh情報をもらうクラス
// カット出来るmeshクラスというイメージだが、面倒だからMeshFilterから直でもらってもいいね。
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class CutMesh : MonoBehaviour
{
    private Mesh _mesh;

    void Start()
    {
        Create();
    }

    public void Create()
    {
        var meshFilter = GetComponent<MeshFilter>();
        _mesh = meshFilter.mesh;
    }

    public Mesh Mesh
    {
        get
        {
            return _mesh;
        }
    }

    public Vector3[] Vertices
    {
        get
        {
            return _mesh.vertices;
        }
    }
}