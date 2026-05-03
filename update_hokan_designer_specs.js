const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";

async function updateHokanSpecs() {
  try {
    const searchDB = await axios.post('https://api.notion.com/v1/search', {
      query: "NPC 도감",
      filter: { value: "database", property: "object" }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const dbId = searchDB.data.results[0]?.id;
    
    const pageQuery = await axios.post(`https://api.notion.com/v1/databases/${dbId}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const pageId = pageQuery.data.results[0]?.id; // 첫 번째 페이지(호칸) 선택
    
    if (pageId) {
      const blocks = [
        { "object": "block", "type": "divider", "divider": {} },
        {
          "object": "block",
          "type": "heading_2",
          "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎨 아트 및 애니메이션 제작 사양" } }] }
        },
        {
          "object": "block",
          "type": "table",
          "table": {
            "table_width": 4,
            "has_column_header": true,
            "children": [
              { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "동작명" } }], [{ "type": "text", "text": { "content": "프레임" } }], [{ "type": "text", "text": { "content": "루프" } }], [{ "type": "text", "text": { "content": "연출 의도" } }]] } },
              { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Idle" } }], [{ "type": "text", "text": { "content": "12f" } }], [{ "type": "text", "text": { "content": "O" } }], [{ "type": "text", "text": { "content": "어깨를 들썩이며 거칠게 호흡" } }]] } },
              { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Talk" } }], [{ "type": "text", "text": { "content": "8f" } }], [{ "type": "text", "text": { "content": "O" } }], [{ "type": "text", "text": { "content": "입만 움직이는 것이 아니라 고개를 끄덕임" } }]] } },
              { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Work" } }], [{ "type": "text", "text": { "content": "24f" } }], [{ "type": "text", "text": { "content": "O" } }], [{ "type": "text", "text": { "content": "망치를 내리칠 때 불꽃 이펙트 연동 필수" } }]] } }
            ]
          }
        }
      ];

      await axios.patch(`https://api.notion.com/v1/blocks/${pageId}/children`, {
        children: blocks
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log("Hokan designer specs added successfully.");
    }
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

updateHokanSpecs();
