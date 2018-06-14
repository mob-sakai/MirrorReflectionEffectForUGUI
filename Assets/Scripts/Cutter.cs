using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 実際にオブジェクトをカットする処理クラス
public class Cutter : MonoBehaviour
{

    public Material mat;

    private Vector3 _pos1; // planeとmeshの交点その1
    private Vector3 _pos2; // planeとmeshの交点その2


    public void Cut(Plane plane , CutMesh _cutMesh)
    {
        var group1ObjPosList = new List<Vector3>();
        var group1ObjTriList = new List<int>();
        var group1CapPosList = new List<Vector3>();
        var group1CapTriList = new List<int>();

        var group2ObjPosList = new List<Vector3>();
        var group2ObjTriList = new List<int>();
        var group2CapPosList = new List<Vector3>();
        var group2CapTriList = new List<int>();

        // 色々必要になってしまった
        var meshTriangles = _cutMesh.Mesh.triangles;
        var meshVertices = _cutMesh.Mesh.vertices;
        var meshNormals = _cutMesh.Mesh.normals;
        var meshPos = _cutMesh.transform.position;
        var meshScale = _cutMesh.transform.localScale;

        for (var i = 0; i < meshTriangles.Length; i += 3)
        {
            var group1PosList = new List<Vector3>();
            var group1TriList = new List<int>();
            var group2PosList = new List<Vector3>();
            var group2TriList = new List<int>();

            var idx0 = meshTriangles[i];
            var idx1 = meshTriangles[i + 1];
            var idx2 = meshTriangles[i + 2];

			// ワールド座標
            var verts = new List<Vector3>();

            // 頂点位置をscaleやpositionに合わせてしっかり計算しないとおかしくなる
            // あれ、もうmatrixで計算したほうがいい？
			var m = _cutMesh.transform.localToWorldMatrix;
//            var v1 = Vector3.Scale(meshVertices[idx0], meshScale) + meshPos;
//            var v2 = Vector3.Scale(meshVertices[idx1], meshScale) + meshPos;
//			var v3 = Vector3.Scale(meshVertices[idx2], meshScale) + meshPos;
			var v1 = m.MultiplyPoint3x4(meshVertices[idx0]);
			var v2 = m.MultiplyPoint3x4(meshVertices[idx1]);
			var v3 = m.MultiplyPoint3x4(meshVertices[idx2]);

            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            // そのポリゴンの法線を計算しておく
            var normal = Vector3.Cross(meshVertices[idx2] - meshVertices[idx0], meshVertices[idx1] - meshVertices[idx0]);


            CheckPlaneSide(plane, verts, group1PosList, group2PosList); // 1.グループ分け

            // どちらにもカウントがあるということはplateと交差しているポリゴンということ
            if (group1PosList.Count > 0 && group2PosList.Count > 0)
            {
                CalcCrossPoint(plane, group1PosList, group2PosList); // 2.planeとの交点を求める

                // 3.両方のグループともに交点を入れる
                group1PosList.Add(_pos1);
                group1PosList.Add(_pos2);

                // capping用の表
                group1CapPosList.Add(_pos1);
                group1CapPosList.Add(_pos2);


                group2PosList.Add(_pos1);
                group2PosList.Add(_pos2);

                // capping用裏側
                group2CapPosList.Add(_pos1);
                group2CapPosList.Add(_pos2);
            }

            if (group1PosList.Count > 0)
            {
                var tris1 = CreateTriangles(group1PosList , normal);
                var triIdx = group1ObjPosList.Count;

                group1ObjPosList.AddRange(group1PosList);

                // 二つめ以降ならidxがずれることに注意
                foreach (var triI in tris1)
                {
                    group1ObjTriList.Add(triI + triIdx);
                }
            }

            if (group2PosList.Count > 0)
            {
                var tris2 = CreateTriangles(group2PosList , normal);
                var triIdx = group2ObjPosList.Count;

                group2ObjPosList.AddRange(group2PosList);

                // 二つめ以降ならidxがずれることに注意
                foreach (var triI in tris2)
                {
                    group2ObjTriList.Add(triI + triIdx);
                }
            }
            
        }

        // 両方の蓋を求める
//        Capping(group1CapPosList, group1CapTriList, plane , true);
//        Capping(group2CapPosList, group2CapTriList, plane , false);
//
//        // 1に蓋をマージ
//        var tri1Idx = group1ObjPosList.Count;
//        group1ObjPosList.AddRange(group1CapPosList);
//        foreach (var idx1 in group1CapTriList)
//        {
//            group1ObjTriList.Add(tri1Idx + idx1);
//        }
//
//        // 2に蓋をマージ
//        var tri2Idx = group2ObjPosList.Count;
//        group2ObjPosList.AddRange(group2CapPosList);
//        foreach (var idx2 in group2CapTriList)
//        {
//            group2ObjTriList.Add(tri2Idx + idx2);
//        }

        // 4.2つのグループに分けたオブジェクトを作成する
        CreateCutObj(group1ObjPosList, group1ObjTriList);
        CreateCutObj(group2ObjPosList, group2ObjTriList);


        _cutMesh.gameObject.SetActive(false); // 5.元となるオブジェクトを非表示にする
    }

    // planeのどちらにあるかを計算して振り分ける
    private void CheckPlaneSide(Plane plane, List<Vector3> vertices, List<Vector3> group1, List<Vector3> group2)
    {
        foreach (var v in vertices)
        {
            // どちらかのグループに振り分ける
            if (plane.GetSide(v))
            {
                group1.Add(v);
            }
            else
            {
                group2.Add(v);
            }
        }
    }

    // planeとmeshの交点を求める
    private void CalcCrossPoint(Plane plane, List<Vector3> group1, List<Vector3> group2)
    {
        float distance = 0;
        Vector3 basePos; // 計算する基準となる頂点
        Vector3 tmpPos1; // 基準点以外の頂点1
        Vector3 tmpPos2; // 基準点以外の頂点2

        // 少ない方からplaneに対して交差するpointを聞く
        if (group2.Count < group1.Count)
        {
            basePos = group2[0];
            tmpPos1 = group1[0];
			tmpPos2 = group1[1];
        }
        else
        {
            basePos = group1[0];
            tmpPos1 = group2[0];
			tmpPos2 = group2[1];
        }


//		_pos1 = plane.ClosestPointOnPlane (tmpPos1);
//		_pos2 = plane.ClosestPointOnPlane (tmpPos2);

        // 少ない所から多い片方の頂点に向かってrayを飛ばす。
		Ray ray1 = new Ray(basePos, (tmpPos1 - basePos));
        // planeと交差する距離を求める
        plane.Raycast(ray1, out distance);
        // ray1がその距離を進んだ位置を取得(ここが交点になる)
        _pos1 = ray1.GetPoint(distance);

        // 同じようにもう片方も計算
        Ray ray2 = new Ray(basePos, (tmpPos2 - basePos));
        plane.Raycast(ray2, out distance);
        _pos2 = ray2.GetPoint(distance);
    }

    // 頂点インデックスを計算する
    private List<int> CreateTriangles(List<Vector3> pos , Vector3 normal)
    {
        if (pos.Count < 3)
        {
            return null;
        }


        var triangles = new List<int>();

        var triIdx = 0;
        var triIdx0 = 0; // 0固定
        var triIdx1 = 0;
        var triIdx2 = 0;
        var cross = Vector3.zero;
        var inner = 0.0f;

        for (int i = 0; i < pos.Count; i += 3)
        {
            triIdx0 = triIdx;
            triIdx1 = triIdx + 1;
            triIdx2 = triIdx + 2;

            cross = Vector3.Cross(pos[triIdx2] - pos[triIdx0], pos[triIdx1] - pos[triIdx0]);
            inner = Vector3.Dot(cross, normal);

            // 逆向いている場合は反転させる
            if (inner < 0)
            {
                triIdx0 = triIdx2;
                triIdx2 = triIdx;
            }

            triangles.Add(triIdx0);
            triangles.Add(triIdx1);
            triangles.Add(triIdx2);
            triIdx++;
        }

        return triangles;
    }

    // 切ったオブジェクトの蓋を作る
//    private void Capping(List<Vector3> capPosList , List<int> triList , Plane plane ,bool isFront)
//    {
//        // 中心点を求める
//        Vector3 center = Vector3.zero;
//        foreach(var v in capPosList)
//        {
//            center += v;
//        }
//        center = center / capPosList.Count;
//
//        // 中心点を入れる
//        capPosList.Add(center);
//
//        var centerIdx = capPosList.Count - 1;
//        for (int i = 0; i < capPosList.Count - 1; i += 2)
//        {
//            var idx0 = centerIdx;
//            var idx1 = i;
//            var idx2 = i + 1;
//
//            var cross = Vector3.Cross(capPosList[idx2] - capPosList[idx0], capPosList[idx1] - capPosList[idx0]);
//            var inner = Vector3.Dot(cross, plane.normal);
//
//            // plateに対してどちら側の蓋かによって計算が変わるのに注意
//            if (isFront)
//            {
//                if(inner < 0)
//                {
//                    idx0 = idx2;
//                    idx2 = centerIdx;
//                }
//            }
//            else
//            {
//                if (inner > 0)
//                {
//                    idx0 = idx2;
//                    idx2 = centerIdx;
//                }
//
//            }
//
//            // indexを詰める
//            triList.Add(idx0);
//            triList.Add(idx1);
//            triList.Add(idx2);
//        }
//
//    }

    // cutしたmeshを作る
    private void CreateCutObj(List<Vector3> verts, List<int> tris)
    {
        var obj = new GameObject("cut obj", typeof(MeshFilter), typeof(MeshRenderer),typeof(MeshCollider), typeof(Rigidbody));

        var mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshRenderer>().material = mat;
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;
        obj.GetComponent<MeshCollider>().convex = true;

        var rigidBody = obj.GetComponent<Rigidbody>();
        rigidBody.AddForce(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5), ForceMode.Impulse);
    }

}