const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const TASK_DB_ID = "35214490-6c2d-81c4-8f38-c2f925e34fae";
const MILESTONE_DB_ID = "35214490-6c2d-8165-afc6-cd6671f75af2";

async function cleanAndRestructure() {
  try {
    // 1. 기존 태스크 삭제 (Query -> Delete)
    const queryRes = await axios.post(`https://api.notion.com/v1/databases/${TASK_DB_ID}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    for (const page of queryRes.data.results) {
      await axios.patch(`https://api.notion.com/v1/pages/${page.id}`, { archived: true }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
      });
    }
    console.log("Existing tasks cleared.");

    // 2. 속성 재구성
    await axios.patch(`https://api.notion.com/v1/databases/${TASK_DB_ID}`, {
      properties: {
        "월": { "select": { "options": [
          { "name": "2026.05", "color": "blue" },
          { "name": "2026.06", "color": "green" },
          { "name": "2026.07", "color": "yellow" },
          { "name": "2026.08", "color": "orange" }
        ]}},
        "주차": { "select": { "options": [
          { "name": "1주차", "color": "default" },
          { "name": "2주차", "color": "default" },
          { "name": "3주차", "color": "default" },
          { "name": "4주차", "color": "default" },
          { "name": "5주차", "color": "default" }
        ]}},
        "카테고리": { "select": { "options": [
          { "name": "시스템", "color": "purple" },
          { "name": "레벨", "color": "brown" },
          { "name": "AI", "color": "red" },
          { "name": "아트", "color": "pink" },
          { "name": "기획", "color": "gray" }
        ]}},
        "마일스톤": { "relation": { "database_id": MILESTONE_DB_ID, "single_property": {} } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    console.log("Task DB structure updated.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

cleanAndRestructure();
