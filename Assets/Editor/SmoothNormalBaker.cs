using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Smooth Normal Baker
///
/// 【使い方】
/// Unity メニュー → Tools → Bake Smooth Normals
///
/// シーン上のアウトライン付きシェーダーを持つ全オブジェクトを自動検索し、
/// Smooth Normals を UV2 に書き込む。
///
/// 【保存しない運用】
/// アセットへの保存・シーンへの埋め込みは一切行わない。
/// Unityを開くたびに一度実行するだけでよい（数秒で完了）。
/// ビルド前にも一度実行すること。
/// </summary>
public static class SmoothNormalBaker
{
    private static readonly string[] TargetShaderNames = new[]
    {
        "Custom/URP/HalftoneWithOutline",
        "Custom/URP/HalftoneShaderWithOutline",
        "Custom/URP/HalftoneWithOutline_Player",
        "Custom/URP/HalftoneShaderWithOutline_Player",
    };
    private static Vector3 RoundVertex(Vector3 v, float precision = 1000f)
    {
        return new Vector3(
            Mathf.Round(v.x * precision) / precision,
            Mathf.Round(v.y * precision) / precision,
            Mathf.Round(v.z * precision) / precision
        );
    }

    [MenuItem("Tools/Bake Smooth Normals")]
    private static void BakeAll()
    {
        var targetShaders = new HashSet<Shader>();
        foreach (string name in TargetShaderNames)
        {
            Shader s = Shader.Find(name);
            if (s != null) targetShaders.Add(s);
        }

        if (targetShaders.Count == 0)
        {
            EditorUtility.DisplayDialog("Smooth Normal Baker",
                "対象シェーダーが見つかりません。", "OK");
            return;
        }

        int processedMesh = 0;

        Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Renderer renderer in allRenderers)
        {
            bool hasTargetShader = false;
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && targetShaders.Contains(mat.shader))
                {
                    hasTargetShader = true;
                    break;
                }
            }
            if (!hasTargetShader) continue;

            if (renderer is MeshRenderer)
            {
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    // edit modeではsharedMeshを使う（mf.meshは警告が出る）
                    WriteSmoothNormals(mf.sharedMesh);
                    // 明示的にGPUへアップロード（false=書き込み可能のまま）
                    mf.sharedMesh.UploadMeshData(false);
                    processedMesh++;
                }
            }
            else if (renderer is SkinnedMeshRenderer smr)
            {
                if (smr.sharedMesh != null)
                {
                    WriteSmoothNormals(smr.sharedMesh);
                    smr.sharedMesh.UploadMeshData(false);
                    processedMesh++;
                }
            }
        }

        Debug.Log($"[SmoothNormalBaker] 完了：{processedMesh} メッシュをベイクしました。");
        EditorUtility.DisplayDialog("Smooth Normal Baker",
            $"完了しました。{processedMesh} メッシュにSmooth Normalsを書き込みました。\n\n" +
            "※ Unityを再起動したら再度実行してください。",
            "OK");
    }

    [MenuItem("Tools/Clear Smooth Normals")]
    private static void ClearAll()
    {
        var targetShaders = new HashSet<Shader>();
        foreach (string name in TargetShaderNames)
        {
            Shader s = Shader.Find(name);
            if (s != null) targetShaders.Add(s);
        }

        if (targetShaders.Count == 0)
        {
            EditorUtility.DisplayDialog("Smooth Normal Baker",
                "対象シェーダーが見つかりません。", "OK");
            return;
        }

        int processedMesh = 0;

        Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Renderer renderer in allRenderers)
        {
            bool hasTargetShader = false;
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && targetShaders.Contains(mat.shader))
                {
                    hasTargetShader = true;
                    break;
                }
            }
            if (!hasTargetShader) continue;

            if (renderer is MeshRenderer)
            {
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    // UV2を空配列でクリア
                    mf.sharedMesh.SetUVs(1, (List<Vector3>)null);
                    processedMesh++;
                }
            }
            else if (renderer is SkinnedMeshRenderer smr)
            {
                if (smr.sharedMesh != null)
                {
                    smr.sharedMesh.SetUVs(1, new Vector3[0]);
                    processedMesh++;
                }
            }
        }

        Debug.Log($"[SmoothNormalBaker] クリア完了：{processedMesh} メッシュ");
        EditorUtility.DisplayDialog("Smooth Normal Baker",
            $"クリアしました。{processedMesh} メッシュのSmooth Normalsを削除しました。",
            "OK");
    }

    private static void WriteSmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        // WriteSmoothNormals の先頭に追加
        Debug.Log($"[Baker] 最初の8頂点の法線：");
        for (int i = 0; i < Mathf.Min(8, normals.Length); i++)
            Debug.Log($"  [{i}] 座標{RoundVertex(vertices[i])} 法線{normals[i]}");

        if (normals == null || normals.Length == 0)
        {
            Debug.LogWarning($"[SmoothNormalBaker] '{mesh.name}' に法線データがありません。スキップします。");
            return;
        }

        // ── 座標を丸めてグループ化（浮動小数点ズレ対策） ──
        var groupNormals = new Dictionary<Vector3, Vector3>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 key = RoundVertex(vertices[i]);
            if (!groupNormals.ContainsKey(key))
                groupNormals[key] = Vector3.zero;
            groupNormals[key] += normals[i];
        }

        // デバッグ：グループ数確認（標準Cubeなら8グループになるはず）
        Debug.Log($"[Baker] {mesh.name}: 頂点{vertices.Length}個 → {groupNormals.Count}グループ");

        // ── 各頂点に平均法線を割り当て ──
        var smoothNormals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 key = RoundVertex(vertices[i]);
            Vector3 avg = groupNormals[key];
            smoothNormals[i] = avg.sqrMagnitude > 0.0001f
                ? avg.normalized
                : normals[i];
        }

        mesh.SetUVs(1, new List<Vector3>(smoothNormals));
        Debug.Log($"[SmoothNormalBaker] ベイク：{mesh.name} ({vertices.Length} verts)");
    }
    /*
    private static void WriteSmoothNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        if (normals == null || normals.Length == 0)
        {
            Debug.LogWarning($"[SmoothNormalBaker] '{mesh.name}' に法線データがありません。スキップします。");
            return;
        }

        int vertCount = vertices.Length;

        // ── 座標が近い頂点をグループ化（epsilon比較） ──────────────────
        // Vector3をDictionaryキーにすると浮動小数点誤差で失敗するため
        // O(n²)だが頂点数が数千程度なら問題なし
        int[] groupId = new int[vertCount];
        for (int i = 0; i < vertCount; i++) groupId[i] = i; // 初期値：自分自身

        float epsilon = 0.0001f;
        for (int i = 0; i < vertCount; i++)
        {
            for (int j = i + 1; j < vertCount; j++)
            {
                if (groupId[j] != j) continue; // すでに別グループに統合済み
                if (Vector3.SqrMagnitude(vertices[i] - vertices[j]) < epsilon * epsilon)
                    groupId[j] = i; // jをiのグループに統合
            }
        }

        // ── グループごとに法線を平均化 ─────────────────────────────────
        var groupNormals = new Dictionary<int, Vector3>();
        for (int i = 0; i < vertCount; i++)
        {
            int id = groupId[i];
            if (!groupNormals.ContainsKey(id)) groupNormals[id] = Vector3.zero;
            groupNormals[id] += normals[i];
        }

        // ── 各頂点にスムーズ法線を割り当て ────────────────────────────
        var smoothNormals = new Vector3[vertCount];
        for (int i = 0; i < vertCount; i++)
        {
            Vector3 avg = groupNormals[groupId[i]];
            smoothNormals[i] = avg.sqrMagnitude > 0.0001f
                ? avg.normalized
                : normals[i];
        }

        mesh.SetUVs(1, smoothNormals);
        Debug.Log($"[SmoothNormalBaker] ベイク：{mesh.name} ({vertCount} verts)");
    }
    */
}
