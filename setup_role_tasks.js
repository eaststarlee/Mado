const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const TASK_DB_ID = "35214490-6c2d-81c4-8f38-c2f925e34fae";

async function setupRoleTasks() {
  try {
    // 1. 부모 항목(5월) 찾기
    const monthQuery = await axios.post(`https://api.notion.com/v1/databases/${TASK_DB_ID}/query`, {
      filter: { property: "할 일", title: { contains: "2026.05" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });
    const monthId = monthQuery.data.results[0]?.id;

    // 2. 1주차 공통 항목 생성 (부모: 5월)
    const weekRes = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: TASK_DB_ID },
      properties: {
        "할 일": { title: [{ text: { content: "🗓️ 1주차 (공통 목표: 아키텍처 설계)" } }] },
        "상위 항목": { relation: [{ id: monthId }] },
        "주차": { select: { name: "1주차" } },
        "월": { select: { name: "2026.05" } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const weekId = weekRes.data.id;

    // 3. 팀원별 상세 작업 항목 생성 (부모: 1주차)
    const roleTasks = [
      { name: "💻 [개발] 이동규", desc: "핵심 플레이어 FSM 추상 클래스 구현 및 기술 문서화", cat: "시스템" },
      { name: "🎨 [디자인] 조은비", desc: "자연계 바이옴 비주얼 가이드 및 무드보드 작성", cat: "기획" },
      { name: "🖌️ [레벨] 박현우", desc: "튜토리얼 구역 기초 타일맵 배치 및 충돌체 테스트", cat: "레벨" },
      { name: "🆕 [디자인A]", desc: "배경 프랍(Prop) 기본 에셋 10종 제작", cat: "아트" },
      { name: "🆕 [디자인B]", desc: "적 몬스터 기본 애니메이션 시트 정리", cat: "아트" }
    ];

    for (const role of roleTasks) {
      await axios.post('https://api.notion.com/v1/pages', {
        parent: { database_id: TASK_DB_ID },
        properties: {
          "할 일": { title: [{ text: { content: `${role.name}: ${role.desc}` } }] },
          "상위 항목": { relation: [{ id: weekId }] },
          "카테고리": { select: { name: role.cat } },
          "월": { select: { name: "2026.05" } },
          "주차": { select: { name: "1주차" } }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
    }
    console.log("Role-based sub-tasks created for Week 1.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

setupRoleTasks();
