const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";

async function createBossDB() {
  try {
    const searchDB = await axios.post('https://api.notion.com/v1/search', {
      query: "보스 도감",
      filter: { value: "database", property: "object" }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const dbId = searchDB.data.results[0]?.id;
    
    const pageRes = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: dbId },
      properties: {
        "보스명": { title: [{ text: { content: "🦋 [숲의 주인] 거대 나방" } }] },
        "페이즈 수": { select: { name: "2페이즈" } },
        "총 패턴 수": { number: 6 },
        "난이도": { select: { name: "⭐⭐⭐" } },
        "제작 상태": { status: { name: "🤖 AI 구현 중" } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const pageId = pageRes.data.id;

    const createRow = (texts) => ({
      "type": "table_row",
      "table_row": { "cells": texts.map(t => [{ "type": "text", "text": { "content": t } }]) }
    });

    const blocks = [
      {
        "object": "block", "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "📜 보스 설정 및 서사" } }] }
      },
      {
        "object": "block", "type": "paragraph",
        "paragraph": { "rich_text": [{ "type": "text", "text": { "content": "깊은숲의 군주. 포자에 오염되어 광폭화한 상태." } }] }
      },
      {
        "object": "block", "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "⚔️ 페이즈별 상세 패턴" } }] }
      },
      {
        "object": "block", "type": "table",
        "table": {
          "table_width": 4, "has_column_header": true,
          "children": [
            createRow(["페이즈", "패턴명", "공격 방식", "공략법"]),
            createRow(["1P", "날개 후리기", "전방 광범위 휩쓸기", "하단 대시"]),
            createRow(["1P", "독 포자 발사", "포물선 3연속 발사", "점프로 회피"]),
            createRow(["2P", "독 가루 폭풍", "전체 맵 장판 생성", "정령 폼 보호막"])
          ]
        }
      },
      {
        "object": "block", "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🧠 AI 및 전환 조건" } }] }
      },
      {
        "object": "block", "type": "callout",
        "callout": {
          "rich_text": [{ "type": "text", "text": { "content": "HP 50% 미만 시 2페이즈 진입 및 폭주 패턴 시작." } }],
          "icon": { "emoji": "🧠" }, "color": "gray_background"
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

    console.log("Boss DB sample setup complete.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

createBossDB();
