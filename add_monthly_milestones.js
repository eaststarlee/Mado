const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const ROADMAP_DB_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";
const MILESTONE_DB_ID = "35214490-6c2d-8165-afc6-cd6671f75af2";

const monthlyMilestones = [
  { title: "[5월] 아키텍처 정립 및 데이터 파이프라인 구축", date: "2026-05-31", priority: "High" },
  { title: "[6월] 디자이너 전용 에디터 툴(Level Tool) 완성", date: "2026-06-30", priority: "High" },
  { title: "[7월] 비주얼 AI 노드 에디터 엔진 개발", date: "2026-07-31", priority: "High" },
  { title: "[8월] 1단계 통합 워크플로우 검증 및 안정화", date: "2026-08-31", priority: "High" }
];

async function run() {
  try {
    // 1. 1단계 ID 찾기
    const roadmapRes = await axios.post(`https://api.notion.com/v1/databases/${ROADMAP_DB_ID}/query`, {
      filter: { property: "항목명", title: { contains: "1단계" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const phase1Id = roadmapRes.data.results[0]?.id;
    if (!phase1Id) {
      console.error("Phase 1 not found");
      return;
    }

    // 2. 월간 마일스톤 등록
    for (const m of monthlyMilestones) {
      await axios.post('https://api.notion.com/v1/pages', {
        parent: { database_id: MILESTONE_DB_ID },
        properties: {
          "목표명": { title: [{ text: { content: m.title } }] },
          "우선순위": { select: { name: m.priority } },
          "목표 날짜": { date: { start: m.date } },
          "상태": { status: { name: "Not started" } }
          // Roadmap Relation이 있다면 여기서 연결 (현재는 생략하거나 DB 구조 확인 후 추가)
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log(`Added: ${m.title}`);
    }
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

run();
