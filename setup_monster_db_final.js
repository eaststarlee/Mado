const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";

async function createMonsterDB() {
  try {
    // 1. 몬스터 도감 데이터베이스 생성
    const dbRes = await axios.post('https://api.notion.com/v1/databases', {
      parent: { type: "page_id", page_id: HUB_PAGE_ID },
      title: [{ type: "text", text: { content: "[DB] 몬스터 도감 (Monster Encyclopedia)" } }],
      properties: {
        "몬스터명": { "title": {} },
        "출몰 바이옴": { "relation": { "database_id": BIOME_DB_ID, "single_property": {} } },
        "담당 디자이너": { "select": { "options": [] } },
        "애니메이션 종류": { "multi_select": { "options": [
          { "name": "Idle", "color": "default" },
          { "name": "Move", "color": "blue" },
          { "name": "Attack", "color": "red" },
          { "name": "Hit", "color": "yellow" },
          { "name": "Death", "color": "gray" }
        ]}},
        "프레임 정보": { "rich_text": {} },
        "사용 AI 모듈": { "select": { "options": [
          { "name": "기본 근접 AI", "color": "default" },
          { "name": "원거리 저격 AI", "color": "blue" },
          { "name": "비행 추적 AI", "color": "purple" },
          { "name": "도약 공격 AI", "color": "orange" }
        ]}},
        "핵심 기믹": { "rich_text": {} }
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
        "애니메이션 종류": { multi_select: [{ name: "Idle" }, { name: "Move" }, { name: "Hit" }] },
        "프레임 정보": { rich_text: [{ text: { content: "Idle(6f), Move(12f), Hit(4f)" } }] },
        "사용 AI 모듈": { select: { name: "도약 공격 AI" } },
        "핵심 기믹": { rich_text: [{ text: { content: "플레이어 접근 시 몸을 웅크린 후 도약 공격" } }] }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const pageId = pageRes.data.id;

    // 3. '애벌레' 내부 상세 기획 템플릿 삽입
    const blocks = [
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🗒️ 기획 상세" } }] }
      },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "공격 방식: 인지 범위(4m) 내 진입 시 1.5초 대기 후 플레이어 방향으로 포물선 도약." } }] } },
      { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "이동 경로: 지정된 플랫폼 내에서 좌우 왕복 순찰." } }] } },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎬 애니메이션 및 프레임 사양" } }] }
      },
      {
        "object": "block",
        "type": "table",
        "table": {
          "table_width": 3,
          "has_column_header": true,
          "children": [
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "상태" } }], [{ "type": "text", "text": { "content": "프레임" } }], [{ "type": "text", "text": { "content": "특이사항" } }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Idle" } }], [{ "type": "text", "text": { "content": "6f" } }], [{ "type": "text", "text": { "content": "숨쉬는 연출" } }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "type": "text", "text": { "content": "Attack_Ready" } }], [{ "type": "text", "text": { "content": "8f" } }], [{ "type": "text", "text": { "content": "몸을 압축하는 연출" } }]] } }
          ]
        }
      },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🧠 AI 모듈 및 로직" } }] }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "text": { "content": "기본 Patrol 모듈에 JumpAttack 컴포넌트를 추가하여 사용. 타겟 감지 시 States.Chase 대신 States.Charge로 전이." } }],
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

    console.log("Monster DB and Larva sample created.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

createMonsterDB();
