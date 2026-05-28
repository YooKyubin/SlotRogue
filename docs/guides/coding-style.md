# C# / Unity 코딩 스타일

SlotRogue 코드 작성 시 따르는 규칙. **절대 규칙**은 [`../../AGENTS.md`](../../AGENTS.md) §6에 있고, 이 문서는 세부 사항을 펼친다.

---

## 네이밍

| 대상 | 규칙 | 예 |
|------|------|-----|
| 타입 (class, struct, enum, interface) | `PascalCase`, interface는 `I` prefix | `SpinResult`, `ISaveStore` |
| 메서드, 프로퍼티, 이벤트 | `PascalCase` | `EvaluatePayline`, `IsSpinning`, `OnSpinComplete` |
| public 상수 | `PascalCase` | `MaxReelCount` |
| private/internal 필드 | `_camelCase` (언더스코어 + camelCase) | `_currentSpin`, `_reels` |
| 로컬 변수, 파라미터 | `camelCase` | `spinIndex`, `payoutMultiplier` |
| `[SerializeField] private` 필드 | `_camelCase` (private 필드 규칙과 동일) | `[SerializeField] private Reel[] _reels;` |
| 제네릭 타입 파라미터 | `T` 또는 `TName` | `T`, `TResult`, `TSaveData` |
| 파일명 | 타입명과 동일 (`PascalCase.cs`) | `SpinResult.cs` |

**금지**:
- `m_` prefix (`m_reels`) — 사용 안 함.
- `s_` / `g_` prefix (static/global) — 필요하면 클래스 분리.
- 헝가리언 (`strName`, `iCount`).
- 약어 대문자 폭주 (`HTTPSConnection` 대신 `HttpsConnection`).

### 서술적 이름 — 약어 금지

이름만 보고 변수가 무엇을 담는지 파악할 수 있어야 한다. 선언 줄까지 스크롤하지 않게 한다. 로컬·파라미터·필드·루프 변수 **모두** 동일하게 적용.

- **한 글자 / 줄임말 금지.** `r`, `bb`, `mgr`, `tmp`, `buf` 대신 `reel`, `backBuffer`, `manager`, `tempBuffer`.
- **`foreach` 루프 변수도 풀네임.** `foreach (var reel in _reels)` (O), `foreach (var r in _reels)` (X). 좁은 스코프라도 독자는 약어를 해독해야 한다.
- **허용되는 예외** (좁게):
  - 사소한 정수 카운터의 인덱스 `i`, `j`, `k`.
  - 수학·그래픽스에서 표기 자체가 표준인 짧은 이름: `x`, `y`, `z`, `w`, `r`, `g`, `b`, `a`, `uv`, `n` (normal), `t` (시간/파라미터), 행렬 `M`, `V`, `P`. 주변 문맥이 모호하지 않을 때만.
  - 도메인에서 풀어쓰는 게 오히려 어색한 확립된 약자: `id`, `url`, `ui`, `rng`, `rtt`. **개념 자체일 때만** bare 형태. 구체 인스턴스(핸들, 카운트, 포맷)는 수식어를 붙인다 — `rngSeed`, `uiCanvas`.
  - Unity SDK 관용: 인스펙터 노출 desc 구조체에 `desc`, 디버그 라벨에 `name`. 같은 스코프에 여러 desc가 있으면 수식어 (`spinDesc`, `payoutDesc`).
- **헷갈리면 풀어쓴다.** 길어지는 비용은 싸고, 모호함은 비싸다.

---

## MonoBehaviour 패턴

```csharp
public class Reel : MonoBehaviour
{
    [SerializeField] private Symbol[] _symbols;
    [SerializeField] private float _spinDuration = 1.5f;

    private bool _isSpinning;
    private Tween _tween;

    public bool IsSpinning => _isSpinning;
    public event Action<int> SpinCompleted;

    private void OnDisable()
    {
        transform.DOKill(true);
    }

    public async UniTask SpinAsync(int targetIndex, CancellationToken ct)
    {
        _isSpinning = true;
        try
        {
            await transform.DOLocalMoveY(targetY, _spinDuration)
                .SetEase(Ease.OutCubic)
                .WithCancellation(ct);
        }
        finally
        {
            _isSpinning = false;
        }
    }

    private void HandleTweenComplete() { }
}
```

원칙:
- **멤버 순서는 §클래스 멤버 순서**를 따른다 (필드 → 프로퍼티/이벤트 → lifecycle → public API → private 헬퍼).
- **`[SerializeField] private` 우선.** `public` 필드 금지 (불변 상수만 예외).
- **읽기 전용 노출은 expression-bodied property** (`public bool IsSpinning => _isSpinning;`).
- **Unity 메시지**(`Awake` / `OnEnable` / `Start` / `Update` / `OnDisable` / `OnDestroy`)는 lifecycle 블록 안에서 **실행 순서대로** 위에서 아래로.
- **public API 메서드는 private 헬퍼보다 위**, lifecycle 묶음은 public API **바로 위**.
- 인스펙터 노출이 필요 없는 컴포넌트 참조는 `GetComponent` 캐시 또는 `[RequireComponent]`.

---

## 비동기 (UniTask)

- **모든 비동기는 `UniTask` / `UniTask<T>`.** `Task` 사용 금지 (할당 비용, Unity 메인스레드 컨텍스트 미지원).
- **`async void` 금지.** 이벤트 핸들러는 `async UniTaskVoid` 반환.
- **`CancellationToken` 전파**: 비동기 메서드 마지막 파라미터로 받고, 호출 시 컴포넌트의 `destroyCancellationToken` 전달.
- **`UnityEngine.Object` 사망 후 await 금지**: 씬 전환 / 오브젝트 파괴 시 cancellation으로 깔끔히 종료.

```csharp
private async UniTaskVoid OnButtonClicked()
{
    try
    {
        await PerformSpinAsync(destroyCancellationToken);
    }
    catch (OperationCanceledException)
    {
        // 정상 취소 — 무시
    }
}
```

---

## 트윈 (DOTween)

- **DOTween 핸들은 `OnDisable` / `OnDestroy`에서 정리.**
  - 간단: `transform.DOKill(true)` (해당 transform의 모든 트윈 종료, `true` = onComplete 호출).
  - 명시적: 핸들 보관 후 `_tween?.Kill()`.
- **씬 전환 시 잔존 트윈 주의**: 글로벌 트윈 (예: `DOTween.To`)은 명시 Kill 필요.
- **체이닝은 가독성 위해 줄바꿈**:
  ```csharp
  _tween = transform.DOLocalMoveY(targetY, duration)
      .SetEase(Ease.OutCubic)
      .OnComplete(() => _isSpinning = false);
  ```
- **콜백에서 `this` 캡처 주의**: 람다가 destroyed 오브젝트의 필드를 만지면 null reference.

---

## Null 체크

Unity의 `UnityEngine.Object`는 `==` 오버로드되어 있어 destroyed 오브젝트가 `null`처럼 동작한다. 단 **C# 6의 `?.`은 이 오버로드를 우회**해서 destroyed 오브젝트도 통과한다.

```csharp
// 위험: destroyed 오브젝트도 통과
_audio?.Play();

// 안전: Unity null 체크
if (_audio != null) _audio.Play();
```

룰:
- `UnityEngine.Object` 파생은 **`!= null` 명시적 체크**.
- 순수 C# 객체(예: ScriptableObject가 아닌 DTO)는 `?.` 사용 가능.

---

## 이벤트

- **C# `event` 우선** (인스펙터 노출 불요 시):
  ```csharp
  public event Action<SpinResult> SpinCompleted;
  ```
- **`UnityEvent`는 인스펙터에서 연결해야 할 때만**. 런타임 구독은 C# event가 더 가볍고 명시적.
- **구독 해제 책임**: 구독한 쪽이 `OnDisable`에서 unsubscribe.
- **null 호출은 `?.Invoke()`**:
  ```csharp
  SpinCompleted?.Invoke(result);
  ```

---

## 로깅

- **`Debug.Log`는 임시.** 디버깅 끝나면 정리.
- **영구 로그는 wrapper 통해**:
  ```csharp
  public static class GameLog
  {
      [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
      public static void Info(string msg) => Debug.Log($"[GAME] {msg}");
  }
  ```
- 릴리즈 빌드에서 자동 제거되어 IL2CPP 성능 손실 없음.
- `Debug.LogError`는 진짜 에러일 때만. 정상 흐름에 로그 남길 거면 `Info`.

---

## ScriptableObject

- **밸런스 수치, 슬롯 심볼 정의, 보상 풀** 등 데이터는 `ScriptableObject`로.
- `CreateAssetMenu` 필수:
  ```csharp
  [CreateAssetMenu(menuName = "SlotRogue/Symbol", fileName = "NewSymbol")]
  public class SymbolDefinition : ScriptableObject
  {
      [SerializeField] private string _id;
      [SerializeField] private Sprite _icon;
      [SerializeField] private int _basePayout;

      public string Id => _id;
      public Sprite Icon => _icon;
      public int BasePayout => _basePayout;
  }
  ```
- **인스턴스는 `Assets/_Project/Data/`에 보관**, design-doc의 "의도"와 분리.
- **런타임에 SO 필드를 수정하지 말 것** — 에디터에서는 디스크에 즉시 반영되어 의도치 않은 변경이 커밋된다.

---

## Addressables 키 관리

- **문자열 리터럴 산재 금지.** 키를 상수 또는 SO로 모은다.
  ```csharp
  public static class AddressableKeys
  {
      public const string MainMenuScene = "Scenes/MainMenu";
      public const string SymbolCherry = "Symbols/Cherry";
  }
  ```
- 또는 키 자체를 SO 필드로 보관 (`AssetReference`).

---

## 주석

- **"무엇"이 아니라 "왜"만**. 코드가 말하는 것을 반복하지 않는다.
- **트레이드오프, 제약, 비명백한 의도, 외부 버그 회피**만 적는다.
- 안 좋은 예:
  ```csharp
  // _isSpinning을 true로 설정
  _isSpinning = true;
  ```
- 좋은 예:
  ```csharp
  // DOTween OnComplete가 cancellation 시에도 호출되므로,
  // _isSpinning은 try/finally의 finally에서 리셋한다.
  _isSpinning = true;
  ```
- **XML doc 주석 (`///`)**: 공개 API에만. private 메서드는 코드로 충분하면 생략.

---

## 예외

- **게임 로직에서 예외로 흐름 제어 금지.** 데이터 검증은 반환값 / `bool TryDoThing(out ...)` / assert.
- **예외는 진짜로 회복 불가능한 invariant 위반 때만** throw. (예: SO에 음수 확률)
- **catch는 좁게**. `catch (Exception)`은 최상위 진입점에서만.

---

## 파일 구조

- 한 파일에 한 public 타입. 내부 보조 타입(struct, enum)은 같은 파일에 둘 수 있음.
- using 정렬: System → UnityEngine → 그 외 → 프로젝트 namespace.
- 네임스페이스는 `SlotRogue.<Module>` (예: `SlotRogue.Core.Slot`, `SlotRogue.UI`).

---

## `var` 사용 정책

`var`는 허용하되 기본은 아니다. 룰: **그 줄만 읽고 타입을 알 수 있어야 한다** — `var`가 타입을 *더 잘* 보이게 만들거나, 우변에 타입이 보이거나.

**`var` 권장**:
- 우변에 타입이 드러나는 경우: `var spin = new SpinResult(...)`, `var count = (int)payout`.
- 타입을 적기 비현실적인 경우: 익명 타입, LINQ 결과, 깊은 제네릭 (`Dictionary<string, List<SymbolDefinition>>`).
- 노이즈가 정보보다 큰 boilerplate.

**명시 타입 권장**:
- 함수의 반환값을 받는데 우변에 타입이 없는 경우: `SpinResult result = EvaluatePayline(reels);`. 호출 측에서 타입을 헤더 열지 않고 확인 가능.
- `int` / `float` / `double` 같은 primitive — 우변이 리터럴이면 의도가 모호해진다. `int hp = 100;` (O), `var hp = 100;` (X).
- API 계약상 타입을 고정해야 할 때 (예: `uint`를 의도하는데 `var`로 받으면 `int`로 deduce).

**참조 한정자**:
- 컬렉션 순회 시 복사하지 말 것: `foreach (var symbol in _symbols)`는 `SymbolDefinition`이 클래스면 OK지만 struct면 복사. struct 컬렉션은 `foreach (ref var symbol in _symbols.AsSpan())` 같은 형태로 의도 표시.
- 람다 캡처에서 `var`로 받은 변수의 타입이 의도와 다른지 검토.

핵심 — `var`는 타입을 *숨기는* 도구가 아니라, 이미 자명한 타입의 의식(ceremony)을 줄이는 도구다.

---

## 클래스 멤버 순서

`class` / `struct` 내부는 **역할(종류)별로 위에서 아래로** 묶는다. 같은 블록 안에서만 **접근 수준**(public → internal → protected → private)을 적용한다.

| 순서 | 블록 | 내용 |
|------|------|------|
| 1 | **필드** | `const` / `static readonly` → `[SerializeField] private` → 그 외 private 필드 |
| 2 | **프로퍼티·이벤트** | public 읽기 전용 등 (필드 바로 다음) |
| 3 | **생성자** | (있을 때만) |
| 4 | **Unity lifecycle** | MonoBehaviour만 — 아래 순서표 참고 |
| 5 | **public API** | 외부·다른 타입이 호출하는 메서드 |
| 6 | **private 헬퍼** | lifecycle이 아닌 private 메서드 |

**필드 블록 세부**:
- `[SerializeField] private`는 **항상 non-serialized private 필드보다 위**. 인스펙터에 보이는 순서 = 코드 순서.
- `public` 필드는 금지(§MonoBehaviour 패턴). `const`만 public 필드 예외.

**메서드 블록 세부** (4 → 5 → 6):
- lifecycle 메시지는 **public API보다 위**, private 헬퍼보다 위.
- lifecycle 안에서는 Unity 실행 순: `Awake` → `OnEnable` → `Start` → `FixedUpdate` / `Update` / `LateUpdate` → `OnDisable` → `OnDestroy`. 쓰는 것만 포함, 순서는 건너뛰지 않음.
- public API 메서드는 private 헬퍼보다 위. 같은 블록 안에서는 public → protected → private.

### MonoBehaviour 예시

```csharp
public class Reel : MonoBehaviour
{
    // 1 — 필드
    [SerializeField] private Symbol[] _symbols;
    [SerializeField] private float _spinDuration = 1.5f;
    private bool _isSpinning;
    private Tween _tween;

    // 2 — 프로퍼티·이벤트
    public bool IsSpinning => _isSpinning;
    public event Action<int> SpinCompleted;

    // 4 — lifecycle (3 생성자 없음)
    private void Awake() { }
    private void OnEnable() { }
    private void Start() { }
    private void OnDisable() { transform.DOKill(true); }

    // 5 — public API
    public void BeginSpin(int target) { ... }
    public async UniTask SpinAsync(int targetIndex, CancellationToken ct) { ... }

    // 6 — private 헬퍼
    private void HandleTweenComplete() { }
}
```

### Plain C# 예시 (lifecycle 없음)

```csharp
public sealed class BattleResolver
{
    // 1 — 필드
    private readonly MonsterDefinition _monsterDefinition;

    // 2 — 프로퍼티
    public BattleState State { get; private set; }

    // 3 — 생성자
    public BattleResolver(MonsterDefinition monsterDefinition, int playerMaxHp) { ... }

    // 5 — public API (4 lifecycle 생략)
    public void ProcessSpin(CombatSpinOutcome outcome) { ... }

    // 6 — private 헬퍼
    private void RunPlayerPhase(CombatSpinOutcome outcome) { ... }
}
```

**ScriptableObject**도 동일: `[SerializeField]` 필드 → public 프로퍼티(§ScriptableObject 예시).

근거: Unity에서는 **상태(필드·인스펙터) → 외부 계약(프로퍼티) → Unity 콜백 → 게임 API → 내부 구현** 순이 읽기와 인스펙터 편집에 맞다.

`.editorconfig` / IDE는 멤버 순서를 자동 재정렬하지 **않는다** — 리뷰에서 수동 강제.

---

## 파일 & 함수 라인 예산

- **소프트 캡**: `.cs` 1파일 ~ 500줄, 함수 1개 ~ 한 화면(~ 50줄).
- 한 파일에 여러 책임이 쌓이면 namespace 또는 partial로 분리하기 전에 **타입을 쪼개는 게 먼저**.
- `MonoBehaviour`가 비대해지면: 순수 로직을 일반 클래스(`Plain Old C# Object`)로 추출하고, MB는 Unity 메시지 + 의존성 wiring만 남긴다.
- 함수가 길어지면: 인자 묶음을 데이터 구조로, 단계를 private 메서드로.

하드 룰이 아니라 분할 시점을 잡는 신호.

---

## 포매팅 (`.editorconfig` 권위)

repo 루트 [`/.editorconfig`](../../.editorconfig)가 **유일한 권위**다. IDE(Rider, VS, VS Code C# Dev Kit 모두 지원) 자동 포맷에 맡기고, 이 가이드는 라인 단위 규칙을 중복 기술하지 않는다.

핵심 설정 요약 (자세히는 `.editorconfig`):

- **들여쓰기 4 spaces, 탭 금지, UTF-8.**
- **Allman braces** — 함수·타입·`if`/`for`/`while` 모두 `{`를 다음 줄로.
- **단일 문장에도 항상 중괄호.** `if (cond) Do();` 금지.
- **`var` 정책**은 §`var` 사용 정책에 맞춰 인코딩.
- **using 정렬**: `System.*` 우선, 그 다음 알파벳. namespace **밖**에 둔다.
- **연속 대입/선언 정렬 안 함** (C# 관행상 노이즈).

`.editorconfig`가 처리하지 **않는** 것 (수동 책임):

- 클래스 멤버 순서 (§클래스 멤버 순서).
- using 그룹 (System / UnityEngine / 외부 / 프로젝트의 시멘틱 분리는 수동).
- 네이밍 일부 (Roslyn analyzer가 권장 수준까지만 잡는다).

로컬 강제 적용:

```bash
dotnet format SlotRogue.sln
```

(Unity가 생성하는 `.sln`은 모든 asmdef를 포함하므로 한 번에 전 코드베이스 포맷.)

---

## Format-only 커밋과 `git blame`

**순수 포매팅 변경**(`dotnet format` 일괄, mass rename, 공백 정리)은 **단독 커밋**으로 분리하고, 그 SHA를 repo 루트 [`/.git-blame-ignore-revs`](../../.git-blame-ignore-revs)에 추가한다. `git blame`이 해당 커밋을 건너뛰고 이전 진짜 커밋을 보여준다. GitHub blame UI도 동일 파일을 자동으로 인식.

클론 1회 로컬 설정:

```
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

임시 대안: `git blame -w` (whitespace 무시), `git blame --ignore-rev <sha>` (특정 커밋만 무시).

**룰**:
- 포매팅 + 동작 변경을 **같은 커밋**에 섞지 않는다.
- format-only 커밋 메시지: `style: dotnet format sweep` 또는 `style: rename _foo → _bar` 식으로 의도 명시.

---

## 금지 / 강력 비권장

- **싱글톤.** 명시적 소유와 참조 전달로 대체. `MonoBehaviour.Instance` 패턴 지양.
- **`public` 필드.** `const` 상수만 예외. 외부 노출은 property로.
- **`async void`.** 이벤트 핸들러는 `async UniTaskVoid`, 일반은 `UniTask` / `UniTask<T>`.
- **`Task` / `Task<T>`.** Unity 메인스레드 컨텍스트에서 비용·취소 처리상 `UniTask`로 통일.
- **`GameObject.Find` / `FindObjectOfType` 런타임 호출.** 직렬화 참조 또는 의존성 주입으로 해결. 진짜 부득이할 때 1회 캐시.
- **`SendMessage` / `BroadcastMessage`.** 문자열 기반 — 컴파일 타임 검증 없음.
- **`Resources.Load`** — Addressables 사용. `Resources/` 폴더는 빌드 크기·메모리 안 잡힘.
- **`Camera.main` 매 프레임 호출** — Unity 2020+에서 캐시되지만 명시적 캐시 권장.
- **`new GameObject(...)` 매 프레임** — 풀링.
- **`string` 연결 hot path** — `StringBuilder` 또는 사전 포맷.
- **`LINQ` hot path 사용** — 할당. 게임 루프 안에선 명시적 `for`.
- **암묵 변환 / 위험한 캐스트** — `float → int` 무언의 truncation 등은 `(int)` 명시.
- **`unsafe` / `IntPtr` 일반 코드** — interop 경계에서만, 주석 의무.
- **`UnityEvent` 런타임 구독** — 인스펙터 연결용. 코드 구독은 C# `event`.
- **TODO 무책임 작성** — `// TODO`만 적지 않는다. 작성자 + 날짜 + 이슈/plan 링크. 더 나은 건 exec-plan Notes에 적기.

---

## 빠른 참조

| 룰 | 강제 수단 |
|----|----------|
| 들여쓰기·중괄호·`var` 정책·간단 네이밍 | `.editorconfig` (자동) |
| 멤버 순서·using 그룹 시멘틱 | 리뷰 (수동) |
| Unity 라이프사이클·async·트윈·null | 리뷰 (수동) — AGENTS.md §6 |
| 포매팅 sweep | 단독 커밋 + `.git-blame-ignore-revs` |
