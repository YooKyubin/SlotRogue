# ADR 규칙

Architecture Decision Record는 구속력 있는 결정을 파일 1개로 기록한다.

---

## 작성 시점

거절된 대안이 1개 이상 있는 구체적·인용 가능한 결정이 내려질 때. 예:

- 슬롯 RNG / 페이아웃 모델 (고정 RTP vs 동적)
- 세이브 포맷 (JSON 평문 vs 암호화 binary)
- Addressables 그룹 / 빌드 전략
- 광고/IAP SDK 채택
- 광역 정책 (예: "모든 비동기는 UniTask")

단일 선언적 문장으로 표현 가능하고 거절된 대안이 1개 이상이면 ADR-shaped. 그렇지 않으면 narrative, 열린 질문, 스타일 선호에 가까우므로 design-doc, `coding-style.md`, 또는 코드 주석에 둔다.

---

## 형식

[`../adr/TEMPLATE.md`](../adr/TEMPLATE.md) 사용. 필수 필드:

- Status
- Date
- Context
- Decision
- Alternatives considered
- Consequences

**Alternatives considered**: 1개 이상 **권장**. 정말 대안이 없으면 ADR이 아닐 가능성이 있으므로 design-doc이나 코드 주석을 고려한다.

> 참고: KyuEngine은 "must contain ≥1 rejected alternative"였다. SlotRogue는 1달 스프린트 특성상 권장으로 완화. 단, 0개일 땐 *왜 대안이 없는지* Notes에 한 줄 적기를 권장.

---

## 번호와 lifecycle

- 파일명: `NNNN-kebab-case-title.md` (zero-pad 4자리).
- 번호는 **append-only**. renumber/reuse/delete 금지.
- Supersede: 새 ADR이 옛 것을 `Supersedes:`로 인용, 옛 ADR의 `Status`를 `superseded`로 바꾸고 `Superseded by:` 라인 추가. 두 파일 모두 디스크에 남는다.
- 거절된 제안도 `Status: rejected`로 디스크에 남긴다. 거절 자체가 검색 가능해야 한다.

---

## design-doc과의 관계

- 결정 본문은 ADR에 둔다.
- design-doc은 ADR을 인용하되 Context/Alternatives/Consequences를 재서술하지 않는다.
- design-doc에서 한 줄 navigation 요약은 허용한다. 예: `ADR-0001 — 슬롯 RNG는 결정론적 시드 기반`.
- ADR Decision 본문을 in-place 수정하지 않는다. 본질 변경은 supersede로 처리한다.

---

## 갱신

- 오타 / 명확화 수정 (결정 본문 비변경): in-place 편집, Date bump 불요.
- 본질이 바뀌면: 새 ADR로 supersede.
- [`../adr/INDEX.md`](../adr/INDEX.md) 표는 새 ADR 또는 status 변경과 같은 커밋에 갱신.
