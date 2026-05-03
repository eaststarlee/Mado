const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const ROADMAP_DB_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";
const MILESTONE_DB_ID = "35214490-6c2d-8165-afc6-cd6671f75af2";

async function updateRoadmap() {
  try {
    // 1. 기존 로드맵 항목 검색
    const searchResponse = await axios.post(`https://api.notion.com/v1/databases/${ROADMAP_DB_ID}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const items = searchResponse.data.results;
    const translations = {
      "Phase 1: Foundation & Workflow": "1단계: 기초 시스템 및 워크플로우 구축",
      "Phase 2: Prototype & Vertical Slice": "2단계: 프로토타입 및 수직적 절단면 완성",
      "Phase 3: Production & Parallel Design": "3단계: 본 제작 및 병렬 레벨 디자인",
      "Phase 4: Boss & Combat Master": "4단계: 보스전 고도화 및 콘텐츠 통합",
      "Phase 5: Polish & Global Launch": "5단계: 폴리싱 및 글로벌 출시 준비"
    };

    for (const item of items) {
      const currentTitle = item.properties["항목명"].title[0].plain_text;
      if (translations[currentTitle]) {
        await axios.patch(`https://api.notion.com/v1/pages/${item.id}`, {
          properties: { "항목명": { title: [{ text: { content: translations[currentTitle] } }] } }
        }, {
          headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
        });
        console.log(`Updated: ${currentTitle} -> ${translations[currentTitle]}`);
      }
    }
  } catch (error) {
    console.error("Error updating roadmap:", error.response ? error.response.data : error.message);
  }
}

async function addDetailedMilestones() {
  const milestones = [
    { title: "디자이너용 에디터 툴 개발 (타일맵/오브젝트 배치)", priority: "High" },
    { title: "핵심 플레이어 FSM 및 이동 프로토타입", priority: "High" },
    { title: "첫 번째 바이옴(튜토리얼) 완성도 100% 달성", priority: "High" },
    { title: "세키로식 패링 및 전투 메카닉 고도화", priority: "Medium" },
    { title: "10~12개 바이옴 월드 조립 (디자이너 병렬)", priority: "High" },
    { title: "모듈형 적 AI 패턴 라이브러리 확장 (50종)", priority: "Medium" },
    { title: "7대죄 보스 패턴 설계 및 스크립팅", priority: "High" }
  ];

  for (const m of milestones) {
    try {
      await axios.post('https://api.notion.com/v1/pages', {
        parent: { database_id: MILESTONE_DB_ID },
        properties: {
          "목표명": { title: [{ text: { content: m.title } }] },
          "우선순위": { select: { name: m.priority } },
          "상태": { status: { name: "Not started" } }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log(`Added Milestone: ${m.title}`);
    } catch (error) {
      console.error(`Error adding milestone ${m.title}:`, error.message);
    }
  }
}

async function run() {
  await updateRoadmap();
  await addDetailedMilestones();
}

run();
