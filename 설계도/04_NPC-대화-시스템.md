# 04. NPC 대화 시스템

---

## 역할

| 역할 | 설명 |
|------|------|
| 스토리·세계관 전달 | NPC 대사로 배경·힌트·세계관 전달 |
| 퀘스트 수락·완료 | 대화 노드에서 퀘스트 시작 또는 완료·보상 |
| 호감도 분기 | 선택지·특정 노드 도달 시 NPC 호감도 변화 → 대화 조건으로 재사용 |
| 상점·서비스 연결 | 대화 완료 후 상점 UI 오픈 (별도 시스템, 대화 후 콜백으로 연결) |
| 미연시 요소 (서브, 나중 구현) | 주요 NPC와 호감도 기반 이벤트 대화·스킨 해금. 이 설계도는 토대만 잡음 |

---

## 1. 목표 / 완료 정의

- NPC에게 E키로 대화 시작 → 노드 순서대로 텍스트 출력 → 선택지 선택 → 완료까지 한 사이클이 동작한다.
- 조건 우선순위 목록으로 상황에 맞는 대화가 자동 선택된다.
- 노드 액션(퀘스트 수락·완료, 호감도 변화, 재화 지급)이 대화 중 실행된다.
- 본 대화 기록 + 선택지 기록이 PlayFab에 저장·복원된다.
- DialogueDef ScriptableObject만 새로 만들면 새 대화를 추가할 수 있는 **템플릿** 구조다.

---

## 2. 현재 상태 & 재사용 자산

| 자산 | 경로 | 재사용 방식 |
|------|------|-----------|
| 상호작용 | `Scripts/Interaction/Interactable.cs` | NPC 오브젝트에 부착, E키 `onInteract` → 대화 시작 |
| 재화 지급 | `MetaState.Wallet.Add(CurrencyKind, amount)` | 노드 액션 RewardAction에서 호출 |
| 저장 | `Backend/MetaSaveService.cs` | `KEY_DIALOGUE` 추가, 기존 Export/LoadState 패턴 |
| 호감도 (05번 호감도 시스템 의존) | `MetaState.Affinity` (미구현) | 대화 조건으로 호감도 수치 참조. 호감도 시스템 구현 전까지 조건 검사 스킵 |
| 퀘스트 (05번 퀘스트 시스템 의존) | `MetaState.QuestState` (미구현) | 대화 조건·액션에서 퀘스트 상태 참조. 퀘스트 시스템 구현 전까지 조건 검사 스킵 |

---

## 3. 아키텍처

### 데이터 (ScriptableObject)

```
NpcDef (ScriptableObject, NPC 1인당 1개)
├── npcId          : string            — 고유 ID ("npc_physicist_euler")
├── displayName    : string            — 표시 이름
├── portrait       : Sprite            — 초상화 (일반 대화 UI용)
├── fullArt        : Sprite            — 전신 일러스트 (주요 대화 콜로쉬용, 없으면 null)
└── dialogueSlots  : DialogueSlot[]    — 조건 우선순위 목록 (위에서 첫 번째 조건 충족 대화 실행)

DialogueSlot
├── condition      : DialogueCondition — 이 대화를 보여줄 조건
└── dialogue       : DialogueDef       — 보여줄 대화

DialogueDef (ScriptableObject, 대화 1개당 1개)
├── dialogueId     : string            — 고유 ID ("dlg_euler_first_meet")
├── dialogueType   : DialogueType      — Normal(하단 박스) / Major(전체 화면 콜로쉬)
└── nodes          : DialogueNode[]    — 노드 목록 (index 0 = 시작)

DialogueNode
├── nodeId         : string            — 노드 고유 ID
├── speaker        : string            — 발화자 이름 (NPC 이름 또는 "플레이어")
├── text           : string            — 대사 텍스트
├── actions        : NodeAction[]      — 이 노드 도달 시 실행할 액션들
├── choices        : DialogueChoice[]  — 비어있으면 선형(자동 다음), 있으면 선택지 표시
└── nextNodeId     : string            — choices 없을 때 다음 노드 (비어있으면 대화 종료)

DialogueChoice
├── label          : string            — 선택지 텍스트
├── condition      : DialogueCondition — 이 선택지를 보여줄 조건 (없으면 항상 표시)
└── nextNodeId     : string            — 선택 시 이동할 노드
```

---

### 조건 (DialogueCondition)

```
DialogueCondition (Serializable)
├── conditionType : ConditionType
│   — None(항상 통과)
│   — FirstMeet       (이 dialogueId를 아직 본 적 없음)
│   — DialogueSeen    (특정 dialogueId를 본 적 있음)
│   — QuestActive     (특정 퀘스트 진행 중)
│   — QuestComplete   (특정 퀘스트 완료)
│   — AffinityMin     (NPC 호감도 ≥ 지정값)
│   — AffinityMax     (NPC 호감도 < 지정값)
├── targetId      : string   — dialogueId / questId / npcId 등 조건 대상
└── value         : int      — 호감도 수치 등 비교값
```

- 퀘스트·호감도 조건은 해당 시스템 미구현 시 **항상 통과**로 처리(개발 초기 안전).

---

### 액션 (NodeAction)

```
NodeAction (Serializable abstract → 타입별 구체 클래스)
```

| 타입 | 클래스 | 동작 |
|------|--------|------|
| 재화 지급 | `RewardAction` | `Wallet.Add(kind, amount)` |
| 퀘스트 수락 | `QuestStartAction` | `QuestService.Start(questId)` |
| 퀘스트 완료 보상 | `QuestCompleteAction` | `QuestService.Complete(questId)` |
| 호감도 변화 | `AffinityChangeAction` | `AffinityService.Change(npcId, delta)` |

> 상점 오픈은 대화 **종료 콜백**으로 처리(대화 노드 액션이 아님). `DialoguePlayer.OnDialogueEnd`에 상점 UI 열기를 등록.

---

### 런타임

**DialoguePlayer** (MonoBehaviour, 영구 씬 또는 UI 씬)

```
StartDialogue(NpcDef npc)
  → npc.dialogueSlots 위에서부터 condition 체크
  → 첫 번째 통과 DialogueDef 선택
  → dialogueType에 따라 UI 전환 (Normal / Major)
  → LoadNode(nodes[0])

LoadNode(DialogueNode node)
  → node.actions 실행 (NodeAction.Execute())
  → UI에 speaker·text 표시
  → choices 없으면: [계속] 버튼 → NextNode(node.nextNodeId)
  → choices 있으면: 조건 통과한 choice들만 버튼으로 표시
  → 선택 시: DialogueProgress.RecordChoice(nodeId, choiceIndex) → NextNode(choice.nextNodeId)

NextNode(nodeId)
  → nodeId 비어있으면 EndDialogue()
  → 아니면 LoadNode(해당 노드)

EndDialogue()
  → DialogueProgress.MarkSeen(dialogueId)
  → MetaSaveService.Save()
  → OnDialogueEnd 이벤트 발행 (상점 오픈 등 외부 구독)
  → UI 닫기
```

---

### 저장: DialogueProgress

```
DialogueProgress
├── HashSet<string>              seenDialogueIds    — 본 대화 ID 집합
└── Dictionary<string, int>      choicesMade        — nodeId → 선택한 choice index

DialogueProgressData (Serializable)
├── string[] seenDialogueIds
├── string[] choiceNodeIds
└── int[]    choiceIndices
```

- `MetaState`에 `DialogueProgress` 등록.
- `MetaSaveService`에 `KEY_DIALOGUE = "dialogueProgress"` 추가, 기존 패턴 그대로.

---

### UI 구성

**일반 대화 (Normal)**
```
[DialogueBox]                     ← 화면 하단 패널
  [Portrait]   [SpeakerName]
               [Text]             ← 타이핑 이펙트
               [ChoiceContainer]  ← choice 버튼들 (없으면 [계속] 버튼)
```

**주요 대화 (Major)**
```
[CollosseumOverlay]               ← 전체 화면 어둡게
  [FullArtLeft or Right]          ← NPC 전신 일러스트
  [DialogueBox]                   ← 위와 동일 하단 박스 (재사용)
```

- DialogueBox는 Normal/Major 공통 컴포넌트. Major는 `CollosseumOverlay`를 추가로 활성화.
- 타이핑 이펙트: 클릭/스페이스로 스킵.

---

## 4. 신규/수정 파일 목록

| 파일 경로 | 유형 | 역할 |
|-----------|------|------|
| `Scripts/Dialogue/NpcDef.cs` | 신규 | ScriptableObject: NPC 정의 + 조건부 대화 슬롯 |
| `Scripts/Dialogue/DialogueDef.cs` | 신규 | ScriptableObject: 대화(노드 목록) |
| `Scripts/Dialogue/DialogueNode.cs` | 신규 | Serializable: 노드(텍스트·선택지·액션) |
| `Scripts/Dialogue/DialogueChoice.cs` | 신규 | Serializable: 선택지 |
| `Scripts/Dialogue/DialogueCondition.cs` | 신규 | Serializable: 조건 타입·파라미터 |
| `Scripts/Dialogue/NodeAction.cs` | 신규 | abstract Serializable: 노드 액션 기반 |
| `Scripts/Dialogue/RewardAction.cs` | 신규 | NodeAction 구현: 재화 지급 |
| `Scripts/Dialogue/QuestStartAction.cs` | 신규 | NodeAction 구현: 퀘스트 수락 (05번 연동용 스텁) |
| `Scripts/Dialogue/QuestCompleteAction.cs` | 신규 | NodeAction 구현: 퀘스트 완료 (05번 연동용 스텁) |
| `Scripts/Dialogue/AffinityChangeAction.cs` | 신규 | NodeAction 구현: 호감도 변화 (10번 연동용 스텁) |
| `Scripts/Dialogue/DialoguePlayer.cs` | 신규 | MonoBehaviour: 대화 진행 로직 |
| `Scripts/Meta/DialogueProgress.cs` | 신규 | seen ID 집합 + 선택지 기록, +Data |
| `Scripts/Meta/MetaState.cs` | **수정** | `DialogueProgress` 필드 추가 |
| `Backend/MetaSaveService.cs` | **수정** | `KEY_DIALOGUE` 추가 |
| `Backend/DialogueUI.cs` | 신규 | MonoBehaviour: DialogueBox·CollosseumOverlay UI 제어 |
| `Data/Dialogues/` | 신규 폴더 | NpcDef·DialogueDef ScriptableObject 에셋 |

---

## 5. 단계별 구현 순서

### 5-1. 데이터 모델 (코드만)
- [ ] `DialogueCondition.cs`, `DialogueChoice.cs`, `NodeAction.cs` (+ 4종 구현체) 작성
- [ ] `DialogueNode.cs`, `DialogueDef.cs`, `NpcDef.cs` 작성
- [ ] `DialogueProgress.cs` + Data 작성
- [ ] `MetaState`·`MetaSaveService` 확장

### 5-2. DialoguePlayer (로직)
- [ ] `DialoguePlayer.cs` 작성 (StartDialogue → 조건 선택 → 노드 순회 → 액션 실행 → 저장)
- [ ] 테스트용 NpcDef + DialogueDef 에셋 1개 (조건 없음, 3노드 선형) 제작

### 5-3. DialogueUI (Normal)
- [ ] `DialogueUI.cs` 작성: DialogueBox, Portrait, SpeakerName, Text, ChoiceContainer
- [ ] 타이핑 이펙트 + 스킵 입력 구현
- [ ] DialoguePlayer와 연결, 플레이 모드에서 선형 대화 동작 확인

### 5-4. 선택지·조건 분기
- [ ] ChoiceContainer에 choice 버튼 동적 생성 (조건 통과 항목만 표시)
- [ ] 선택 시 `DialogueProgress.RecordChoice` 호출 확인
- [ ] 테스트 에셋에 선택지·분기 추가, 동작 확인

### 5-5. 노드 액션 실행
- [ ] `RewardAction` 실행 확인 (Wallet.Add)
- [ ] `AffinityChangeAction` 스텁 (호감도 시스템 미구현 시 로그만)
- [ ] `QuestStartAction` / `QuestCompleteAction` 스텁

### 5-6. Major(콜로쉬) UI
- [ ] `CollosseumOverlay` 전체 화면 패널 제작
- [ ] `DialogueType.Major`일 때 오버레이 활성 + FullArt 표시
- [ ] 테스트 에셋을 Major로 설정해 동작 확인

### 5-7. NPC 연결
- [ ] NPC 오브젝트에 `Interactable` + `NpcDef` 연결
- [ ] E키 → `DialoguePlayer.StartDialogue(npcDef)` 호출 확인
- [ ] 조건 우선순위 목록 테스트: 슬롯 2개(조건 다름), 상황 바꿔 다른 대화 나오는지 확인

### 5-8. 저장·복원
- [ ] 대화 완료 시 `seenDialogueIds` 저장, 재시작 후 같은 NPC에서 다른 슬롯 대화 나오는지 확인

---

## 6. 엣지 케이스 / 주의점

| 상황 | 처리 방식 |
|------|----------|
| dialogueSlots 조건 전부 불통과 | 마지막 슬롯을 조건 None(항상 통과)으로 두는 규칙 → 항상 기본 대화 보장 |
| 퀘스트·호감도 시스템 미구현 | 해당 conditionType은 항상 `true` 반환(스킵). 시스템 완성 후 실제 참조로 교체 |
| 선택지가 조건 때문에 0개 | UI에 [계속] 버튼 표시, `nextNodeId`로 진행 |
| 대화 중 씬 전환·전멸 | `DialoguePlayer.ForceEnd()` — UI 닫기 + 저장. 씬 이벤트에서 호출 |
| 타이핑 도중 다음 노드 요청 | 타이핑 즉시 완성 → 한 번 더 클릭하면 다음 노드 |
| NodeId 참조 오류 (없는 ID) | 경고 로그 + EndDialogue() 호출 (강제 종료) |
| 선택지 기록 중복 저장 | `choicesMade[nodeId]`에 덮어쓰기 — 재플레이 시 가장 최근 선택 반영 |

---

## 7. 검증 방법

### 최소 검증 세트
테스트용 NPC 1개, DialogueDef 2개(슬롯A: FirstMeet 조건 / 슬롯B: 조건 None), 노드 5개(선형 3 + 분기 1 + 선택 2).

| 검증 항목 | 기대 결과 |
|----------|----------|
| NPC E키 → 최초 대화 | 슬롯A(FirstMeet) 대화 실행 |
| 대화 완료 후 다시 E키 | 슬롯B(기본) 대화 실행 |
| 선택지 선택 | 해당 nextNodeId 노드로 이동, choicesMade 기록 |
| RewardAction 포함 노드 도달 | Wallet에 재화 추가 확인 |
| Major 대화 | 전체 화면 콜로쉬 오버레이 + FullArt 표시 |
| 타이핑 스킵 | 클릭 1회 → 전체 출력, 2회 → 다음 노드 |
| 저장·복원 | 게임 재시작 후 두 번째 대화(슬롯B)부터 시작 |

Claude가 PowerShell 스크린샷으로 직접 확인(메모리 규칙).
