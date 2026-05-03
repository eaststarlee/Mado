const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";

async function updateNPCDB() {
  try {
    // 1. NPC 도감 ID 찾기
    const searchRes = await axios.post('https://api.notion.com/v1/search', {
      query: "NPC 도감",
      filter: { value: "database", property: "object" }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const dbId = searchRes.data.results[0]?.id;
    if (!dbId) {
      console.error("NPC DB not found.");
      return;
    }

    // 2. 디자이너용 속성 추가 및 수정
    await axios.patch(`https://api.notion.com/v1/databases/${dbId}`, {
      properties: {
        "담당 디자이너": { "select": { "options": [] } },
        "필요 애니메이션": { "multi_select": { "options": [
          { "name": "Idle(대기)", "color": "default" },
          { "name": "Talk(대화)", "color": "blue" },
          { "name": "Walk(이동)", "color": "green" },
          { "name": "Action(특수)", "color": "red" },
          { "name": "Reaction(반응)", "color": "yellow" }
        ]}},
        "프레임 가이드": { "rich_text": {} }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    console.log("Designer properties added to NPC DB.");

    // 3. '호칸' 페이지 ID 찾기 및 내용 업데이트 (디자이너 관점)
    const pageQuery = await axios.post(`https://api.notion.com/v1/databases/${dbId}/query`, {
      filter: { property: "NPC명", title: { contains: "호칸" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const pageId = pageQuery.data.results[0]?.id;
    if (pageId) {
      // 속성 업데이트
      await axios.patch(`https://api.notion.com/v1/pages/${pageId}`, {
        properties: {
          "필요 애니메이션": { multi_select: [{ name: "Idle(대기)" }, { name: "Talk(대화)" }, { name: "Action(특수)" }] },
          "프레임 가이드": { rich_text: [{ text: { content: "대기(12f), 대화(8f), 강화 작업(24f)" } }] }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });

      // 상세 내용 추가 (디자이너 관점 섹션)
      const blocks = [
        { "object": "block", "type": "divider", "divider": {} },
        {
          "object": "block",
          "type": "heading_2",
          "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎨 아트 및 애니메이션 제작 사양" } }] }
        },
        {
          "object": "block",
          "type": "callout",
          "callout": {
            "rich_text": [{ "text": { "content": "비주얼 컨셉: 상체 근육이 발달한 거구의 노인. 커다란 대장장이 앞치마를 두르고 있으며, 한쪽 눈은 안대로 가려져 있음." } }],
            "icon": { "emoji": "🖌️" },
            "color": "blue_background"
          }
        },
        {
          "object": "block",
          "type": "table",
          "table": {
            "table_width": 4,
            "has_column_header": true,
            "children": [
              { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "동작명" }], [{ "plain_text": "프레임" }], [{ "plain_text": "루프" }], [{ "plain_text": "연출 의도" }]] } },
              { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "Idle" }], [{ "plain_text": "12f" }], [{ "plain_text": "O" }], [{ "plain_text": "어깨를 들썩이며 거칠게 호흡" }]] } },
              { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "Talk" }], [{ "plain_text": "8f" }], [{ "plain_text": "O" }], [{ "plain_text": "입만 움직이는 것이 아니라 고개를 끄덕임" }]] } },
              { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "Work" }], [{ "plain_text": "24f" }], [{ "plain_text": "O" }], [{ "plain_text": "망치를 내리칠 때 불꽃 이펙트 연동 필수" }]] } }
            ]
          }
        }
      ];

      await axios.patch(`https://api.notion.com/v1/blocks/${pageId}/children`, {
        children: blocks
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
      console.log("Hokan designer specs updated.");
    }
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

updateNPCDB();
