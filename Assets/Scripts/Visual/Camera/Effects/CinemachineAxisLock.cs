using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 강제로 특정 축(여기서는 Y축)의 카메라 이동을 고정하는 확장 컴포넌트입니다.
/// CameraManager에서 RoomData의 lockCameraY가 true일 때 활성화됩니다.
/// </summary>
[SaveDuringPlay]
[AddComponentMenu("Mado/Camera/Cinemachine Axis Lock")]
[ExecuteAlways]
public class CinemachineAxisLock : CinemachineExtension
{
    [Tooltip("Y축을 고정할지 여부")]
    public bool lockY = false;

    [Tooltip("체크 시 룸 진입 순간의 카메라 Y축을 자동으로 기준점으로 잡습니다.")]
    public bool useCurrentYAsLock = true;

    [Tooltip("고정할 Y축의 월드 좌표 (useCurrentYAsLock이 false일 때 사용)")]
    public float lockedYPosition = 0f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        // Body 대신 Finalize(모든 연산 및 Confiner 처리가 끝난 후) 단계에서 개입
        if (lockY && stage == CinemachineCore.Stage.Finalize)
        {
            // 1. RawPosition 강제 고정
            Vector3 pos = state.RawPosition;
            pos.y = lockedYPosition;
            state.RawPosition = pos;

            // 2. 혹시 모를 보정값(Confiner 등)의 Y축 이동도 차단
            Vector3 correction = state.PositionCorrection;
            correction.y = 0f;
            state.PositionCorrection = correction;
        }
    }
}
