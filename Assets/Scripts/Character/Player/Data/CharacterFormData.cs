using UnityEngine;

// 코어 개발자나 레벨 디자이너가 아닌 팀원이 수정할 수 없는 값들을 숨깁니다.
//Create a new playerData object by right clicking in the Project Menu then Create/Player/Player Data and drag onto the player
[CreateAssetMenu(fileName = "NewCharacterForm", menuName = "Player/Character Form Data")]
public class CharacterFormData : ScriptableObject
{
	[Header("Form Identity")]
	public FormType formType = FormType.Normal;
	public Sprite formSprite; // 폼별 기본 스프라이트 (선택)
	public Mado.AnimationSystem.CharacterAnimationSet animationSet; // [New] 폼별 애니메이션 세트
	
	[Header("공격 프로필")]
	[Tooltip("폼별 공격 데이터 (Normal/Up/Down Attack + 보정값)")]
	public FormAttackProfile attackProfile;

	public GravityData gravity = new GravityData();
	public RunData run = new RunData();
	public JumpData jump = new JumpData();
	public WallData wall = new WallData();
	public AbilityData ability = new AbilityData();

	public AssistData assist = new AssistData();
    public ReactionData reaction = new ReactionData();
    public ParryData parry = new ParryData(); // [New] 패링 데이터
    public SkillResourceData skillResource = new SkillResourceData(); // [New] 스킬 게이지 데이터

    [System.Serializable]
    public class SkillResourceData
    {
        [Header("Skill Gauge (Soul)")]
        [Tooltip("게임 시작 시 발동 가능한 기본 최대 스킬 게이지(MP)량")]
        public int baseMaxGauge = 100;
        
        [Tooltip("일반 공격 명중 시 차오르는 게이지량")]
        public int gainOnAttack = 5;
        
        [Tooltip("패링 성공 시 차오르는 게이지량")]
        public int gainOnParry = 10;
    }

    [System.Serializable]
    public class ParryData
    {
        [Header("Parry Timing (Seconds)")]
        [Tooltip("A키를 누른 직후 패링 판정이 발생하기까지의 선딜레이 (보통 0)")]
        public float startupTime = 0.0f;
        [Tooltip("실제로 적의 공격을 튕겨낼 수 있는 유효 시간 (예: 0.15초)")]
        public float activeTime = 0.15f;
        [Tooltip("패링이 끝난 후 다시 행동할 수 있을 때까지의 후딜레이 (예: 0.05초)")]
        public float recoveryTime = 0.05f;
        [Tooltip("패링 시도 후 다음 패링을 다시 사용할 수 있는 쿨다운 (연타 방지)")]
        public float cooldown = 0.3f;

        [Header("Parry Effect")]
        [Tooltip("패링에 성공했을 때 다단 히트를 막기 위해 부여되는 짧은 무적 시간")]
        public float successInvincibilityDuration = 0.2f;
        [Tooltip("패링 성공 시 시전되는 역경직(HitStop) 시간")]
        public float successHitStopDuration = 0.1f;
        [Tooltip("패링 성공 시 화면 흔들림 강도")]
        public float successScreenShakePower = 0.3f;

        [Header("Parry Knockback")]
        [Tooltip("패링 성공 시 뒤로 밀려나는 힘 (x: 수평 밀림, y: 수직 뜸)")]
        public Vector2 successKnockbackForce = new Vector2(5f, 0f);
        [Tooltip("패링 넉백이 지속되는(조작 불가) 시간")]
        public float successKnockbackDuration = 0.1f;
    }

    [System.Serializable]
    public class ReactionData
    {
        [Header("Reaction")]
        // public float knockbackDuration = 0.2f; // Removed: Controlled by Source (DamageDealer)
        // public Vector2 knockbackSpeed; // Removed: Controlled by Source (DamageDealer)
        public float flashDuration = 0.1f; // 깜빡임 주기
        public Color flashColor = new Color(1f, 1f, 1f, 0.5f); // 깜빡일 때 색상
        
        [Header("Impact Feel")]
        public float hitStopDuration = 0.1f;    // 피격 시 시간 정지 (0.1초 정도)
        public float screenShakePower = 0.5f;   // 화면 흔들림 강도 (TargetOffset 변경량)
        public float screenShakeDuration = 0.2f;// 화면 흔들림 시간
    }

	[System.Serializable]
	public class GravityData
	{
		[Header("Gravity")]
		[HideInInspector] public float strength; //점프 높이와 점프 도달 시간에 맞춰 계산된 중력값.
		[HideInInspector] public float scale; // 유니티의 기본 중력값 대비 계산된 Rigidbody의 중력 스케일.
		[Space(5)]
		public float fallGravityMult = 1.9f; // 기본 하강 시 중력 배율.
		public float maxFallSpeed = 25f; // 최대 하강 속도.
	}

	[System.Serializable]
	public class RunData
	{
		[Header("Run")]
		public float maxSpeed; // 목표 최고 속도.
		public float acceleration; //최고 속도까지 도달하는 가속도.
		[HideInInspector] public float accelAmount; //실제 가해지는 힘.
		public float decceleration; // 감속도.
		[HideInInspector] public float deccelAmount; // 실제 가해지는 힘.
		public float turnDeceleration; // 방향 전환 시 감속도
		[HideInInspector] public float turnDeccelAmount; // 실제 감속 힘
		[Space(5)]
		[Range(0f, 1)] public float accelInAir; // 공중에서의 가속도 배율.
		[Range(0f, 1)] public float deccelInAir;
		[Space(5)]
		public bool doConserveMomentum;
	}

	[System.Serializable]
	public class JumpData
	{
		[Header("Basic Jump")]
		public float jumpHeight = 3.5f; // 점프 높이.
		public float timeToApex = 0.45f; // 점프 최고점 도달 시간. (0.4 -> 0.45로 증가시켜 체공시간 확보)
		[HideInInspector] public float jumpForce; // 실제 가해지는 점프 힘.

		[Space(5)]
		[Header("Double Jump")]
		public bool hasDoubleJumpAbility; // 더블 점프 능력 보유 여부
		public float doubleJumpForce = 20f; // 더블 점프 시 가해지는 힘
		[Tooltip("더블 점프 발동 전 선딜레이 시간 (초). 0.08~0.1 권장")]
		public float doubleJumpAnticipationDelay = 0.08f;

		[Space(10)]
		[Header("Sprint Jump")]
		public float sprintJumpForce = 28f; // 스프린트 점프 시 가해지는 수직 힘.
		public float sprintJumpLandCooldown = 0.2f; // 착지 후 다음 스프린트 점프까지의 대기시간.
		public float sprintJumpPrepareTime = 0.1f; // 스프린트 점프 전 준비 시간(선딜레이).
		public float sprintJumpAirControlLockDuration = 0.5f; // 스프린트 점프 후 공중 컨트롤 잠금 시간.

		[Header("Jump Modifiers")]
		public float jumpCutGravityMult = 3f; // 점프 버튼을 짧게 눌렀을 때의 중력 배율 (상승을 빠르게 멈춤).
		[Range(0f, 1)] public float jumpHangGravityMult = 0.5f; // 점프 최고점 근처에서의 중력 배율 (0.9 -> 0.5로 낮춰 체공감 증가).
		public float jumpHangTimeThreshold = 1.0f; // 점프 최고점으로 간주할 속도 임계값.
		[Space(0.5f)]
		public float jumpHangAccelerationMult = 1.1f; // 최고점 근처에서의 공중 가속도 배율.
		public float jumpHangMaxSpeedMult = 1.1f; // 최고점 근처에서의 최대 속도 배율.
	}

	[System.Serializable]
	public class WallData
	{
		[Header("Wall Mechanics")]
		public float slideSpeed; // 벽 슬라이딩 속도.
		public float slideAccel; // 벽 슬라이딩 가속도.
		public float stickTime; // 벽에 붙어있을 시간.
		public float jumpCooldown; // 벽점프 재사용 대기시간
		public float coyoteTime; // 벽에서 떨어진 후에도 벽점프 가능한 시간

		[Space(10)]
		[Header("Neutral Wall Jump")]
		public Vector2 neutralWallJumpForce; // 방향키 입력 없이 점프 시 가해지는 힘 (예: 3칸)
		public float neutralWallJumpTime; // 조작 불가 시간
		[Range(0f, 1f)] public float neutralWallJumpCutMultiplier; // 점프 키를 짧게 눌렀을 때 속도 감소 배율
		public float wallJumpUpwardPopForce; // 점프 키를 길게 눌렀을 때 추가되는 상승 힘

		[Space(10)]
		[Header("Wall Climb Jump")]
		public float climbForce; // 수직 벽 타기 상승 힘
		public float climbDuration; // 벽 타기 지속 시간
		public float climbStopSmoothingDuration = 0.15f; // 벽 타기 종료 시 감속 구간 시간

		[Space(10)]
		[Header("Ledge Climb (Mantle)")]
		public float ledgeClimbSpeed; // ㄱ자 이동 속도
		public float ledgeScanHeight; // 머리 위에서 스캔할 높이
		public float ledgeLandOffset; // 벽 안쪽으로 착지할 깊이
		public float ledgeMaxClimbHeight; // 최대 기어오르기 높이 (무한 등반 방지)
	}

	[System.Serializable]
	public class AbilityData
	{
		[Header("Dash")]
		public float dashSpeed;
		public float dashTime;
		public int amountOfDashes;
		public float dashCooldown; // 연속 대쉬 방지용 쿨타임
		public float dashEndYMultiplier; // 대시가 끝날 때 y축 속도에 곱해질 값

        [Space(20)]
        [Header("Sprint")] // [SPRINT_DISABLED] - 인스펙터 노출은 유지하되 HideInInspector로 숨김
        [HideInInspector] public bool canDashToSprint;
        [HideInInspector] public float sprintSpeed;
        [HideInInspector] public float sprintStopDeceleration; // 스프린트 정지 시 감속도
        [HideInInspector] public float sprintStopDeccelAmount; // 실제 감속 힘 (원래도 HideInInspector)
        [Space(5)]
        [HideInInspector] public float sprintTurnDuration; // 스프린트 중 방향 전환에 걸리는 시간
        [HideInInspector] public float sprintTurnDeceleration; // 스프린트 방향 전환 시 감속도
        [HideInInspector] public float sprintTurnDeccelAmount; // 실제 감속 힘 (원래도 HideInInspector)
        [HideInInspector] public float sprintTurnCooldown; // 스프린트 턴 재사용 대기시간

        // [SPRINT_DISABLED] Sprint Turn Jump Restrictions
        [HideInInspector] public float sprintTurnJumpSpeedThreshold;
        [HideInInspector] public float sprintTurnJumpLockDuration;






		[Space(20)]
		[Header("Gliding")]
		public float glideFallSpeed; // 활공 시 최대 하강 속도
		public float glideSmoothTime; // 활공 속도 전환 부드러움
		public float glideHorizontalMultiplier; // 활공 중 좌우 이동 속도 감속
		public float glideAccelerationMultiplier; // 활공 중 좌우 가속도 배율 (방향 전환 빠릿함 유지)
		public float glideFallThreshold; // 낙하 감지 임계값 (짧은 점프는 활공 안 됨)
	}

	[System.Serializable]
	public class AssistData
	{
		[Header("Assists")]
		[Range(0.01f, 0.5f)] public float coyoteTime; // 절벽에서 떨어진 후에도 점프할 수 있는 시간.
		[Range(0.01f, 0.5f)] public float jumpInputBufferTime; // 착지 직전에 점프를 미리 입력할 수 있는 시간.
	}


	// 인스펙터 값이 변경될 때마다 유니티 에디터에서 자동으로 호출됨
	private void OnValidate()
	{
		// 중력 강도 계산: gravity = 2 * jumpHeight / timeToJumpApex^2
		if (jump.timeToApex > 0)
		{
			gravity.strength = -(2 * jump.jumpHeight) / (jump.timeToApex * jump.timeToApex);
		}

		// 유니티 기본 중력 대비 Rigidbody의 중력 스케일 계산
		if (Physics2D.gravity.y != 0)
		{
			gravity.scale = gravity.strength / Physics2D.gravity.y;
		}

		// 달리기 가속/감속 힘 계산 (RunData)
		if(run.maxSpeed > 0)
		{
			run.accelAmount = (50 * run.acceleration) / run.maxSpeed;
			run.deccelAmount = (50 * run.decceleration) / run.maxSpeed;
			run.turnDeccelAmount = (50 * run.turnDeceleration) / run.maxSpeed;
		}
		
		// 스프린트 가속/감속 힘 계산 (AbilityData)
		if (ability.sprintSpeed > 0)
		{
			ability.sprintStopDeccelAmount = (50 * ability.sprintStopDeceleration) / ability.sprintSpeed;
			ability.sprintTurnDeccelAmount = (50 * ability.sprintTurnDeceleration) / ability.sprintSpeed;
		}

		// 점프 힘 계산: initialJumpVelocity = gravity * timeToJumpApex
		jump.jumpForce = Mathf.Abs(gravity.strength) * jump.timeToApex;
	}
}