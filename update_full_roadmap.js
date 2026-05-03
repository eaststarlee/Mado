const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

const roadmapUpdates = [
  { step: "3단계: ", newTitle: "3단계: 대규모 월드 확장 및 바이옴 병렬 제작 (Alpha)", start: "2027-05-01", end: "2029-04-30" },
  { step: "4단계: ", newTitle: "4단계: 고난도 보스전 설계 및 시스템 통합 (Beta)", start: "2029-05-01", end: "2030-01-31" },
  { step: "5단계: ", newTitle: "5단계: 최종 폴리싱 및 밸런스 조정", start: "2030-02-01", end: "2030-05-31" },
  { step: "6단계: ", newTitle: "6단계: 플랫폼 최적화 및 포팅 (PC/Switch)", start: "2030-06-01", end: "2030-07-15" },
  { step: "7단계: ", newTitle: "7단계: 글로벌 로컬라이징 및 QA", start: "2030-07-16", end: "2030-08-15" },
  { step: "8단계: ", newTitle: "8단계: 마케팅 캠페인 및 최종 데모 출시", start: "2030-08-16", end: "2030-08-31" },
  { step: "9단계: ", newTitle: "9단계: 글로벌 정식 출시 (2030.09)", start: "2030-09-01", end: "2030-09-30" },
  { step: "10단계: ", newTitle: "10단계: 포스트 런칭 및 DLC 기획", start: "2030-10-01", end: "2030-12-31" }
];

async function updateRoadmap() {
  try {
    const response = await axios.post(`https://api.notion.com/v1/databases/${DATABASE_ID}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const items = response.data.results;

    for (const update of roadmapUpdates) {
      const item = items.find(i => i.properties["항목명"].title[0]?.plain_text === update.step);
      if (item) {
        await axios.patch(`https://api.notion.com/v1/pages/${item.id}`, {
          properties: {
            "항목명": { title: [{ text: { content: update.newTitle } }] },
            "기간": { date: { start: update.start, end: update.end } }
          }
        }, {
          headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
        });
        console.log(`Updated: ${update.newTitle}`);
      }
    }
  } catch (error) {
    console.error("Error updating roadmap:", error.response ? error.response.data : error.message);
  }
}

updateRoadmap();
