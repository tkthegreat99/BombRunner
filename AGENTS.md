BOOM RUNNER - CODEX DEVELOPMENT README

Plain Text Version



이 문서는 Boom Runner 프로젝트의 개발 기준을 정의하는 순수 텍스트 README입니다.





============================================================

1\. 프로젝트 개요

============================================================



프로젝트명: Boom Runner / 붐 러너



장르:

탑다운 3D 멀티플레이 캐주얼 서바이벌 난투



핵심 컨셉:

살아 움직이는 폭탄 크리처가 한 명의 플레이어를 추적한다.

타겟이 된 플레이어는 다른 플레이어에게 몸을 부딪쳐 타겟을 토스한다.

플레이어들은 폭탄을 피하고, 넘기고, 도발하고, 서로 트롤링하며 최후까지 살아남는다.



개발 방식:

1인 개발



개발 목표:

12주 개발 플랜을 기준으로 Steam 출시 가능한 상태까지 제작한다.

시작 날짜는 26.07.07.



중요한 방향:

처음부터 네트워크 기반 구조로 만든다.

싱글플레이처럼 보이는 테스트도 혼자 방에 들어간 상태로 테스트한다.

나중에 멀티플레이 대응을 위해 코드를 갈아엎는 일이 없도록 한다.





============================================================

2\. 최우선 개발 원칙

============================================================



1\. 기능 손실 금지



기존 동작을 삭제하거나 약화하지 않는다.

리팩토링을 하더라도 공개 API, 게임 규칙, 네트워크 동기화 흐름을 임의로 변경하지 않는다.

기존 코드가 지저분해 보여도 기능이 보존되는 것을 최우선으로 한다.

기능 변경이 필요하면 반드시 이유를 설명한다.





2\. 네트워크 선구축



일반 Instantiate보다 네트워크 스폰 구조를 우선 고려한다.

폭탄 AI, 타겟 변경, 라운드 상태, 맵 선택, 승리 판정은 네트워크 동기화를 전제로 설계한다.

혼자 테스트하더라도 로컬 전용 구조로 만들지 않는다.

Master Client 또는 서버 권한 주체가 어떤 판단을 하는지 명확히 한다.





3\. GC 최소화



런타임 중 반복 생성 및 파괴되는 객체는 풀링을 우선 고려한다.

LINQ, boxing, 반복 string allocation, 매 프레임 closure 생성을 피한다.

Update 루프, 네트워크 틱, 충돌 판정, 라운드 핵심 로직에서는 할당을 만들지 않는다.

AudioSource, VFX, UI 반복 요소, 캐릭터, 폭탄, 이펙트성 오브젝트는 Pool 기반을 우선한다.





4\. Coroutine 금지



Unity Coroutine은 사용하지 않는다.

IEnumerator, StartCoroutine, StopCoroutine을 새로 작성하지 않는다.

모든 비동기 흐름은 UniTask, async, await 기반으로 작성한다.





5\. private 필드 언더바 금지



private 변수나 private 프로퍼티 앞에 언더바를 붙이지 않는다.



좋은 예:

private StageManager stageManager;

private SoundManager soundManager;



나쁜 예:

private StageManager \_stageManager;

private SoundManager \_soundManager;





6\. 동적 생성 자제



SFX, VFX, 필요한 프리팹 Instantiate 정도는 허용한다.

단, 가능하면 Pooling을 우선 고려한다.



UI를 코드로 즉석 동적 생성하지 않는다.

HUD, Popup, Result UI를 런타임에서 임의로 조립하지 않는다.

맵이나 플레이 경험에 직접 영향을 주는 구조물을 검증 없이 동적 생성하지 않는다.

플레이 중 사용자 경험에 지장을 줄 수 있는 생성 코드를 작성하지 않는다.

프레임 드랍, 로딩 끊김, 예측 불가능한 배치를 유발하는 구조를 피한다.



UI는 미리 만든 프리팹, 씬 배치, 명시적인 View 클래스를 우선한다.

맵은 검증된 JSON 데이터와 승인된 블록 프리팹을 사용하고, 로딩 화면 중 통제된 방식으로 조립한다.





============================================================

3\. 확정 기술 스택

============================================================



Unity URP

Photon Fusion 또는 Unity Netcode 계열

UniTask

R3

VContainer

Cinemachine

New Input System

DOTween

Pooling

Steamworks.NET



역할 구분:



비동기 처리: UniTask

반응형 상태 및 UI: R3

DI 및 생명주기 관리: VContainer

카메라 제어: Cinemachine

입력 처리: New Input System

UI 애니메이션: DOTween

오브젝트 재사용: Pooling

Steam 연동: Steamworks.NET





============================================================

4\. 코드 스타일 규칙

============================================================



4.1 필드 네이밍



private 필드에 언더바를 붙이지 않는다.



좋은 예:



private StageManager stageManager;

private readonly CompositeDisposable disposables = new();



나쁜 예:



private StageManager \_stageManager;

private readonly CompositeDisposable \_disposables = new();





4.2 var 사용



타입이 오른쪽에서 명확하면 var를 적극 사용한다.



좋은 예:



var player = playerFactory.Create(spawnPoint);

var delay = TimeSpan.FromSeconds(0.5f);



타입이 명확하지 않거나 가독성이 떨어지면 명시 타입을 사용한다.



예:



IReadOnlyList<PlayerContext> alivePlayers = matchState.AlivePlayers;





4.3 LINQ 사용 제한



런타임 빈번 호출 코드, Update 루프, 네트워크 틱, 충돌 판정, 라운드 핵심 로직에서는 LINQ를 사용하지 않는다.



피해야 하는 예:



var target = players.Where(x => x.IsAlive).OrderBy(x => x.Distance).FirstOrDefault();



권장 예:



PlayerContext target = null;

var bestDistance = float.MaxValue;



for (var i = 0; i < players.Count; i++)

{

&#x20;   var player = players\[i];



&#x20;   if (!player.IsAlive)

&#x20;   {

&#x20;       continue;

&#x20;   }



&#x20;   var distance = player.Distance;



&#x20;   if (distance >= bestDistance)

&#x20;   {

&#x20;       continue;

&#x20;   }



&#x20;   bestDistance = distance;

&#x20;   target = player;

}





============================================================

5\. Unity 프로젝트 구조

============================================================



기본 폴더 구조:



Assets/

&#x20; 00\_Project/

&#x20;   Scripts/

&#x20;     App/

&#x20;     Core/

&#x20;     DI/

&#x20;     Input/

&#x20;     Camera/

&#x20;     UI/

&#x20;     Audio/

&#x20;     Network/

&#x20;     Gameplay/

&#x20;       Bomb/

&#x20;       Player/

&#x20;       Round/

&#x20;       Match/

&#x20;       Taunt/

&#x20;       LastStand/

&#x20;     MapEditor/

&#x20;     Data/

&#x20;     Pooling/

&#x20;   Scenes/

&#x20;     Init.unity

&#x20;     Lobby.unity

&#x20;     Game.unity

&#x20;     MapEditor.unity

&#x20;   Prefabs/

&#x20;   ScriptableObjects/

&#x20;   Art/

&#x20;   Audio/

&#x20;   Settings/



씬 구조:



Init:

부트스트랩, 전역 LifetimeScope, 전역 매니저 로드



Lobby:

로비, 방 생성, 빠른 입장, 맵 투표



Game:

실제 인게임



MapEditor:

UGC 맵 에디터





============================================================

6\. VContainer DI 설계

============================================================



DI는 과도하게 남발하지 않는다.

서비스, 매니저, 팩토리, 풀, 라운드 흐름 제어에 사용한다.



ProjectLifetimeScope:

게임 실행 내내 살아있는 전역 인프라를 등록한다.



등록 대상 예시:

SceneLoader

DataManager

SoundManager

UiManager

PoolService

AssetLoader

SaveService

NetworkBootstrap



GameLifetimeScope:

인게임 한 판 동안만 살아있는 서비스를 등록한다.



등록 대상 예시:

StageManager

RoundFlowService

InputService

CameraService

BombTargetService

PlayerSpawnService

MapRuntimeBuilder

MatchStateService

LastStandService



MapEditorLifetimeScope:

UGC 맵 에디터에서만 필요한 서비스를 등록한다.



등록 대상 예시:

GridEditorService

MapValidationService

MapSerializeService

MapPreviewService

MapLinkService



DI에 넣지 말 것:

단순 데이터 클래스

ScriptableObject 데이터 자체

MonoBehaviour View 전부

매 프레임 생성되는 값 객체

Inspector 연결이 더 자연스러운 단순 UI 부품





============================================================

7\. UniTask 사용 규칙

============================================================



CancellationToken을 가능한 한 사용한다.

비동기 루프나 지연 작업은 CancellationToken을 받아야 한다.

MonoBehaviour에서는 GetCancellationTokenOnDestroy를 우선 사용한다.



async void는 사용하지 않는다.

Unity 이벤트 연결 등 불가피한 경우를 제외하고 async void를 금지한다.

Fire-and-forget은 UniTaskVoid를 사용할 수 있지만 예외 처리에 주의한다.



Coroutine 대신 UniTask를 사용한다.



나쁜 예:



private IEnumerator DashCooldown()

{

&#x20;   yield return new WaitForSeconds(1f);

}



좋은 예:



private async UniTaskVoid RunDashCooldownAsync(CancellationToken cancellationToken)

{

&#x20;   await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);

}





============================================================

8\. R3 사용 규칙

============================================================



R3는 상태 변화, UI 반응, 쿨타임, 타이머 상태 변경에 사용한다.



사용 대상 예시:

대시 쿨타임 상태

타겟 플레이어 변경

폭탄 타이머 단계 변경

라운드 상태 변경

로비 준비 상태

맵 투표 상태



구독은 반드시 Dispose 처리한다.

CompositeDisposable을 사용한다.

View가 비활성화되거나 파괴될 때 구독이 남지 않도록 한다.





============================================================

9\. New Input System 설계

============================================================



Action Map 이름:

Player



Actions:

Move: Vector2

Dash: Button

Taunt: Button

Confirm: Button

Cancel: Button



플레이어 컨트롤러가 직접 InputAction을 읽지 않는다.

반드시 IInputService를 통해 추상화한다.



IInputService 기본 형태:



public interface IInputService

{

&#x20;   Vector2 Move { get; }

&#x20;   bool DashPressed { get; }

&#x20;   bool TauntPressed { get; }

}



PC, 게임패드, 모바일 조이스틱 대응은 InputService 내부에서 흡수한다.





============================================================

10\. Cinemachine 카메라 설계

============================================================



카메라는 Cinemachine을 사용한다.



기본 원칙:

탑다운 3D 고정 각도

플레이어 또는 타겟 그룹 Follow

맵 전체를 보기 쉬운 거리 유지

폭탄 폭주 시 약한 흔들림

폭발 시 Cinemachine Impulse

라스트 스탠드 시 전장 줌아웃

최후 생존자 판정 직전 승리자 줌인



카메라 제어는 CameraService로 모은다.





============================================================

11\. DOTween UI 애니메이션 규칙

============================================================



UI 애니메이션은 DOTween을 사용한다.



사용 대상:

팝업 열림 및 닫힘

버튼 눌림 효과

대시 쿨타임 UI

타겟 락온 화살표 흔들림

면역 5초 카운트다운 게이지

폭탄 위험도 경고 UI

라스트 스탠드 흑백 및 강조 연출 보조

결과 화면 승리 텍스트



Tween은 반드시 정리한다.

OnDisable 또는 Dispose에서 Kill한다.

반복 재사용되는 UI는 Pooling과 Tween Kill을 함께 고려한다.



기본 규칙:

UI는 코드로 즉석 생성하지 않는다.

미리 만든 프리팹 또는 씬에 배치된 View를 사용한다.

UiManager는 UI를 생성하는 역할보다 열고 닫고 상태를 전달하는 역할을 맡는다.





============================================================

12\. 핵심 게임 규칙

============================================================



12.1 라운드 시작



3초 카운트다운

필드 중앙에서 폭탄 크리처 등장

생존자 중 랜덤 1명을 타겟으로 지정

폭탄이 즉시 타겟 추적 시작





12.2 타겟 상태



타겟 플레이어는 아래 연출을 가진다.



머리 위 빨간 락온 화살표

발밑 빨간 조준 레이더

폭탄과 타겟 사이 레이저 점선 레이더





12.3 타겟 토스



타겟 플레이어가 일반 플레이어와 충돌하면 타겟이 상대에게 이전된다.

타겟을 넘겨준 플레이어는 5초 면역 상태가 된다.

면역 중에는 바로 다시 타겟을 받을 수 없다.

이 규칙은 무한 비비기와 즉시 반환 버그를 방지한다.





12.4 도발



일반 플레이어가 폭탄 근처에 있을 때 도발 가능하다.

도발 시 코믹 모션을 실행한다.

40% 확률로 폭탄 타겟이 도발자에게 변경된다.

성공 또는 실패와 무관하게 폭탄 분노 스택은 증가한다.





12.5 폭탄 분노 스택



도발당할 때마다 폭탄은 강화된다.



몸집 증가

이동 속도 증가

최종 폭발 범위 증가



최대 속도와 최대 범위 상한이 있어야 한다.





============================================================

13\. 폭탄 타이머 및 폭주 연출

============================================================



폭탄은 내부적으로 15초 타이머를 가진다.

단, 숫자 UI는 보여주지 않는다.



1단계: 평온기

시간: 15초 \~ 10초

연출:

민트색 또는 녹색 빛

귀여운 표정

낮은 기계음



2단계: 경고기

시간: 10초 \~ 5초

연출:

주황 또는 붉은 점멸

화난 표정

경고음 빨라짐

레이더 회전 속도 증가



3단계: 초폭주기

시간: 5초 \~ 0초

연출:

핏빛 과열

0.2초 간격 맥박 스케일링

스팀 및 스파크

심장 박동 사운드



상태 enum 예시:



public enum BombTimerPhase

{

&#x20;   Calm,

&#x20;   Warning,

&#x20;   Overdrive

}





============================================================

14\. 라스트 스탠드

============================================================



라스트 스탠드는 항상 발동하지 않는다.

아래 조건일 때만 특수 연출을 실행한다.



조건:

폭탄 타이머가 0초

폭발 범위 안에 남은 생존자 전원이 포함됨

즉, 전원 몰살 위기일 때만 발동



Phase 1:

Time.timeScale = 0.05

전장 줌아웃

화면 흑백

폭탄 코어와 타겟 표시만 원색 강조

BGM 뮤트

진공 사운드



Phase 2:

구형 대미지 링 확장

링에 닿는 순서대로 캐릭터 랙돌 및 탈락 연출

시스템은 ms 단위로 충돌 순서를 기록



Phase 3:

가장 마지막에 링에 닿은 유저를 최종 승리자로 판정

해당 유저에게 카메라 줌인

Time.timeScale = 1.0 복구

대폭발 이펙트



결과 UI:

\[0.02초 차이로 최후 생존!]





============================================================

15\. UGC 맵 에디터

============================================================



맵 에디터는 2D 그리드 기반이다.

맵 데이터는 JSON 텍스트로 저장 및 공유한다.



필수 배치 요소:

플레이어 스폰 2\~8개

폭탄 스폰 1개



배치 가능 기믹:

기본 도형 블록

원웨이 토관

셔터 스위치

컨베이어 벨트



원웨이 토관:

입구와 출구를 격자에 배치한다.

에디터 상에서 선으로 링크 연결한다.

플레이어 진입 시 출구로 과속 발사한다.

폭탄 AI는 토관을 타지 않는다.

폭탄은 지상 경로 기준으로 가장 가까운 타겟을 계속 추적한다.



셔터 스위치:

바닥 압력 발판과 철문을 링크한다.

발판을 밟으면 문이 3초간 폐쇄된다.

트롤링 가능해야 한다.



컨베이어 벨트:

진행 방향 화살표 설정 가능.

플레이어를 강제 감속 또는 가속한다.



유효성 검사:

MapValidationService는 최소한 아래를 검사한다.



플레이어 스폰 개수 2\~8개인지

폭탄 스폰이 정확히 1개인지

링크 없는 파이프가 없는지

링크 없는 스위치 또는 문이 없는지

맵 경계 밖 오브젝트가 없는지

저장 가능한 JSON인지





============================================================

16\. 동적 생성 제한 기준

============================================================



Boom Runner는 플레이 중 끊김, UI 불안정, 예측 불가능한 맵 조립을 피해야 한다.



허용되는 동적 생성:

SFX AudioSource

VFX

필요한 Prefab Instantiate

네트워크 스폰 대상 Prefab

Pooling을 전제로 한 반복 사용 오브젝트



피해야 하는 동적 생성:

UI를 코드로 즉석 생성

HUD, Popup, Result UI를 런타임에서 임의 조립

맵 구조물을 검증 없이 동적 생성

플레이 중 사용자 경험에 영향을 주는 대형 구조물 생성

프레임 드랍이나 로딩 끊김을 유발할 수 있는 생성 코드



UI 작성 원칙:

미리 만든 Popup Prefab을 UiManager가 열고 닫는다.

HUD View를 씬 또는 프리팹에 구성한다.

DOTween으로 표시 및 숨김 애니메이션을 처리한다.

닫힐 때 Tween을 Kill한다.

코드에서 Button, Text, Image를 즉석 생성해 화면을 구성하지 않는다.

런타임마다 Layout을 새로 조립하지 않는다.



맵 작성 원칙:

MapValidationService를 통과한 맵만 사용한다.

승인된 블록 Prefab만 사용한다.

로딩 화면 중 조립한다.

Pooling 또는 통제된 Instantiate를 사용한다.

플레이 시작 전 안정화 완료 상태여야 한다.

플레이 중 검증되지 않은 맵 조각을 생성하지 않는다.





============================================================

17\. 네트워크 설계 기준

============================================================



기본 원칙:

처음부터 네트워크 기반

혼자 테스트해도 방을 생성해서 플레이

일반 Instantiate 지양

네트워크 스폰 사용

중요 상태는 RPC 또는 Networked State로 동기화



Master Client 또는 서버 권한 주체가 연산할 항목:

폭탄 AI 타겟 추적

첫 타겟 랜덤 선정

타겟 변경 확정

도발 성공 여부 40% 판정

폭탄 분노 스택

폭발 판정

라스트 스탠드 승리자 판정

맵 투표 결과 확정



커스텀 방:

4\~6자리 방 코드 발급

2\~8인 설정

폭탄 타이머 설정

커스텀 맵 설정

방장이 설정 관리



빠른 입장:

대기 유저 자동 매칭

핑이 좋은 유저에게 Master Client 권한 위임

매칭 후 랜덤 3개 맵 후보 제시

유저 다수결 투표로 맵 결정

Classic / Crazy 채널 분리



리전:

Asia

NA

EU



로비에는 핑 상태를 신호등 컬러로 표시한다.





============================================================

18\. Pooling 기준

============================================================



반복 생성 및 파괴되는 것은 풀링한다.



풀링 대상:

AudioSource

VFX

UI Popup

Floating Text

Lock-on Indicator

Radar UI

Explosion Ring

Player Character

Bomb Creature

Ghost Character

Map Runtime Blocks



풀링 대상 View는 재사용 시 반드시 상태를 초기화한다.



인터페이스 예시:



public interface IPoolable

{

&#x20;   void OnSpawned();

&#x20;   void OnDespawned();

}





============================================================

19\. SoundManager 기준

============================================================



AudioSource를 매번 생성하지 않는다.

풀에서 꺼내 사용한다.



필요 사운드:

폭탄 단계별 기계음

경고음

심장 박동

폭발음

대시음

타겟 토스음

도발음

UI 클릭음

결과 화면 효과음





============================================================

20\. UI Manager 기준

============================================================



UI는 스택 구조로 관리한다.



UI 분류:

HUD

Popup

Modal

Result

Loading

Toast



UiManager의 역할:

UI를 직접 새로 조립하지 않는다.

미리 준비된 프리팹 또는 씬 View를 열고 닫는다.

열림, 닫힘, 표시 상태, 입력 차단, 결과 화면 전환을 관리한다.





============================================================

21\. 12주 개발 마스터 플랜

============================================================



1주차:

부트스트랩 및 네트워크 코어 매니저 세팅



작업:

Unity URP 프로젝트 생성

폴더 구조 정립

Init 씬 생성

네트워크 패키지 임포트

GameManager 또는 Bootstrapper 구조 작성

SceneLoader 구현

DataManager 구현

SoundManager 풀링 구현

UiManager 스택 구조 설계

DontDestroyOnLoad 검증

코드 네이밍 규칙 검수



2주차:

네트워크 입력 시스템 및 플레이어 평면 무빙



작업:

StageManager 구현

New Input System 연동

PC 및 모바일 입력 추상화

네트워크 스폰으로 캐릭터 생성

Y축 점프 없는 2D 평면 이동

UniTask 기반 Dash 및 Cooldown

대시 SFX 및 UI 연동

혼자 방 생성 후 네트워크 이동 테스트



3주차:

네트워크 폭탄 AI 및 타겟 토스 시스템



작업:

Master Client 기준 폭탄 AI

타겟 추적

레이저 점선 레이더 동기화

15초 폭탄 폭주 단계 구현

타겟 토스 RPC

5초 배달 면역

도발 40% 타겟 변경

폭탄 분노 스택 동기화



4주차:

라스트 스탠드 특수 연출



작업:

폭발 범위 판정

전원 몰살 위기 조건 체크

Time.timeScale 0.05 슬로우 모션

흑백 화면 전환

ms 단위 최종 승리자 판정

결과 UI

탈락자 유령 상태



5주차:

2D 그리드 기반 맵 에디터



작업:

그리드 UI

마우스 좌표를 격자 좌표로 변환

기본 도형 배치

RGB 색상 및 Scale 편집

스폰 유효성 검사

JSON 저장 및 불러오기



6주차:

핵심 기믹 및 런타임 자동 조립



작업:

원웨이 토관

셔터 스위치

컨베이어 벨트

JSON 기반 런타임 맵 조립

트롤링 맵 예외 처리



7주차:

실시간 맵 데이터 동기화



작업:

방 코드 로비

10KB 미만 커스텀 맵 JSON 송수신

0.5초 수준 백그라운드 수신 목표

로딩 중 자동 맵 조립

멀티 기기 검증



8주차:

빠른 입장, 채널, 리전



작업:

Quick Match

Classic 및 Crazy 채널 분리

맵 후보 3개 투표

Asia, NA, EU 리전

핑 신호등 UI



9주차:

예외 처리 및 메모리 최적화



작업:

Host Migration

커스텀 맵 캐시 정리

최대 30개 FIFO

Unity Profiler 메모리 전수조사

UniTask cancellation 점검

GC 유발 코드 제거



10주차:

Steamworks SDK 및 상점 페이지



작업:

Steamworks.NET 연동

Steam 닉네임 로비 연동

상점 그래픽 제작

스크린샷 5장 이상

상점 페이지 제출



11주차:

Steam 빌드 업로드 및 비공개 테스트



작업:

PC Standalone 빌드

Steam Depot 생성

SteamCmd 업로드

Default Branch 연결

비공개 테스트 키 발급

실제 Steam 클라이언트 테스트



12주차:

최종 출시



작업:

상점 및 빌드 승인 확인

제품 가격 확정

커뮤니티 홍보

GIF 및 출시 예고 글 준비

Steam Release Product

라이브 트래픽 체크

유저 피드백 수집





============================================================

22\. Codex 작업 방식

============================================================



Codex가 코드를 수정할 때는 아래 순서를 따른다.



1\. 현재 구조 파악

2\. 기존 기능 보존

3\. 변경 범위 최소화

4\. GC, 네트워크, DI, UniTask 규칙 확인

5\. 컴파일 에러 가능성 점검

6\. 필요한 경우 간단한 사용 예시 추가

7\. 변경 요약 작성



금지 행동:

기존 기능 삭제

임의의 싱글톤 추가

Coroutine 추가

private 필드 언더바 추가

런타임 핵심 로직에 LINQ 남발

네트워크 상태를 로컬 전용으로 처리

DOTween Kill 누락

CancellationToken 없는 무한 또는 장기 UniTask 루프

씬에 강하게 결합된 하드코딩 남발

UI를 코드로 즉석 동적 생성

검증되지 않은 맵 또는 플레이 구조물 동적 생성

플레이 및 사용자 경험에 지장을 줄 수 있는 런타임 생성



선호 행동:

작고 명확한 클래스 작성

서비스와 View 분리

VContainer 주입 구조 사용

UniTask와 CancellationToken 사용

R3 상태 구독과 Dispose 처리

Pooling 사용

네트워크 권한 주체 명확화

테스트 가능한 순수 로직 분리

한국어 주석 적절하게 작성하되, 명사형 어미로 끝나게 한다. (예: 인스턴스를 만듭니다 -> 인스턴스를 만듦.)




============================================================



임시 검증용 코드 처리:

초기 조작감, 씬 전환, 카메라, HUD 상태를 빠르게 확인하기 위한 임시 코드는 허용한다.

단, 임시 코드는 제품 구조로 간주하지 않는다.

예시:

MainMenuQuickStartController

DashCooldownLogView

LocalPlayerCameraFollow

위와 같은 임시 클래스는 실제 UI, CameraService, SceneFlow, Lobby Flow가 추가되면 교체하거나 제거한다.

임시 HUD는 코드로 UI를 동적 생성하지 않는다.

쿨타임, 상태 표시, 디버그 안내는 로그 또는 미리 배치된 View/Prefab 기반으로만 처리한다.

임시 씬 진입 로직도 SceneLoader와 GameSettings를 경유해야 하며, raw string scene name을 직접 흩뿌리지 않는다.

임시 코드에는 한국어 주석으로 목적과 교체 시점을 남긴다.

임시 코드가 네트워크 권한, 라운드 규칙, UI Manager 구조를 우회해서 영구 기능처럼 자리 잡지 않도록 한다.



24\. 절대 잊지 말 것

============================================================



Boom Runner는 단순히 폭탄 피하기 게임이 아니다.



핵심 재미:

살아 움직이는 폭탄

누가 타겟인지 모두가 아는 강렬한 락온 연출

몸을 비벼 넘기는 타겟 토스

5초 면역으로 생기는 심리전

도발 40% 확률 도박

도발할수록 강해지는 폭탄

전원 몰살 위기에서만 터지는 라스트 스탠드

0.02초 차이로 갈리는 코믹한 최후 생존

UGC 맵으로 생기는 트롤링 난장판



모든 코드와 구조는 이 재미를 빠르게 검증하고 안정적으로 확장하기 위해 존재한다.



============================================================

추가 규칙: 맵 컨셉 / 던지기 아이템

============================================================



Map Theme Rule:

\- Each map must have a strong readable concept/theme.

\- Do not create maps that are only different wall layouts.

\- A map should have its own visual identity, representative gimmicks, representative throwable items, and sound mood.

\- Players should feel excited when the map appears.

\- Map theme must support the bomb chase and target-passing core, not replace it.



Example Map Themes:

\- Banana Factory: conveyors, banana peels, slippery floor.

\- Toilet Trouble: plungers, cute poop puddles, toilet pipes, wet tiles.

\- Jelly Lab: jelly floors, bumpers, slime blobs.

\- Toy Room: block walls, rubber balls, toy hammers, rotating gates.

\- Volcano Snack Island: Bomb Rain/meteor pressure, hot floor warnings, meteor fragments.

\- Construction Playground: cones, boxes, plungers, shutters, switches, narrow paths.



Throwable Item Rule:

\- Maps can contain small throwable pickup items.

\- Players may hold only one throwable item at a time.

\- Pickup/throw input should initially be a simple button, such as E or gamepad face button.

\- Initial aiming should use character facing direction or movement direction, not precision aiming.

\- Throwable items are comedic disruption tools, not primary kill tools.

\- Effects must be short, readable, and fair.



Throwable Item Examples:

\- BananaPeel: brief slip or fall.

\- Plunger: sticks to the target visually and briefly slows them.

\- Poop: creates a small cute dirty puddle that slows players.

\- RubberBall: short knockback.

\- SlimeBlob: sticky slow.

\- Firework or MeteorFragment: Crazy-mode knockback hazard.



Balance Rules:

\- Map-wide item count should start around 3 to 6.

\- Respawn time should start around 8 to 15 seconds.

\- Most item effects should last about 0.5 to 1.5 seconds.

\- Long stun on the current bomb target is forbidden because it can become an unfair guaranteed death.

\- Target players may receive reduced item effect duration.

\- Items should create funny mistakes and forced movement, not unavoidable deaths.



Network Rules:

\- Host/Master confirms pickup, throw, hit, and status effect application.

\- Clients only request pickup/throw.

\- Clients may play projectile visuals, but authoritative hit/effect state comes from Host/Master.

\- Do not trust a client claim such as “I hit Player 3.”

\- Host must validate item existence, pickup distance, throw direction, and hit result.



Implementation Rules:

\- Avoid raw string IDs for item types.

\- Use ThrowableItemType enum or typed ID.

\- Item definitions can be ScriptableObjects.

\- Item spawn points should be part of validated map data.

\- Repeated item objects, projectiles, puddles, and VFX should be pooled.

\- Add Korean comments explaining gameplay purpose, Host authority, and status effect intent.



Suggested enum:

public enum ThrowableItemType

{

&#x20;   None,

&#x20;   BananaPeel,

&#x20;   Plunger,

&#x20;   Poop,

&#x20;   RubberBall,

&#x20;   SlimeBlob,

&#x20;   Firework,

&#x20;   MeteorFragment

}



Suggested systems:

\- ThrowableItemDefinition

\- ThrowableItemSpawnPoint

\- ThrowableItemService

\- ThrowableProjectile

\- PlayerItemHolder

\- PlayerStatusEffectController





