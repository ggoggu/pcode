# PenguinShot 프로젝트 기술 및 구현 보고서 아카이브 (Report)

이 디렉토리는 **PenguinShot (Unity 6 / 6000.3.19f1)** 프로젝트의 핵심 아키텍처 설계, 주요 기능 구현 내역, UI 반응형 분석, 독립 검증 보고서들을 체계적으로 분류·보관하는 아카이브입니다.

---

## 📁 디렉토리 구조 요약

```
Report/
├── Architecture/         # 시스템 전체 아키텍처 명세 및 거버넌스 보고서
│   ├── Diagrams/         # 핵심 아키텍처 및 상태 머신 다이어그램 이미지
│   │   ├── 01_overall_architecture.png
│   │   └── 02_game_loop_state_machine.png
│   ├── PenguinShot_Architecture_Overview.pdf
│   └── PenguinShot_SubAgent_Code_Audit_Architecture_Report.md
│
├── Features/             # 기획서/모듈별 세부 기능 구현 및 단위 테스트 검증 보고서
│   ├── 06_Object_System_Implementation_Report.pdf
│   ├── 07_Slot_DragAndDrop_Implementation_Report.pdf
│   ├── 08_Enemy_Death_And_Coin_Drop_Implementation_Report.pdf
│   ├── 09_Bench_System_Implementation_Report.pdf
│   ├── 10_Enemy_and_Waypoint_System_Implementation_Report.pdf
│   ├── 11_Penguin_Physics_and_Wall_Sliding_Implementation_Report.pdf
│   ├── 12_Enemy_Wave_and_AI_AntiOverlap_System_Implementation_Report.pdf
│   └── Slingshot_Feature_Implementation_Report.pdf
│
└── UI/                   # UI/UX 반응형 레이아웃 및 디바이스 해상도 대응 분석 보고서
    ├── TestScene_UI_Responsiveness_Analysis_Report.html
    ├── TestScene_UI_Responsiveness_Analysis_Report.pdf
    └── TestScene_UI_깨짐방지_구현분석_보고서.pdf
```

---

## 📑 분류별 세부 문서 명세

### 1. Architecture (시스템 아키텍처 및 거버넌스)

| 파일명 | 형식 | 설명 및 핵심 내용 | 작성/버전 |
| :--- | :---: | :--- | :--- |
| [`Diagrams/01_overall_architecture.png`](Architecture/Diagrams/01_overall_architecture.png) | PNG | **전체 아키텍처 다이어그램**: Pure C# Domain Tier, Presentation Tier, Data Tier의 3계층 분리 구조도 | 2026-09 |
| [`Diagrams/02_game_loop_state_machine.png`](Architecture/Diagrams/02_game_loop_state_machine.png) | PNG | **게임 루프 상태 머신 다이어그램**: 준비(Ready), 전투(Battle), 정산(Rest) 상태 전이 흐름도 | 2026-09 |
| [`PenguinShot_Architecture_Overview.pdf`](Architecture/PenguinShot_Architecture_Overview.pdf) | PDF (5p) | **핵심 서브시스템 구조 명세서**: Enemy, Object Storage, Placement 3대 모듈의 클래스 구조 및 라이프사이클 분석 | 2026-09 (v1.0.0) |
| [`PenguinShot_SubAgent_Code_Audit_Architecture_Report.md`](Architecture/PenguinShot_SubAgent_Code_Audit_Architecture_Report.md) | MD | **독립 서브에이전트 적대적 코드 심사 체계 보고서**: FC 프로젝트 벤치마킹, Unity 6 5대 심사 차원 및 `/interrogate` 연동 | 2026-11 |

---

### 2. Features (세부 기능 구현 보고서)

기획서 명세 번호 및 서브시스템별 구현 완료 내역과 단위 테스트 검증 보고서입니다.

| 파일명 | 대상 기획/모듈 | 핵심 구현 및 검증 내역 | 검증 상태 |
| :--- | :--- | :--- | :---: |
| [`06_Object_System_Implementation_Report.pdf`](Features/06_Object_System_Implementation_Report.pdf) | 기획 06 오브젝트 | 오브젝트 보관소, 드랍, 배치 파이프라인 정합성 점검 및 결함 해결 | Unit Test 5/5 Pass |
| [`07_Slot_DragAndDrop_Implementation_Report.pdf`](Features/07_Slot_DragAndDrop_Implementation_Report.pdf) | 기획 07 인벤토리 | 둥지(벤치 6칸) 및 인벤토리(9칸) 슬롯 드래그 앤 드롭/스왑 시스템 | Build Passed |
| [`08_Enemy_Death_And_Coin_Drop_Implementation_Report.pdf`](Features/08_Enemy_Death_And_Coin_Drop_Implementation_Report.pdf) | 기획 06 처치와 드랍 | 적 체력 0 도달 시 충돌체 비활성화 및 코인 드랍/획득 파이프라인 안정화 | Build Passed (v1.2.0) |
| [`09_Bench_System_Implementation_Report.pdf`](Features/09_Bench_System_Implementation_Report.pdf) | 벤치 시스템 UI/3D | 펭귄 합성 레벨(1~5단계) 등급별 테두리 색상 및 슬롯 뒤 3D 모델 프리뷰 렌더링 | Unit Test 100% Pass |
| [`10_Enemy_and_Waypoint_System_Implementation_Report.pdf`](Features/10_Enemy_and_Waypoint_System_Implementation_Report.pdf) | 적 AI 및 웨이포인트 | Catmull-Rom 스플라인 경로 추적, 동적 물리 넉백 FSM, 웨이브 연동 분석 | Unit Test 100% Pass (v1.4.0) |
| [`11_Penguin_Physics_and_Wall_Sliding_Implementation_Report.pdf`](Features/11_Penguin_Physics_and_Wall_Sliding_Implementation_Report.pdf) | 펭귄 물리 & 가이드 벽면 | 2D/3D 하이브리드 투영, GuideWall 슬라이딩 벡터 유도, PreStepVelocity 관성 보존 및 끼임 방지 | Unit Test 100% Pass (v1.5.0) |
| [`12_Enemy_Wave_and_AI_AntiOverlap_System_Implementation_Report.pdf`](Features/12_Enemy_Wave_and_AI_AntiOverlap_System_Implementation_Report.pdf) | 적 웨이브 & AI 겹침방지 | 스폰 시작점 Clearance 반경 큐잉 및 전방 부채꼴 시야각 기반 Kinematic Car-Following 감속 FSM | Unit Test 100% Pass (v1.6.0) |
| [`Slingshot_Feature_Implementation_Report.pdf`](Features/Slingshot_Feature_Implementation_Report.pdf) | 기획 07-2 슬링샷 | 단일 오브젝트 물리 궤적 계산, 발사 파이프라인 및 인벤토리 미소환 결함 수정 | Unit Test 6/6 Pass |

---

### 3. UI (반응형 레이아웃 분석)

모바일(세로 9:16 Portrait) 및 PC/태블릿 등 다양한 가로세로 비율과 디스플레이 해상도 환경에 대응하기 위한 UI 아키텍처 분석 보고서입니다.

| 파일명 | 형식 | 설명 |
| :--- | :---: | :--- |
| [`TestScene_UI_Responsiveness_Analysis_Report.html`](UI/TestScene_UI_Responsiveness_Analysis_Report.html) | HTML | 브라우저 인터랙티브 뷰어 (웹 반응형 보고서) |
| [`TestScene_UI_Responsiveness_Analysis_Report.pdf`](UI/TestScene_UI_Responsiveness_Analysis_Report.pdf) | PDF (7p) | TestScene UI 반응형 및 깨짐 방지 5단계 방어 아키텍처 분석 보고서 (영문 파일명) |
| [`TestScene_UI_깨짐방지_구현분석_보고서.pdf`](UI/TestScene_UI_깨짐방지_구현분석_보고서.pdf) | PDF (7p) | 국문 파일명 동일 문서 (SHA256 동일 바이너리 보관) |
