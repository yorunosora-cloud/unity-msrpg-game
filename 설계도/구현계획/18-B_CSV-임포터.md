# 18-B 구현 계획 — 캐릭터 CSV 임포터 (관계형 10시트) + 대표 캐릭터 이관

> 근거: `설계도/18_캐릭터-스키마-확장.md` **§8 구현 개정(2026-11-22)**, §3-8, `설계도/22_전투-시스템-확장.md` §4-3·4-6·4-7. 선행: 18-A 완료.
> 방식: TDD(빨강→구현→초록→커밋), `-testPlatform PlayMode`. 기준선 **501개 중 499 통과**(기존 `SkillProgressTests` 2건 실패는 무관).
> 범위 밖: `character_codex.csv`(2단 도감·개념 문제·호감도 — 18-C), `unlockCond` 평가(스토리·승급 조회 서비스 필요), 씬 변경, 시드 10종 이전·삭제, 캐릭터 일괄 이관(대표 4명만).

**목표:** CSV 시트를 편집하고 `MSRPG > Import Character CSVs`를 실행하면 `CharacterDef`/`SkillDef`/`TagRuleTable` 에셋이 생성·갱신되고 `CharacterDatabase`에 반영된다. 오류는 **시트명·행 번호가 든 메시지**로 알려 준다.

## 구조 결정 (계획 단계 확정)

| 항목 | 결정 |
|---|---|
| 테스트 가능성 | 에디터 코드는 PlayMode 테스트에서 호출할 수 없다 → **파싱·매핑·검증을 런타임 어셈블리의 순수 로직**(`Scripts/Import/`)으로 만들고, 에디터 임포터는 파일 읽기·에셋 쓰기만 하는 **얇은 래퍼** |
| 빌더 출력 | 입력 `{시트명→CSV 텍스트}` → 출력 `ImportResult { characters, skills, tagRules, errors, warnings }`. 에셋이 아닌 **메모리 ScriptableObject**(`CreateInstance`)라 테스트가 직접 검사 |
| 에셋 갱신 | 같은 id의 에셋이 있으면 `EditorUtility.CopySerialized`로 **제자리 갱신**(GUID·참조 유지), 없으면 생성. 같은 CSV 재임포트는 결과 동일(멱등) |
| 에셋 경로 | `Assets/_Game/Data/Characters/{id}.asset`, `.../Skills/{skillId}.asset`(시드와 동일 폴더), `Assets/_Game/Resources/TagRuleTable.asset` |
| DB 갱신 | `CharacterDatabase`를 **덮어쓰지 않고** `Data/Characters`의 모든 `CharacterDef`를 모아 재구성(시드 + 임포트 병합). 시드 스크립트는 건드리지 않는다 |
| 시드 충돌 | 문서의 새 id(`forte_r` 등)는 시드 id(`light_ssr`·`mass_sr`·`time_sr`)와 달라 충돌 없음. 만약 같은 id가 있고 스킬 id가 달라지면 **경고**(기존 세이브의 해금 기록이 고아가 됨) |
| 삭제 | CSV에서 사라진 캐릭터·스킬 에셋은 **삭제하지 않고** 경고로 목록만 출력 |
| 해금 경고 | `startUnlocked=false`인데 `ProblemDatabase`에 그 스킬을 푸는 문제가 없으면 경고(스킬이 영구히 잠김) |

## CSV 공통 규칙

- UTF-8(BOM 허용), 쉼표 구분, `"` 로 감싸면 쉼표·줄바꿈 허용(`""` = 따옴표). 첫 칸이 `#`인 줄은 주석, 빈 줄 무시, 첫 비주석 줄 = 헤더
- 빈 칸 = 기본값. 리스트는 `;` 구분. 불리언 `true/false/1/0`(대소문자 무시). 실수는 `.` 소수점(Invariant). 색은 `#RRGGBB`
- enum 값은 **C# 이름 그대로**(`Physics`, `StateBased` …). 알 수 없는 값·형식 오류·필수 열 누락·알 수 없는 참조는 **오류**, 알 수 없는 추가 열은 **경고**
- 오류 형식: `[character_skills.csv:12] skillId=forte_s3: effectKind 'Foo'는 알 수 없는 값입니다 (허용: Strike, Aoe, HealBuff, Mark)`

## 단계

### A — CSV 코어(순수)
- A1 `CsvTable`(파서: 따옴표·줄바꿈·BOM·주석·빈 줄·CRLF/LF), 행 접근자 `CsvRow`(열 이름으로 `Str/Int/Float/Bool/List/Enum<T>/Color`, 필수 여부, 오류 수집) — `CsvTableTests`
- A2 `ImportReport`(오류·경고, 시트·행 번호 포맷)

### B — 시트 → 데이터 매핑(순수, `CharacterCsvBuilder`)
- B1 `tag_rules` → `TagRule[]`, `character_catalog` → `CharacterDef`(단일 폼 스탯·속성·성격·모션·portraitColor 등)
- B2 `character_forms`(기본 폼 1개·기본 폼 스탯이 `baseStats`)·`character_auto_rules` → `forms[]`/`autoRules[]`
- B3 `character_skills`(전 열) + `skill_effects`(효과 목록 / 패시브 효과 구분) → `SkillDef.effects`, 스킬을 `slot` 순으로 `CharacterDef.skills`에 배치
- B4 `skill_formchange` + `skill_formchange_states` → `FormChangeSpec`
- B5 `skill_passives` + `skill_modifiers` → `PassiveSpec`(스택·수정자·공식)
- B6 교차 검증: id 중복, 없는 ownerId/skillId/formId 참조, 슬롯 범위(0~6)와 같은 슬롯·같은 폼 중복, `FormChange` 스킬에 `skill_formchange` 행 필요, `Passive` 스킬에 `skill_passives` 행 필요, 기본 폼 정확히 1개, 자동 규칙·상태 규칙의 `toForm` 존재 — `CharacterCsvBuilderTests`(시트별 정상 매핑 + 오류 메시지)

### C — 에디터 래퍼
- C1 `CharacterCsvImporter`(`MSRPG > Import Character CSVs`): CSV 읽기 → 빌더 → 에셋 생성/제자리 갱신(스킬 → 캐릭터 순, 참조 연결), `TagRuleTable` 에셋, `CharacterDatabase` 병합 재구성, 경고 출력(삭제 후보·고아 가능성·해금 문제 없음), 배치 모드 진입점 `RunBatch`
- C2 배치 모드 실행으로 에셋 생성 확인(Unity를 닫은 상태에서 CLI 실행)

### D — 대표 캐릭터 이관 + 실파일 검증
- D1 `Resources/Data/`에 10개 CSV 헤더·범례 주석 + 대표 4명 이관: **힘**(단순·패시브 OnHitTaken)·**루미에르(빛)**(수동 토글 폼·폼별 스킬·패시브)·**마시오&이네르셔**(자동 폼·HP 이월·폼 한정 패시브·적 오라)·**크로노(시간)**(타격 공식·파티 존재 수정자)
- D2 `ShippedCsvTests`: 실제 CSV 파일을 읽어 빌더로 돌렸을 때 **오류 0**, 캐릭터·스킬·폼 수가 문서와 일치, 핵심 수치 몇 개(문서 대조)
- D3 문서로 **표현할 수 없는 항목 목록**(스키마 갭)을 정리해 설계도 18 §8에 기록

### E — 마무리
- 설계도 18 §8·메모리 갱신, push

## 완료 기준
- 대표 4명의 CSV가 오류 없이 임포트되어 에셋이 만들어지고, 실제 `CharacterDef`/`SkillDef` 값이 문서와 일치
- 잘못된 CSV는 시트명·행 번호가 든 오류로 보고되고 에셋을 쓰지 않는다(오류가 있으면 임포트 중단)
- 같은 CSV 재임포트 시 에셋 내용 불변(멱등), 기존 시드 에셋·`CharacterDatabase` 항목 유지
- 기존 501개 중 499 통과 유지
