using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手（または提示対象の頬）の左右
/// </summary>
public enum HandSide
{
    Right, // 右手 → 右頬
    Left   // 左手 → 左頬
}

/// <summary>
/// 論文手法（Method A: バーチャルの指から頬への触覚リマッピング）に基づく触覚マッパー
/// 論文「振動刺激を用いた指から頬への触覚リマッピングシステムの開発」2.2節の数式に従い計算します。
/// </summary>
public class MethodAHapticMapper : MonoBehaviour, IHapticMapper
{
    [Header("手の設定")]
    [Tooltip("対象の手（右手→右頬、左手→左頬）")]
    [SerializeField] private HandSide handSide = HandSide.Right;

    [Tooltip("手掌のTransform（設定されている場合、手の姿勢・回転を考慮した手のローカル座標系で計算します）")]
    [SerializeField] private Transform handTransform;

    [Header("動的正規化パラメータ Wx(t), Wy(t)")]
    [Tooltip("全指先のTransform（5指のTransformを登録すると、時刻tで最も離れた指先距離Wx(t), Wy(t)を動的に算出します）")]
    [SerializeField] private List<Transform> fingerTips = new List<Transform>();

    [Tooltip("指先リスト未設定時またはフォールバック用の基準幅 Wx (m)")]
    [SerializeField] private float defaultWx = 0.08f;

    [Tooltip("指先リスト未設定時またはフォールバック用の基準高さ Wy (m)")]
    [SerializeField] private float defaultWy = 0.09f;

    [Tooltip("Wx, Wy の最小値（ゼロ除算防止用）")]
    [SerializeField] private float minW = 0.01f;

    [Header("デバッグ")]
    [Tooltip("計算結果と入力座標をConsoleにログ出力するか")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>
    /// 手掌中心 (phand) と指先位置 (pfinger) から、5x5モータアレイの行 i (1〜5) と列 j (1〜5) を計算します。
    /// </summary>
    /// <param name="fingerPos">指先の座標 (ワールド座標)</param>
    /// <param name="handPos">手掌中心の座標 (ワールド座標)</param>
    /// <returns>Vector2Int(i, j) ※ x = 行 i (1〜5: 上から下), y = 列 j (1〜5: 鼻側から耳側)</returns>
    public Vector2Int CalculateMotorIndex(Vector3 fingerPos, Vector3 handPos)
    {
        // ----------------------------------------------------
        // 1. 手の座標系における指先の相対位置 D = (dx, dy, dz) の計算 (Equation 1)
        //    dx: 手の左右方向（母指-小指方向）
        //    dy: 手の上下方向（手首-指先方向）
        //    dz: 手掌の法線方向（奥行き）
        // ----------------------------------------------------
        float dx, dy, dz;
        if (handTransform != null)
        {
            // 手の向き（姿勢）を考慮したローカル座標系に変換
            Vector3 localFinger = handTransform.InverseTransformPoint(fingerPos);
            dx = localFinger.x;
            dy = localFinger.y;
            dz = localFinger.z;
        }
        else
        {
            // Transform未指定時は直接の差分ベクトルを使用
            Vector3 diff = fingerPos - handPos;
            dx = diff.x;
            dy = diff.y;
            dz = diff.z;
        }

        // ----------------------------------------------------
        // 2. 最も離れた指先距離 Wx(t), Wy(t) の取得と [0, 1] への正規化 (Equation 2)
        //    d^x = (dx + Wx(t)) / (2 * Wx(t))
        //    d^y = (dy + Wy(t)) / (2 * Wy(t))
        // ----------------------------------------------------
        float wx = defaultWx;
        float wy = defaultWy;

        // 全指先のTransformが登録されていれば、時刻tにおける最大変位を動的に計算
        if (fingerTips != null && fingerTips.Count > 0)
        {
            float maxAbsX = 0f;
            float maxAbsY = 0f;

            foreach (var tip in fingerTips)
            {
                if (tip == null) continue;
                Vector3 tipLocal = (handTransform != null)
                    ? handTransform.InverseTransformPoint(tip.position)
                    : (tip.position - handPos);

                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(tipLocal.x));
                maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(tipLocal.y));
            }

            if (maxAbsX > minW) wx = maxAbsX;
            if (maxAbsY > minW) wy = maxAbsY;
        }

        // ゼロ除算防止
        wx = Mathf.Max(wx, minW);
        wy = Mathf.Max(wy, minW);

        // Equation 2: [0, 1] への正規化
        float normDx = (dx + wx) / (2f * wx);
        float normDy = (dy + wy) / (2f * wy);

        // ----------------------------------------------------
        // 3. 列インデックス j の計算 (Equation 3, 4)
        //    1 (鼻側/内側) 〜 5 (耳側/外側)
        // ----------------------------------------------------
        int j;
        if (handSide == HandSide.Right)
        {
            // Equation 3: 右頬 (dx > 0 が耳側)
            // j_right = clip(round(4 * d^x + 1))
            j = Clip(Mathf.RoundToInt(4f * normDx + 1f));
        }
        else
        {
            // Equation 4: 左頬 (左右反転)
            // j_left = clip(round(4 * (1 - d^x) + 1))
            j = Clip(Mathf.RoundToInt(4f * (1f - normDx) + 1f));
        }

        // ----------------------------------------------------
        // 4. 行インデックス i の計算 (Equation 5)
        //    1 (上段) 〜 5 (下段)
        //    手の上方向 (dy > 0) が上段 (i = 1) のため、d^y を反転
        //    i = clip(round(4 * (1 - d^y) + 1))
        // ----------------------------------------------------
        int i = Clip(Mathf.RoundToInt(4f * (1f - normDy) + 1f));

        // 1次元モータID（1〜25: 左上から右に向かって 1, 2, 3...）
        int motorId = GetMotorId(i, j);

        // ----------------------------------------------------
        // デバッグログ出力
        // ----------------------------------------------------
        if (enableDebugLog)
        {
            Debug.Log($"<color=#7CFC00>[MethodAHapticMapper]</color> " +
                      $"HandSide: {handSide} | " +
                      $"p_hand: {handPos:F3}, p_finger: {fingerPos:F3} | " +
                      $"D: (dx={dx:F3}, dy={dy:F3}) [Wx={wx:F3}, Wy={wy:F3}] | " +
                      $"Norm: (d^x={normDx:F2}, d^y={normDy:F2}) => " +
                      $"<b>Matrix: 行 i={i}, 列 j={j}</b> (モータID: {motorId})");
        }

        // x: 行 i (1〜5), y: 列 j (1〜5)
        return new Vector2Int(i, j);
    }

    /// <summary>
    /// 論文のクリッピング関数: clip(v) = min(5, max(1, v))
    /// </summary>
    private int Clip(int value)
    {
        return Mathf.Clamp(value, 1, 5);
    }

    /// <summary>
    /// 5x5グリッドの行 i (1〜5) と列 j (1〜5) から 1次元のモータIDを算出します。
    /// （左上(1,1)を1とし、右へ向かって1〜5、2行目が6〜10、計1〜25）
    /// </summary>
    /// <param name="i">行番号 (1〜5)</param>
    /// <param name="j">列番号 (1〜5)</param>
    /// <param name="zeroIndexed">0始まり (0〜24) で取得する場合はtrue</param>
    /// <returns>モータID (1〜25 または 0〜24)</returns>
    public int GetMotorId(int i, int j, bool zeroIndexed = false)
    {
        int id1Based = (i - 1) * 5 + j;
        return zeroIndexed ? (id1Based - 1) : id1Based;
    }

    /// <summary>
    /// 左右の手・頬を動的に設定します
    /// </summary>
    public void SetHandSide(HandSide side)
    {
        handSide = side;
    }

    /// <summary>
    /// 手掌のTransformを設定します
    /// </summary>
    public void SetHandTransform(Transform t)
    {
        handTransform = t;
    }

    /// <summary>
    /// 5本の指先Transformリストを設定します（動的正規化用）
    /// </summary>
    public void SetFingerTips(IEnumerable<Transform> tips)
    {
        fingerTips.Clear();
        if (tips != null)
        {
            fingerTips.AddRange(tips);
        }
    }
}
