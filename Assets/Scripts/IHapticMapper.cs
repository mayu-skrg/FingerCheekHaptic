using UnityEngine;

/// <summary>
/// 触覚リマッピングにおけるモータインデックス計算用インターフェース
/// </summary>
public interface IHapticMapper
{
    /// <summary>
    /// 指先位置と手（基準位置）から対応する振動モータのインデックス（2Dグリッド配列等）を計算します。
    /// </summary>
    /// <param name="fingerPos">指先のワールド座標またはローカル座標</param>
    /// <param name="handPos">手（掌や手首などの基準）のワールド座標またはローカル座標</param>
    /// <returns>対応するモータのインデックス (X: 列, Y: 行など。該当なし/無効の場合は (-1, -1))</returns>
    Vector2Int CalculateMotorIndex(Vector3 fingerPos, Vector3 handPos);
}
