using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Meta Questのハンドトラッキング（OVRSkeleton）から実行時に動的生成される
/// 手掌（手首/掌）および5指先端（Tip）ボーンを自動検出し、
/// MethodAHapticMapper および HapticObjectInteraction に自動設定するプロバイダー
/// </summary>
public class MetaHandProvider : MonoBehaviour
{
    [Header("Meta ハンド設定")]
    [Tooltip("参照するOVRSkeleton（空欄の場合は同オブジェクトまたはシーン内から自動検索）")]
    [SerializeField] private OVRSkeleton ovrSkeleton;

    [Tooltip("右手か左手か")]
    [SerializeField] private HandSide handSide = HandSide.Right;

    [Header("自動連携先（未指定時はシーン内から自動取得）")]
    [SerializeField] private MethodAHapticMapper hapticMapper;
    [SerializeField] private HapticObjectInteraction hapticObjectInteraction;

    [Header("検出されたボーン（確認用・自動更新）")]
    [SerializeField] private Transform handRootTransform;
    [SerializeField] private Transform thumbTip;
    [SerializeField] private Transform indexTip;
    [SerializeField] private Transform middleTip;
    [SerializeField] private Transform ringTip;
    [SerializeField] private Transform pinkyTip;

    [Header("デバッグ")]
    [SerializeField] private bool showDebugLog = true;

    /// <summary>手掌（手のひら/手首）のTransform</summary>
    public Transform HandRootTransform => handRootTransform;

    /// <summary>5本の指先Transform（親指〜小指）</summary>
    public List<Transform> FingerTipTransforms => new List<Transform> { thumbTip, indexTip, middleTip, ringTip, pinkyTip };

    private void Awake()
    {
        if (ovrSkeleton == null)
        {
            ovrSkeleton = GetComponent<OVRSkeleton>();
        }

        if (hapticMapper == null)
        {
            hapticMapper = FindFirstObjectByType<MethodAHapticMapper>();
        }

        if (hapticObjectInteraction == null)
        {
            hapticObjectInteraction = FindFirstObjectByType<HapticObjectInteraction>();
        }
    }

    private void Start()
    {
        StartCoroutine(WaitForSkeletonInitialization());
    }

    /// <summary>
    /// OVRSkeletonの初期化が完了するのを待機し、ボーンを取得します
    /// </summary>
    private IEnumerator WaitForSkeletonInitialization()
    {
        // シーン内からOVRSkeletonの自動探索（未設定時）
        if (ovrSkeleton == null)
        {
            var skeletons = FindObjectsByType<OVRSkeleton>(FindObjectsSortMode.None);
            foreach (var skel in skeletons)
            {
                var skelType = skel.GetSkeletonType();
                bool isRight = (skelType == OVRSkeleton.SkeletonType.HandRight || skelType == OVRSkeleton.SkeletonType.XRHandRight);
                bool isLeft = (skelType == OVRSkeleton.SkeletonType.HandLeft || skelType == OVRSkeleton.SkeletonType.XRHandLeft);

                if ((handSide == HandSide.Right && isRight) || (handSide == HandSide.Left && isLeft))
                {
                    ovrSkeleton = skel;
                    break;
                }
            }

            // 見つからなければ最初のものをフォールバック
            if (ovrSkeleton == null && skeletons.Length > 0)
            {
                ovrSkeleton = skeletons[0];
            }
        }

        if (ovrSkeleton == null)
        {
            Debug.LogWarning("[MetaHandProvider] シーン内に OVRSkeleton が見つかりませんでした。");
            yield break;
        }

        // OVRSkeletonの初期化完了を待機
        while (!ovrSkeleton.IsInitialized || ovrSkeleton.Bones == null || ovrSkeleton.Bones.Count == 0)
        {
            yield return null;
        }

        // ボーンの抽出
        ExtractHandBones();

        // マッパー・インタラクションスクリプトへの自動バインド
        BindToHapticComponents();
    }

    /// <summary>
    /// OVRSkeleton.Bones から手首/掌および指先ボーンを抽出
    /// </summary>
    private void ExtractHandBones()
    {
        foreach (var bone in ovrSkeleton.Bones)
        {
            if (bone.Transform == null) continue;

            // 手首 / 手のひら
            if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot ||
                bone.Id == OVRSkeleton.BoneId.Hand_Start ||
                bone.Id == OVRSkeleton.BoneId.XRHand_Wrist ||
                bone.Id == OVRSkeleton.BoneId.XRHand_Palm)
            {
                if (handRootTransform == null || bone.Id == OVRSkeleton.BoneId.Hand_WristRoot || bone.Id == OVRSkeleton.BoneId.XRHand_Palm)
                {
                    handRootTransform = bone.Transform;
                }
            }

            // 親指先端
            if (bone.Id == OVRSkeleton.BoneId.Hand_ThumbTip || bone.Id == OVRSkeleton.BoneId.XRHand_ThumbTip)
            {
                thumbTip = bone.Transform;
            }
            // 人差し指先端
            else if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip || bone.Id == OVRSkeleton.BoneId.XRHand_IndexTip)
            {
                indexTip = bone.Transform;
            }
            // 中指先端
            else if (bone.Id == OVRSkeleton.BoneId.Hand_MiddleTip || bone.Id == OVRSkeleton.BoneId.XRHand_MiddleTip)
            {
                middleTip = bone.Transform;
            }
            // 薬指先端
            else if (bone.Id == OVRSkeleton.BoneId.Hand_RingTip || bone.Id == OVRSkeleton.BoneId.XRHand_RingTip)
            {
                ringTip = bone.Transform;
            }
            // 小指先端
            else if (bone.Id == OVRSkeleton.BoneId.Hand_PinkyTip || bone.Id == OVRSkeleton.BoneId.XRHand_LittleTip)
            {
                pinkyTip = bone.Transform;
            }
        }

        // 手掌が未取得ならSkeletonのTransformを使用
        if (handRootTransform == null)
        {
            handRootTransform = ovrSkeleton.transform;
        }

        if (showDebugLog)
        {
            Debug.Log($"<color=#1E90FF><b>[MetaHandProvider]</b> 手のボーン自動取得完了 ({handSide}):</color>\n" +
                      $"手掌/手首: {handRootTransform.name}\n" +
                      $"親指Tip: {thumbTip?.name}, 人差Tip: {indexTip?.name}, 中指Tip: {middleTip?.name}, " +
                      $"薬指Tip: {ringTip?.name}, 小指Tip: {pinkyTip?.name}");
        }
    }

    /// <summary>
    /// 取得したボーン情報を触覚マッパー等のコンポーネントに注入します
    /// </summary>
    private void BindToHapticComponents()
    {
        // 1. MethodAHapticMapper への設定
        if (hapticMapper != null)
        {
            hapticMapper.SetHandSide(handSide);
            hapticMapper.SetHandTransform(handRootTransform);

            var tips = new List<Transform>();
            if (thumbTip != null) tips.Add(thumbTip);
            if (indexTip != null) tips.Add(indexTip);
            if (middleTip != null) tips.Add(middleTip);
            if (ringTip != null) tips.Add(ringTip);
            if (pinkyTip != null) tips.Add(pinkyTip);

            hapticMapper.SetFingerTips(tips);

            if (showDebugLog)
            {
                Debug.Log("<color=#1E90FF>[MetaHandProvider]</color> MethodAHapticMapper に手掌Transformと5指先リストをバインドしました。");
            }
        }
    }
}
