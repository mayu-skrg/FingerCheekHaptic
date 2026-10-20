using UnityEngine;

/// <summary>
/// 比較用手法（Method B / 仮クラス）に基づく触覚マッパー
/// </summary>
public class MethodBHapticMapper : MonoBehaviour, IHapticMapper
{
    [Header("モータグリッド設定")]
    [Tooltip("モータアレイの列数（X方向）")]
    [SerializeField] private int gridColumns = 3;

    [Tooltip("モータアレイの行数（Y方向）")]
    [SerializeField] private int gridRows = 3;

    /// <summary>
    /// 指先位置と手の基準位置から、対応する振動モータのグリッドインデックスを計算します（Method B: 仮実装）
    /// </summary>
    /// <param name="fingerPos">指先の位置</param>
    /// <param name="handPos">手の基準位置</param>
    /// <returns>対応するモータのグリッド座標 (Column, Row)</returns>
    public Vector2Int CalculateMotorIndex(Vector3 fingerPos, Vector3 handPos)
    {
        // 【比較手法用ロジックのプレースホルダー】
        // 例: 単純な距離ベースや角度ベース等の別アルゴリズム
        Vector3 relativePos = fingerPos - handPos;

        // 簡易的なマッピング例
        int col = Mathf.Clamp(Mathf.FloorToInt((relativePos.x + 0.05f) / 0.1f * gridColumns), 0, gridColumns - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((relativePos.y + 0.05f) / 0.1f * gridRows), 0, gridRows - 1);

        return new Vector2Int(col, row);
    }
}
