const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";

async function updateAllDBProperties() {
  try {
    // 1. 모든 데이터베이스 검색
    const searchRes = await axios.post('https://api.notion.com/v1/search', {
      filter: { value: "database", property: "object" }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const dbs = searchRes.data.results;
    console.log(`Found ${dbs.length} databases.`);

    for (const db of dbs) {
      const dbId = db.id;
      const dbTitle = db.title[0]?.plain_text || "Untitled";
      const properties = db.properties;
      const updates = {};

      // 변경할 속성 이름들
      const targetProps = ["담당 디자이너", "배경 담당", "기획 담당", "담당자"];

      for (const propName of targetProps) {
        if (properties[propName]) {
          // select 타입을 multi_select로 변경
          updates[propName] = { "multi_select": { "options": properties[propName].select?.options || [] } };
        }
      }

      if (Object.keys(updates).length > 0) {
        await axios.patch(`https://api.notion.com/v1/databases/${dbId}`, {
          properties: updates
        }, {
          headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
        });
        console.log(`Updated properties in: ${dbTitle}`);
      }
    }

    console.log("All targeted properties updated to Multi-select.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

updateAllDBProperties();
