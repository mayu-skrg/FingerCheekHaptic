using System;
using UnityEngine;

/// <summary>
/// 実験条件の定義
/// </summary>
public enum ExperimentCondition
{
    /// <summary>Condition 1: Method A × Dynamic (接触中リアルタイム更新)</summary>
    Condition1_MethodA_Dynamic = 1,

    /// <summary>Condition 2: Method A × Fixed (接触した瞬間の位置で固定)</summary>
    Condition2_MethodA_Fixed = 2,

    /// <summary>Condition 3: Method B × Dynamic (接触中リアルタイム更新)</summary>
    Condition3_MethodB_Dynamic = 3,

    /// <summary>Condition 4: Method B × Fixed (接触した瞬間の位置で固定)</summary>
    Condition4_MethodB_Fixed = 4,

    /// <summary>Condition 5: Visual Only (振動刺激なし)</summary>
    Condition5_VisualOnly = 5
}

/// <summary>
/// VR触覚リマッピング実験の実験条件管理マネージャー
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance { get; private set; }

    [Header("現在の実験条件")]
    [Tooltip("インスペクターから直接選択可能。実行中のキー入力(1~5)でも切り替わります。")]
    [SerializeField] private ExperimentCondition currentCondition = ExperimentCondition.Condition1_MethodA_Dynamic;

    [Header("マッパー参照")]
    [Tooltip("Method A のマッパーコンポーネント")]
    [SerializeField] private MethodAHapticMapper methodAMapper;

    [Tooltip("Method B のマッパーコンポーネント")]
    [SerializeField] private MethodBHapticMapper methodBMapper;

    /// <summary>条件変更時のイベント通知</summary>
    public event Action<ExperimentCondition> OnConditionChanged;

    /// <summary>現在選択されている実験条件</summary>
    public ExperimentCondition CurrentCondition => currentCondition;

    /// <summary>
    /// 現在の条件に応じたマッパー（VisualOnlyの場合はnull）
    /// </summary>
    public IHapticMapper CurrentMapper
    {
        get
        {
            return currentCondition switch
            {
                ExperimentCondition.Condition1_MethodA_Dynamic or ExperimentCondition.Condition2_MethodA_Fixed => methodAMapper,
                ExperimentCondition.Condition3_MethodB_Dynamic or ExperimentCondition.Condition4_MethodB_Fixed => methodBMapper,
                _ => null
            };
        }
    }

    /// <summary>
    /// リアルタイム更新（Dynamic）モードかどうか
    /// </summary>
    public bool IsDynamicMode
    {
        get
        {
            return currentCondition == ExperimentCondition.Condition1_MethodA_Dynamic ||
                   currentCondition == ExperimentCondition.Condition3_MethodB_Dynamic;
        }
    }

    /// <summary>
    /// 振動フィードバックが有効かどうか（VisualOnlyの場合はfalse）
    /// </summary>
    public bool IsVibrationEnabled => currentCondition != ExperimentCondition.Condition5_VisualOnly;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // マッパーが未アサインの場合は自動取得を試行
        if (methodAMapper == null) methodAMapper = GetComponent<MethodAHapticMapper>();
        if (methodBMapper == null) methodBMapper = GetComponent<MethodBHapticMapper>();
    }

    private void Start()
    {
        // 起動時の初期条件をログ出力
        LogCurrentCondition();
    }

    private void Update()
    {
        // キーボード数字キー(1〜5)による条件切り替え
        HandleKeyInput();
    }

    /// <summary>
    /// キーボード(1〜5)の入力を検知して条件を切り替えます
    /// </summary>
    private void HandleKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SetCondition(ExperimentCondition.Condition1_MethodA_Dynamic);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SetCondition(ExperimentCondition.Condition2_MethodA_Fixed);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SetCondition(ExperimentCondition.Condition3_MethodB_Dynamic);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            SetCondition(ExperimentCondition.Condition4_MethodB_Fixed);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            SetCondition(ExperimentCondition.Condition5_VisualOnly);
        }
    }

    /// <summary>
    /// 実験条件を変更し、コンソールに通知ログを出力します
    /// </summary>
    /// <param name="newCondition">新しい実験条件</param>
    public void SetCondition(ExperimentCondition newCondition)
    {
        if (currentCondition == newCondition && Application.isPlaying)
        {
            return;
        }

        currentCondition = newCondition;
        LogCurrentCondition();
        OnConditionChanged?.Invoke(currentCondition);
    }

    /// <summary>
    /// 現在の条件をConsoleに出力
    /// </summary>
    private void LogCurrentCondition()
    {
        string detail = currentCondition switch
        {
            ExperimentCondition.Condition1_MethodA_Dynamic => "[Condition 1] Method A × Dynamic (リアルタイム更新)",
            ExperimentCondition.Condition2_MethodA_Fixed => "[Condition 2] Method A × Fixed (接触開始時固定)",
            ExperimentCondition.Condition3_MethodB_Dynamic => "[Condition 3] Method B × Dynamic (リアルタイム更新)",
            ExperimentCondition.Condition4_MethodB_Fixed => "[Condition 4] Method B × Fixed (接触開始時固定)",
            ExperimentCondition.Condition5_VisualOnly => "[Condition 5] Visual Only (振動なし)",
            _ => currentCondition.ToString()
        };

        Debug.Log($"<color=#00FFFF><b>[ExperimentManager]</b> 実験条件が切り替わりました: <b>{detail}</b></color>");
    }

#if UNITY_EDITOR
    private ExperimentCondition lastInspectorCondition;

    private void OnValidate()
    {
        // エディタのプレイ中にInspector上で直接Enum値を変更した場合の検知
        if (Application.isPlaying && currentCondition != lastInspectorCondition)
        {
            lastInspectorCondition = currentCondition;
            LogCurrentCondition();
            OnConditionChanged?.Invoke(currentCondition);
        }
    }
#endif
}
