const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

async function updatePhase10() {
  try {
    const response = await axios.post(`https://api.notion.com/v1/databases/${DATABASE_ID}/query`, {
      filter: { property: "항목명", title: { contains: "10단계" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const item = response.data.results[0];
    if (item) {
      await axios.patch(`https://api.notion.com/v1/pages/${item.id}`, {
        properties: {
          "항목명": { title: [{ text: { content: "10단계: 포스트 런칭 및 DLC 기획" } }] },
          "기간": { date: { start: "2030-10-01", end: "2030-12-31" } },
          "상세 내용": { rich_text: [{ text: { content: "신규 보스, 폼 및 추가 바이옴을 포함한 DLC 로드맵 수립 및 사후 지원" } }] },
          "목표 플레이타임": { rich_text: [{ text: { content: "30시간 + Alpha" } }] }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log("Updated: 10단계");
    }
  } catch (error) {
    console.error("Error updating phase 10:", error.message);
  }
}

updatePhase10();
