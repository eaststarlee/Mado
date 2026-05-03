const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const TASK_DB_ID = "35214490-6c2d-81c4-8f38-c2f925e34fae";

async function enableSubItems() {
  try {
    // 1. 자기 참조(Self-relation) 속성 추가 (single_property 방식)
    await axios.patch(`https://api.notion.com/v1/databases/${TASK_DB_ID}`, {
      properties: {
        "상위 항목": {
          "relation": {
            "database_id": TASK_DB_ID,
            "single_property": {}
          }
        }
      }
    }, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
      }
    });
    console.log("Sub-items relation added.");

    // 2. 2026.05 부모 항목 생성
    const parentRes = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: TASK_DB_ID },
      properties: {
        "할 일": { title: [{ text: { content: "📅 2026.05 (5월 전체 일정)" } }] },
        "월": { select: { name: "2026.05" } },
        "상태": { status: { name: "Not started" } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const parentId = parentRes.data.id;

    // 3. 주간 자식 항목 생성 및 부모 연결
    const weeklyTasks = [
      "1주차: 핵심 플레이어 FSM 및 상태 전환 로직 설계",
      "2주차: JSON 데이터 시트(StatManager) 연동 시스템 개발",
      "3주차: 디자이너용 타일맵 워크플로우 확정 및 테스트 씬 구축",
      "4주차: 캐릭터 기본 애니메이션(Idle/Move) 연동 및 입력 핸들러 작업"
    ];

    for (const task of weeklyTasks) {
      await axios.post('https://api.notion.com/v1/pages', {
        parent: { database_id: TASK_DB_ID },
        properties: {
          "할 일": { title: [{ text: { content: task } }] },
          "월": { select: { name: "2026.05" } },
          "상위 항목": { relation: [{ id: parentId }] },
          "상태": { status: { name: "Not started" } }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
    }
    console.log("Weekly sub-items created and linked to May 2026.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

enableSubItems();
