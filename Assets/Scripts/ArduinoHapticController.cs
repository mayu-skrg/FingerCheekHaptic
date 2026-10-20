using System;
using UnityEngine;

/// <summary>
/// Arduino等のマイコンへシリアル通信で振動モータ駆動信号を送信するコントローラー
/// </summary>
public class ArduinoHapticController : MonoBehaviour
{
    public static ArduinoHapticController Instance { get; private set; }

    [Header("シリアルポート設定")]
    [Tooltip("接続先のCOMポート名（例: COM3）")]
    [SerializeField] private string portName = "COM3";

    [Tooltip("ボーレート")]
    [SerializeField] private int baudRate = 115200;

    [Tooltip("シリアル通信をシミュレーション（実機未接続でもエラーを出さない）")]
    [SerializeField] private bool simulationMode = true;

    [Header("デバッグ")]
    [Tooltip("送信コマンドをConsoleに表示するか")]
    [SerializeField] private bool showDebugLog = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 指定したモータIDに振動駆動命令を送信します
    /// </summary>
    /// <param name="motorId">駆動するモータID (1〜25)</param>
    /// <param name="intensity">振動の強さ (0.0〜1.0)</param>
    public virtual void SendMotorCommand(int motorId, float intensity = 1.0f)
    {
        if (showDebugLog)
        {
            Debug.Log($"<color=#FFA500>[ArduinoHapticController]</color> モータ駆動送信: ID=<b>{motorId}</b>, 強度={intensity:F2}");
        }

        if (simulationMode) return;

        // 【実機通信用】シリアルポートへのバイト/文字列送信例
        // string command = $"M,{motorId},{(int)(intensity * 255)}\n";
        // serialPort.Write(command);
    }

    /// <summary>
    /// 特定のモータの振動を停止します
    /// </summary>
    /// <param name="motorId">停止するモータID (1〜25)</param>
    public virtual void StopMotor(int motorId)
    {
        if (showDebugLog)
        {
            Debug.Log($"<color=#FFA500>[ArduinoHapticController]</color> モータ停止送信: ID=<b>{motorId}</b>");
        }

        if (simulationMode) return;

        // string command = $"S,{motorId}\n";
        // serialPort.Write(command);
    }

    /// <summary>
    /// 全てのモータの振動を即時停止します
    /// </summary>
    public virtual void StopAllMotors()
    {
        if (showDebugLog)
        {
            Debug.Log("<color=#FFA500>[ArduinoHapticController]</color> 全モータ停止送信");
        }

        if (simulationMode) return;

        // string command = "STOP_ALL\n";
        // serialPort.Write(command);
    }
}
