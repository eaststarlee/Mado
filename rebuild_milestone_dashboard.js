const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const MILESTONE_DB_ID = "35214490-6c2d-8165-afc6-cd6671f75af2";

async function rebuildDashboard() {
  try {
    const queryRes = await axios.post(`https://api.notion.com/v1/databases/${MILESTONE_DB_ID}/query`, {
      filter: { property: "목표명", title: { contains: "5월" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const pageId = queryRes.data.results[0]?.id;
    if (!pageId) return;

    const createToDo = (text) => ({ "type": "to_do", "to_do": { "rich_text": [{ "type": "text", "text": { "content": text } }] } });

    const blocks = [
      {
        "object": "block",
        "type": "heading_1",
        "heading_1": { "rich_text": [{ "type": "text", "text": { "content": "🎯 5월 핵심 목표 (Key Results)" } }], "color": "blue_background" }
      },
      createToDo("자연계 바이옴 비주얼 가이드 및 무드보드 확정"),
      createToDo("핵심 플레이어 FSM 아키텍처 설계 및 기술 문서화"),
      createToDo("디자이너용 데이터 시트(JSON) 연동 툴 1차 완성"),
      createToDo("전 팀원 주간 워크플로우 테스트 및 피드백 반영"),
      { "object": "block", "type": "divider", "divider": {} },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "📋 주간 세부 업무 현황 (담당자별)" } }] }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "type": "text", "text": { "content": "💻 이동규 (Lead Programmer)" }, "annotations": { "bold": true } }],
          "icon": { "emoji": "💻" },
          "color": "blue_background",
          "children": [
            createToDo("1주차: FSM 추상 클래스 및 상태 전환 엔진 구현"),
            createToDo("2주차: JSON 기반 StatManager 데이터 파이프라인 구축"),
            createToDo("3주차: 캐릭터 입력 핸들러 및 기본 이동 연동"),
            createToDo("4주차: 애니메이션 상태머신(Animator) 베이스라인 통합")
          ]
        }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "type": "text", "text": { "content": "🎨 조은비 (Lead Designer)" }, "annotations": { "bold": true } }],
          "icon": { "emoji": "🎨" },
          "color": "pink_background",
          "children": [
            createToDo("1주차: 메인 바이옴 3종 무드 및 라이팅 컨셉 확정"),
            createToDo("2주차: 주인공 캐릭터 폼 변신(정령/기사) 디자인 시트 완성"),
            createToDo("3주차: UI/UX 레이아웃 기초 설계 (체력바, 패링 게이지)"),
            createToDo("4주차: 인게임 폰트 및 가독성 테스트")
          ]
        }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "type": "text", "text": { "content": "🖌️ 박현우 (Level Designer)" }, "annotations": { "bold": true } }],
          "icon": { "emoji": "🖌️" },
          "color": "orange_background",
          "children": [
            createToDo("1주차: 튜토리얼 씬 기초 타일맵 레이아웃 작업"),
            createToDo("2주차: 주요 플랫폼 및 기믹(문, 스위치) 배치 테스트"),
            createToDo("3주차: 탐험 루트 및 숏컷 연결 구조 설계"),
            createToDo("4주차: 1단계 월드 안정성 테스트 및 오브젝트 정리")
          ]
        }
      }
    ];

    await axios.patch(`https://api.notion.com/v1/blocks/${pageId}/children`, {
      children: blocks
    }, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
      }
    });

    console.log("Professional Dashboard rebuild successful.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

rebuildDashboard();
