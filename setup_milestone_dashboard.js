const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const MILESTONE_DB_ID = "35214490-6c2d-8165-afc6-cd6671f75af2";

async function setupMilestoneDashboard() {
  try {
    const queryRes = await axios.post(`https://api.notion.com/v1/databases/${MILESTONE_DB_ID}/query`, {
      filter: { property: "목표명", title: { contains: "5월" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const pageId = queryRes.data.results[0]?.id;
    if (!pageId) {
      console.error("5월 Milestone page not found.");
      return;
    }

    const createRow = (texts) => ({
      "type": "table_row",
      "table_row": {
        "cells": texts.map(t => [{ "type": "text", "text": { "content": t } }])
      }
    });

    const blocks = [
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "📅 팀원별 주간 업무 계획 (5월)" } }] }
      },
      {
        "object": "block",
        "type": "table",
        "table": {
          "table_width": 5,
          "has_column_header": true,
          "has_row_header": false,
          "children": [
            createRow(["담당자", "1주차", "2주차", "3주차", "4주차"]),
            createRow(["💻 이동규 (개발)", "", "", "", ""]),
            createRow(["🎨 조은비 (디자인)", "", "", "", ""]),
            createRow(["🖌️ 박현우 (레벨)", "", "", "", ""]),
            createRow(["🆕 신규 디자인 A", "", "", "", ""]),
            createRow(["🆕 신규 디자인 B", "", "", "", ""])
          ]
        }
      }
    ];

    await axios.patch(`https://api.notion.com/v1/blocks/${pageId}/children`, {
      children: blocks
    }, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
      }
    });

    console.log("Milestone dashboard structure created successfully.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

setupMilestoneDashboard();
