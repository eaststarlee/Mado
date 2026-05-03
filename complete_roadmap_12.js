const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

const newPhases = [
  { title: "11단계: 글로벌 로컬라이징 및 마케팅 자산 제작", start: "2030-08-01", end: "2030-08-31", detail: "다국어 번역 적용 및 Steam Next Fest 참여. 최종 트레일러 공개", playtime: "30시간" },
  { title: "12단계: 글로벌 정식 출시 (2030.09)", start: "2030-09-01", end: "2030-09-30", detail: "전 세계 동시 정식 출시 및 초기 데이원 패치 대응", playtime: "30시간+" }
];

async function updatePhase10AndAddMore() {
  try {
    const response = await axios.post(`https://api.notion.com/v1/databases/${DATABASE_ID}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });
    const items = response.data.results;

    // 10단계 업데이트
    const phase10 = items.find(i => i.properties["항목명"].title[0]?.plain_text.includes("10단계"));
    if (phase10) {
      await axios.patch(`https://api.notion.com/v1/pages/${phase10.id}`, {
        properties: {
          "항목명": { title: [{ text: { content: "10단계: 집중 밸런싱 및 하드코어 유저 FGT" } }] },
          "기간": { date: { start: "2030-06-01", end: "2030-07-31" } },
          "상세 내용": { rich_text: [{ text: { content: "하드코어 유저 대상 FGT 진행 및 난이도/패링 판정 미세 조정" } }] },
          "목표 플레이타임": { rich_text: [{ text: { content: "30시간" } }] }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log("Updated Phase 10");
    }

    // 11, 12단계 추가
    for (const phase of newPhases) {
      await axios.post('https://api.notion.com/v1/pages', {
        parent: { database_id: DATABASE_ID },
        properties: {
          "항목명": { title: [{ text: { content: phase.title } }] },
          "기간": { date: { start: phase.start, end: phase.end } },
          "상세 내용": { rich_text: [{ text: { content: phase.detail } }] },
          "목표 플레이타임": { rich_text: [{ text: { content: phase.playtime } }] },
          "단계": { select: { name: "Phase 5: Polish & Launch" } },
          "상태": { status: { name: "Not started" } }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log(`Added: ${phase.title}`);
    }
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

updatePhase10AndAddMore();
