const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";
const AI_DB_ID = "35214490-6c2d-8108-8e38-f72acddd42e2";

async function createMonsterDB() {
  try {
    // 1. 몬스터 도감 데이터베이스 생성
    const dbRes = await axios.post('https://api.notion.com/v1/databases', {
      parent: { type: "page_id", page_id: HUB_PAGE_ID },
      title: [{ type: "text", text: { content: "[DB] 몬스터 도감 (Monster Bestiary)" } }],
      properties: {
        "몬스터명": { "title": {} },
        "출몰 바이옴": { "relation": { "database_id": BIOME_DB_ID, "single_property": {} } },
        "등급": { "select": { "options": [
          { "name": "일반", "color": "gray" },
          { "name": "엘리트", "color": "blue" },
          { "name": "보스", "color": "red" }
        ]}},
        "담당 디자이너": { "select": { "options": [] } },
        "애니메이션": { "multi_select": { "options": [
          { "name": "Idle", "color": "default" },
          { "name": "Move", "color": "blue" },
          { "name": "Attack", "color": "red" },
          { "name": "Hit", "color": "yellow" },
          { "name": "Death", "color": "gray" }
        ]}},
        "프레임 정보": { "rich_text": {} },
        "적용 AI 패턴": { "relation": { "database_id": AI_DB_ID, "single_property": {} } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });

    const dbId = dbRes.data.id;
    console.log("Monster DB created.");

    // 2. '애벌레' 샘플 등록
    const pageRes = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: dbId },
      properties: {
        "몬스터명": { title: [{ text: { content: "🐛 애벌레 (Larva)" } }] },
        "등급": { select: { name: "일반" } },
        "애니메이션": { multi_select: [{ name: "Idle" }, { name: "Move" }, { name: "Hit" }, { name: "Death" }] },
        "프레임 정보": { rich_text: [{ text: { content: "Idle(6f), Move(10f), Hit(4f), Death(8f)" } }] }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const pageId = pageRes.data.id;

    // 3. '애벌레' 내부 상세 기획 삽입
    const blocks = [
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🗒️ 기획 상세" } }] }
      },
      {
        "object": "block",
        "type": "bulleted_list_item",
        "bulleted_list_item": { "rich_text": [{ "text": { "content": "행동 양식: 평상시 바닥을 느리게 기어 다니다가 플레이어가 특정 범위 내로 들어오면 몸을 웅크린 뒤 약하게 도약하며 공격." } }] }
      },
      { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "특이사항: 처치 시 작은 산성 액체를 뿌리며 주변 플레이어에게 둔화 효과 디버프 부여." } }] } },
      
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎬 애니메이션 사양" } }] }
      },
      {
        "object": "block",
        "type": "table",
        "table": {
          "table_width": 3,
          "has_column_header": true,
          "children": [
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "상태" } }], [{ "type": "text", "text": { "content": "프레임 수" } }], [{ "type": "text", "text": { "content": "비고" } }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Idle" } }], [{ "type": "text", "text": { "content": "6f" } }], [{ "type": "text", "text": { "content": "루프 가능" } }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Move" } }], [{ "type": "text", "text": { "content": "10f" } }], [{ "type": "text", "text": { "content": "속도에 따라 재생 배속 조절" } }]] } }
          ]
        }
      },
      
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🧠 AI 로직 (모듈)" } }] }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "text": { "content": "사용 모듈: [기본 근접 AI] + [도약 공격 컴포넌트]. 감지 범위(Detection Range) 변수 값은 3.5f로 설정." } }],
          "icon": { "emoji": "🧠" },
          "color": "gray_background"
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

    console.log("Larva sample setup complete.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

createMonsterDB();
