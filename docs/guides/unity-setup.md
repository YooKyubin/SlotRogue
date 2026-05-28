# Unity 셋업 가이드

SlotRogue를 새 머신에서 클론하고 에디터로 열기까지의 절차, 그리고 프로젝트 설정 / 패키지 / 폴더 컨벤션.

> 코딩 스타일은 [`coding-style.md`](./coding-style.md) 참조.

---

## Unity 버전

- **6000.3.10f1**
- Unity Hub에서 정확히 이 버전으로 설치. 빌드 타겟에 **Android Build Support**(IL2CPP / SDK / NDK / JDK 포함) 체크.
- 다른 버전으로 열면 `ProjectSettings/ProjectVersion.txt`가 변경되어 팀 전체에 영향. 버전 업그레이드는 ADR로 박제 후 실행.

---

## 첫 클론 절차

```powershell
git clone https://github.com/YooKyubin/SlotRogue.git
cd SlotRogue
```

Unity Hub에서 `Open` → 클론한 폴더 선택 → 첫 임포트(수 분 소요). 첫 임포트 후 `Library/`가 생성되며, 이 폴더는 `.gitignore`에 포함되어 있다.

---

## 프로젝트 설정 체크리스트

`Edit > Project Settings`에서 다음을 확인 (이미 커밋되어 있어야 정상, 새 머신에서 깨졌다면 보정):

### Editor

- **Asset Serialization Mode**: `Force Text` — Scene/Prefab/SO를 텍스트로 저장하여 머지 가능.
- **Version Control Mode**: `Visible Meta Files` — `.meta` 파일이 보이고 커밋 대상이 됨.
- **Default Behavior Mode**: `3D` 또는 `2D` (프로젝트 결정 — 슬롯 게임 특성상 `2D` 가능성 높음, 결정 시 ADR로 박제).

### Player (Android)

- **Scripting Backend**: `IL2CPP`
- **Api Compatibility Level**: `.NET Standard 2.1`
- **Target Architectures**: `ARM64` (필수, ARMv7는 Google Play 정책상 제외 가능)
- **Minimum API Level**: 23 이상 권장 (Google Play 요구 사항 확인)
- **Target API Level**: Automatic (가장 높은 설치된 SDK)

### Quality

- 모바일 타겟이므로 기본 Quality 프리셋 정리. URP 채택 시 URP Asset 연결 확인.

> 위 항목 중 결정 사항(예: 2D vs 3D, URP vs Built-in)은 첫 작업 시 ADR로 박제한다.

---

## 핵심 패키지

UPM (`Window > Package Manager`)으로 설치. `Packages/manifest.json`이 source of truth.

### Addressables

- Package Manager에서 `com.unity.addressables` 설치.
- `Window > Asset Management > Addressables > Groups`로 그룹 관리.
- **키는 상수 / SO로 관리**하고 문자열 리터럴 산재 금지 (AGENTS.md §6).
- 빌드 전략(로컬 only vs 원격 호스팅), 그룹 분할은 결정 시 ADR로 박제.

### UniTask

- GitHub Releases (`Cysharp/UniTask`)에서 UPM URL 또는 unitypackage로 설치.
- `manifest.json`에 git URL 추가 권장:
  ```json
  "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  ```
- **모든 비동기는 UniTask 사용**, `async void` 금지 (AGENTS.md §6).
- 이벤트 핸들러는 `UniTaskVoid`, 일반은 `UniTask` / `UniTask<T>`.

### DOTween

- Asset Store에서 무료 버전 또는 DOTween Pro 임포트.
- 설치 후 `Tools > Demigiant > DOTween Utility Panel > Setup DOTween...` 실행 (필수).
- **`OnDisable` / `OnDestroy`에서 `transform.DOKill(true)` 또는 핸들 보관 후 명시적 Kill** (AGENTS.md §6).

---

## 권장 폴더 컨벤션 (`Assets/`)

> **팀 합의로 확정 필요.** 초기 권장안은 다음과 같다.

```
Assets/
├── _Project/                   # 우리 코드/자산 (언더스코어 prefix로 상단 고정)
│   ├── Scripts/
│   │   ├── Core/               # 게임 로직 (MonoBehaviour 비의존 가능 부분)
│   │   ├── UI/                 # 화면, 버튼, HUD
│   │   ├── Data/               # ScriptableObject 정의
│   │   └── Editor/             # 에디터 전용 (asmdef 분리)
│   ├── Art/
│   │   ├── Sprites/
│   │   ├── Animations/
│   │   └── UI/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Data/                   # ScriptableObject 인스턴스 (밸런스 수치 등)
│   └── Addressables/           # Addressable 마크된 자산 (그룹 정책에 따라 분류)
├── Plugins/                    # 외부 패키지 (Asset Store 임포트 등)
└── ...
```

핵심:
- `_Project/` prefix로 우리 자산을 상단 고정 (Plugins/외부와 시각적 분리).
- `Scripts/`는 도메인별 폴더 (`Core`, `UI`, `Data`, `Editor`).
- 외부 패키지는 `Plugins/` 또는 UPM(`Packages/`).

---

## asmdef 권장 분할

| asmdef | 의존 | 역할 |
|--------|------|------|
| `SlotRogue.Core` | (없음 또는 UniTask) | 게임 로직 본체. MonoBehaviour 사용 가능하되 UI/Editor 비의존. |
| `SlotRogue.Data` | (없음) | ScriptableObject 정의. 순수 데이터. |
| `SlotRogue.UI` | `SlotRogue.Core`, `SlotRogue.Data` | 화면, 입력, 표시. |
| `SlotRogue.Editor` | `SlotRogue.Core`, `SlotRogue.Data` | 에디터 툴 (Editor 전용 platform). |

**의존 방향은 단방향**. `Core`가 `UI`를 알게 되면 즉시 위반.

---

## 자주 발생하는 문제

### "메타 파일이 없다" 경고

새 자산을 만들고 다른 팀원이 pull 했을 때 `.meta` 파일이 누락되면 발생. **`.meta`는 항상 같은 커밋에 포함** (AGENTS.md §6).

### 첫 임포트 실패

Library/ 캐시 손상 시 Library/, Temp/, obj/를 지우고 Unity 재실행. `.gitignore`에 포함되어 있으므로 안전.

### DOTween null reference (씬 전환 후)

`DOTween` 핸들이 destroyed 오브젝트를 참조 중. `OnDisable`에서 `DOKill(true)` 호출했는지 확인.

---

## 추후 추가 가이드

다음 가이드는 필요해질 때 작성한다 (지금은 만들지 않음):

- `mobile-build.md` — Android 키스토어, AAB 업로드, iOS 프로비저닝
- `unity-profiling.md` — Profiler, Frame Debugger, Memory Profiler 사용
- `addressables-workflow.md` — 그룹 분할 정책, 원격 호스팅, 업데이트 흐름
- `package-setup.md` — UPM 스코프드 레지스트리, 패키지 추가 절차 표준화
