const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";

async function createBossDB() {
  try {
    // 1. 보스 도감 데이터베이스 생성
    const dbRes = await axios.post('https://api.notion.com/v1/databases', {
      parent: { type: "page_id", page_id: HUB_PAGE_ID },
      title: [{ type: "text", text: { content: "[DB] 보스 도감 (Boss Encyclopedia)" } }],
      properties: {
        "보스명": { "title": {} },
        "출현 장소": { "relation": { "database_id": BIOME_DB_ID, "single_property": {} } },
        "페이즈 수": { "select": { "options": [{ "name": "1페이즈" }, { "name": "2페이즈" }, { "name": "3페이즈" }] } },
        "총 패턴 수": { "number": {} },
        "난이도": { "select": { "options": [
          { "name": "⭐", "color": "gray" },
          { "name": "⭐⭐", "color": "yellow" },
          { "name": "⭐⭐⭐", "color": "orange" },
          { "name": "⭐⭐⭐⭐", "color": "red" },
          { "name": "⭐⭐⭐⭐⭐", "color": "purple" }
        ]}},
        "제작 상태": { "status": { "options": [
          { "name": "📝 기획 중", "color": "gray" },
          { "name": "🤖 AI 구현 중", "color": "blue" },
          { "name": "✨ 연출/VFX", "color": "pink" },
          { "name": "⚖️ 밸런싱", "color": "yellow" },
          { "name": "✅ 완료", "color": "green" }
        ]}}
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });

    const dbId = dbRes.data.id;
    console.log("Boss DB created.");

    // 2. '거대 나방' 샘플 등록
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

    // 3. '거대 나방' 내부 상세 기획 템플릿 삽입
    const blocks = [
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "📜 보스 설정 및 서사" } }] }
      },
      {
        "object": "block",
        "type": "paragraph",
        "paragraph": { "rich_text": [{ "text": { "content": "깊은숲의 가장 높은 거대 나무 위에 서식하는 군주. 한때는 숲의 수호자였으나 현재는 포자에 중독되어 눈에 보이는 모든 것을 공격합니다. 거대한 날개짓으로 독가루를 뿌리는 것이 특징입니다." } }] }
      },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "⚔️ 페이즈별 상세 패턴" } }] }
      },
      {
        "object": "block",
        "type": "table",
        "table": {
          "table_width": 4,
          "has_column_header": true,
          "children": [
            { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "페이즈" }], [{ "plain_text": "패턴명" }], [{ "plain_text": "공격 방식" }], [{ "plain_text": "공략법(회피)" }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "1P" }], [{ "plain_text": "날개 후리기" }], [{ "plain_text": "전방 넓은 범위 휩쓸기" }], [{ "plain_text": "하단 대시로 회피" }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "1P" }], [{ "plain_text": "독 포자 발사" }], [{ "plain_text": "포물선 3연속 발사" }], [{ "plain_text": "거리를 벌리며 점프" }]] } },
            { "type": "table_row", "table_row": { "cells": [[{ "plain_text": "2P" }], [{ "plain_text": "독 가루 폭풍" }], [{ "plain_text": "맵 전체 독 안개 생성" }], [{ "plain_text": "정령 폼으로 정화막 생성" }]] } }
          ]
        }
      },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎬 애니메이션 및 VFX 사양" } }] }
      },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "패턴 [독 가루 폭풍] 시 날개 끝에서 밝은 보라색 파티클이 쏟아져 나와야 함." } }] } },
      { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "전환 연출: 2페이즈 진입 시 비명을 지르며 날개 색상이 붉게 변함 (셰이더 컬러 변경)." } }] } },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🧠 AI 및 전환 조건" } }] }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "text": { "content": "Phase Transition: HP 50% 미만 시 즉시 모든 패턴 중단 후 '폭주 연출' 트리거 실행. 이후 공격 속도 1.5배 증가." } }],
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

    console.log("Boss DB and sample setup complete.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

createBossDB();
