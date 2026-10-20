using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 接触中の指先情報
/// </summary>
public class FingerContactInfo
{
    public Collider Collider { get; set; }
    public Transform FingerTransform { get; set; }
    public int ActiveMotorId { get; set; } = -1; // 現在振動中のモータID (1〜25, なしは -1)
}

/// <summary>
/// 仮想の球などのオブジェクトにアタッチし、指先との接触を検知して
/// 実験条件（VisualOnly / Fixed / Dynamic）に応じた触覚モータ駆動命令を送信するクラス
/// </summary>
[RequireComponent(typeof(Collider))]
public class HapticObjectInteraction : MonoBehaviour
{
    [Header("参照設定")]
    [Tooltip("手掌（手のひら）のTransform。未設定の場合は指先コライダーの親階層を自動探索します。")]
    [SerializeField] private Transform handCenterTransform;

    [Tooltip("Arduino/シリアル送信コントローラー（未アサイン時は自動検出）")]
    [SerializeField] private ArduinoHapticController hapticController;

    [Header("接触フィルタ設定")]
    [Tooltip("指先コライダーを判定するタグ（空欄の場合は名前等で判定）")]
    [SerializeField] private string fingerTag = "";

    [Tooltip("接触対象とするレイヤー")]
    [SerializeField] private LayerMask contactLayer = ~0;

    [Header("デバッグ")]
    [Tooltip("接触イベントや駆動命令のログを表示するか")]
    [SerializeField] private bool showDebugLog = true;

    // 現在接触している指先リスト (Collider単位)
    private readonly Dictionary<Collider, FingerContactInfo> activeContacts = new Dictionary<Collider, FingerContactInfo>();

    private void Awake()
    {
        // コライダーがTriggerになっているか確認
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (hapticController == null)
        {
            hapticController = FindFirstObjectByType<ArduinoHapticController>();
        }
    }

    private void Start()
    {
        // 実験条件が切り替わったときのイベントを購読
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnConditionChanged += HandleConditionChanged;
        }
    }

    private void OnDestroy()
    {
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnConditionChanged -= HandleConditionChanged;
        }
        StopAllContacts();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidFinger(other)) return;

        // 新規接触指の登録
        var contactInfo = new FingerContactInfo
        {
            Collider = other,
            FingerTransform = other.transform,
            ActiveMotorId = -1
        };
        activeContacts[other] = contactInfo;

        if (showDebugLog)
        {
            Debug.Log($"<color=#32CD32>[HapticObjectInteraction]</color> 指先が接触しました: {other.name}");
        }

        // 接触開始時のモータ計算・駆動
        UpdateHapticsForFinger(contactInfo, isFirstTouch: true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!activeContacts.TryGetValue(other, out var contactInfo)) return;

        // Dynamicモードの場合のみ、毎フレーム最新位置でモータを更新
        if (ExperimentManager.Instance != null && ExperimentManager.Instance.IsDynamicMode)
        {
            UpdateHapticsForFinger(contactInfo, isFirstTouch: false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!activeContacts.TryGetValue(other, out var contactInfo)) return;

        if (showDebugLog)
        {
            Debug.Log($"<color=#FF6347>[HapticObjectInteraction]</color> 指先が離脱しました: {other.name}");
        }

        // 現在振動中のモータを停止
        if (contactInfo.ActiveMotorId > 0 && hapticController != null)
        {
            hapticController.StopMotor(contactInfo.ActiveMotorId);
        }

        activeContacts.Remove(other);

        // すべての指が離れた場合は安全のため全停止
        if (activeContacts.Count == 0 && hapticController != null)
        {
            hapticController.StopAllMotors();
        }
    }

    /// <summary>
    /// 実験条件に従い、指のモータ位置を決定して駆動命令を送信します
    /// </summary>
    private void UpdateHapticsForFinger(FingerContactInfo contactInfo, bool isFirstTouch)
    {
        if (ExperimentManager.Instance == null) return;

        // 1. VisualOnly条件: 振動を出さない
        if (!ExperimentManager.Instance.IsVibrationEnabled)
        {
            if (contactInfo.ActiveMotorId > 0 && hapticController != null)
            {
                hapticController.StopMotor(contactInfo.ActiveMotorId);
                contactInfo.ActiveMotorId = -1;
            }
            return;
        }

        // 2. Fixed条件: 接触した最初のフレームのみ計算。それ以降は位置を固定（変更しない）
        if (!isFirstTouch && !ExperimentManager.Instance.IsDynamicMode)
        {
            return;
        }

        // マッパーの取得
        IHapticMapper mapper = ExperimentManager.Instance.CurrentMapper;
        if (mapper == null) return;

        // 手掌中心と指先の座標
        Vector3 fingerPos = contactInfo.FingerTransform.position;
        Vector3 handPos = GetHandCenterPosition(contactInfo.FingerTransform);

        // モータの2Dインデックス計算 (i: 行 1~5, j: 列 1~5)
        Vector2Int motorIndex = mapper.CalculateMotorIndex(fingerPos, handPos);

        // 1次元モータID (1〜25: (i-1)*5 + j)
        int targetMotorId = (motorIndex.x - 1) * 5 + motorIndex.y;

        // モータIDが範囲外の場合は処理しない
        if (targetMotorId < 1 || targetMotorId > 25)
        {
            return;
        }

        // 3. モータ駆動・更新
        if (hapticController != null)
        {
            // Dynamicモードで以前と異なるモータに移動した場合、前のモータを停止
            if (contactInfo.ActiveMotorId > 0 && contactInfo.ActiveMotorId != targetMotorId)
            {
                hapticController.StopMotor(contactInfo.ActiveMotorId);
            }

            // 新しいモータを駆動
            hapticController.SendMotorCommand(targetMotorId, intensity: 1.0f);
        }

        contactInfo.ActiveMotorId = targetMotorId;
    }

    /// <summary>
    /// 手掌中心のワールド座標を取得します
    /// </summary>
    private Vector3 GetHandCenterPosition(Transform fingerTransform)
    {
        if (handCenterTransform != null)
        {
            return handCenterTransform.position;
        }

        // 指定がない場合は、指先の上位階層（手首や手のルートオブジェクト）を探索
        Transform current = fingerTransform.parent;
        while (current != null)
        {
            string lowerName = current.name.ToLower();
            if (lowerName.Contains("hand") || lowerName.Contains("palm") || lowerName.Contains("wrist"))
            {
                return current.position;
            }
            current = current.parent;
        }

        // 見つからない場合は指先の少し後ろを手掌中心としてフォールバック
        return fingerTransform.position - fingerTransform.forward * 0.08f;
    }

    /// <summary>
    /// 接触コライダーが指先（または仮想手）か判定します
    /// </summary>
    private bool IsValidFinger(Collider col)
    {
        // レイヤーマスク判定
        if ((contactLayer.value & (1 << col.gameObject.layer)) == 0)
        {
            return false;
        }

        // タグが指定されていればタグ判定
        if (!string.IsNullOrEmpty(fingerTag))
        {
            return col.CompareTag(fingerTag);
        }

        // デフォルト: 名前による緩やかな判定（Tip, Finger, Hand, Bone等）
        string objName = col.name.ToLower();
        return objName.Contains("tip") || objName.Contains("finger") || objName.Contains("hand");
    }

    /// <summary>
    /// 実験条件切り替え時のハンドラ
    /// </summary>
    private void HandleConditionChanged(ExperimentCondition newCondition)
    {
        if (showDebugLog)
        {
            Debug.Log($"<color=#32CD32>[HapticObjectInteraction]</color> 条件変更を検知: {newCondition}");
        }

        // VisualOnlyに切り替わった場合は全モータ停止
        if (newCondition == ExperimentCondition.Condition5_VisualOnly)
        {
            StopAllContacts();
            return;
        }

        // 接触中の指がある場合、新条件でモータ駆動を再計算
        foreach (var kvp in activeContacts)
        {
            UpdateHapticsForFinger(kvp.Value, isFirstTouch: true);
        }
    }

    /// <summary>
    /// 全ての接触指のモータを停止します
    /// </summary>
    private void StopAllContacts()
    {
        if (hapticController != null)
        {
            hapticController.StopAllMotors();
        }

        foreach (var kvp in activeContacts)
        {
            kvp.Value.ActiveMotorId = -1;
        }
    }
}
