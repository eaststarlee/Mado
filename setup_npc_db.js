const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";

async function createNPCDB() {
  try {
    // 1. NPC 도감 데이터베이스 생성
    const dbRes = await axios.post('https://api.notion.com/v1/databases', {
      parent: { type: "page_id", page_id: HUB_PAGE_ID },
      title: [{ type: "text", text: { content: "[DB] NPC 도감 (NPC Encyclopedia)" } }],
      properties: {
        "NPC명": { "title": {} },
        "주요 위치": { "relation": { "database_id": BIOME_DB_ID, "single_property": {} } },
        "유형": { "select": { "options": [
          { "name": "💰 상인", "color": "orange" },
          { "name": "🤝 조력자", "color": "blue" },
          { "name": "📜 퀘스트 NPC", "color": "yellow" },
          { "name": "👑 스토리 핵심", "color": "red" }
        ]}},
        "분기점 존재": { "checkbox": {} },
        "퀘스트 상태": { "status": { "options": [
          { "name": "미발견", "color": "gray" },
          { "name": "진행 중", "color": "blue" },
          { "name": "완료", "color": "green" },
          { "name": "실패/사망", "color": "red" }
        ]}}
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });

    const dbId = dbRes.data.id;
    console.log("NPC DB created.");

    // 2. 샘플 NPC '호칸' 등록
    const pageRes = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: dbId },
      properties: {
        "NPC명": { title: [{ text: { content: "🛠️ 늙은 대장장이 호칸" } }] },
        "유형": { select: { name: "💰 상인" } },
        "분기점 존재": { checkbox: true },
        "퀘스트 상태": { status: { name: "진행 중" } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const pageId = pageRes.data.id;

    // 3. 내부 상세 기획 템플릿 삽입
    const blocks = [
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "📜 배경 및 상세 설정" } }] }
      },
      {
        "object": "block",
        "type": "paragraph",
        "paragraph": { "rich_text": [{ "type": "text", "text": { "content": "호칸은 과거 왕실의 수석 대장장이였으나, 마계의 침공 당시 가족을 잃고 현재는 깊은숲 입구에서 은거하고 있습니다. 플레이어의 무기를 강화해주며 초반부 서사의 가이드 역할을 합니다. 말투는 무뚝뚝하지만 주인공에게서 죽은 아들의 모습을 봅니다." } }] }
      },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎁 관련 이벤트 & 퀘스트" } }] }
      },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "[메인] 부서진 검의 복구: 튜토리얼 종료 후 호칸에게 말을 걸면 발생. 재료: 철광석 5개." } }] } },
      { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "[서브] 잃어버린 망치: 깊은숲 내부 숨겨진 구역에서 호칸의 가업인 망치를 찾아 전달." } }] } },
      
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🌳 서사 분기 및 선택지" } }] }
      },
      {
        "object": "block",
        "type": "table",
        "table": {
          "table_width": 3,
          "has_column_header": true,
          "children": [
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "선택지 / 조건" } }], [{ "type": "text", "text": { "content": "결과 (서사 변화)" } }], [{ "type": "text", "text": { "content": "보상 및 영향" } }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "망치를 돌려준다" } }], [{ "type": "text", "text": { "content": "마을로 복귀하여 상점을 엽니다." } }], [{ "type": "text", "text": { "content": "강화 비용 20% 할인" } }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "망치를 팔아버린다" } }], [{ "type": "text", "text": { "content": "절망하여 숲을 떠나며 행방불명됩니다." } }], [{ "type": "text", "text": { "content": "강화 불가, 즉시 골드 획득" } }]] } }
          ]
        }
      },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "✨ 기타 특이사항" } }] }
      },
      { "object": "block", "type": "quote", "quote": { "rich_text": [{ "text": { "content": "특수 대사: '그 검의 무게를 견딜 준비가 되었나?' (무기 3단계 강화 시)" } }] } }
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

    console.log("NPC DB and sample setup complete.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

createNPCDB();
