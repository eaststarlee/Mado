# 애니메이션 아키텍처 리팩토링 작업 목록

- `[x]` **Phase 1: 기반 시스템 구축 (Foundation)**
  - `[x]` `ICharacterAnimator.cs` 인터페이스 작성
  - `[x]` `UnityAnimatorAdapter.cs` 컴포넌트 작성
  - `[x]` `CharacterSpriteAnimator.cs`에 인터페이스 상속 적용
- `[/]` **Phase 2: 핵심 개체 연동 (Integration)**
  - `[x]` `EnemyEntity` 및 Enemy Modules 애니메이터 참조를 `ICharacterAnimator`로 교체
  - `[x]` `PetController` 및 `PetState` 자동 매핑 적용
  - `[x]` `PlayerController` 및 `PlayerState` 의존성 변경
- `[x]` **Phase 3: 테스트 및 검증 (Validation)**
  - `[x]` 컴파일 에러 체크 및 안정화
