const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";

async function upgradeBossMasterSheet() {
  try {
    const searchRes = await axios.post('https://api.notion.com/v1/search', {
      query: "보스 도감",
      filter: { value: "database", property: "object" }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const dbId = searchRes.data.results[0]?.id;
    if (!dbId) return;

    // 1. 속성 추가
    await axios.patch(`https://api.notion.com/v1/databases/${dbId}`, {
      properties: { "담당 디자이너": { "select": { "options": [] } } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    console.log("Properties updated.");

    const pageQuery = await axios.post(`https://api.notion.com/v1/databases/${dbId}/query`, {
      filter: { property: "보스명", title: { contains: "거대 나방" } }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const pageId = pageQuery.data.results[0]?.id;
    if (!pageId) return;

    const createRow = (texts) => ({
      "type": "table_row",
      "table_row": { "cells": texts.map(t => [{ "type": "text", "text": { "content": t } }]) }
    });

    // Chunk 1: 비주얼 가이드 & 애니메이션 사양표
    const chunk1 = [
      { "object": "block", "type": "divider", "divider": {} },
      {
        "object": "block", "type": "heading_1",
        "heading_1": { "rich_text": [{ "type": "text", "text": { "content": "🎨 비주얼 및 연출 가이드 (Visual Direction)" } }], "color": "blue_background" }
      },
      {
        "object": "block", "type": "callout",
        "callout": {
          "rich_text": [{ "text": { "content": "할로우 나이트의 '그림(Grimm)'과 같은 절도 있는 움직임 지향. 대기 시에도 날개가 미세하게 떨리는 2차 동작 필수. 모든 공격 전에는 반드시 명확한 선딜레이(Telegraphing) 모션 필수." } }],
          "icon": { "emoji": "🦋" }, "color": "gray_background"
        }
      },
      {
        "object": "block", "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🎬 패턴별 초정밀 애니메이션 사양 (60fps 기준)" } }] }
      },
      {
        "object": "block", "type": "table",
        "table": {
          "table_width": 6, "has_column_header": true,
          "children": [
            createRow(["패턴명", "선딜(f)", "공격(f)", "후딜(f)", "총 프레임", "VFX/연출 포인트"]),
            createRow(["급강하 돌진", "24f", "10f", "30f", "64f", "강하 시 소용돌이 이펙트"]),
            createRow(["독 포자 산탄", "18f", "12f", "20f", "50f", "포자 발사 시 화면 흔들림"]),
            createRow(["날개 휩쓸기", "20f", "8f", "25f", "53f", "궤적 잔상 효과(Trail)"])
          ]
        }
      }
    ];

    await axios.patch(`https://api.notion.com/v1/blocks/${pageId}/children`, { children: chunk1 }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    console.log("Chunk 1 added.");

    // Chunk 2: 히트박스 & 체크리스트
    const chunk2 = [
      {
        "object": "block", "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "💥 판정 및 물리 설계 (Hitbox Design)" } }] }
      },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "몸체 히트박스: 날개 끝부분은 피격 판정 제외 (불합리함 방지)." }, "annotations": { "bold": true } }] } },
      { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "공격 판정: 돌진 시 머리 부분에 원형 콜라이더 생성, 꼬리 쪽은 판정 약화." } }] } },
      {
        "object": "block", "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "🔊 사운드 및 VFX 체크리스트" } }] }
      },
      { "object": "block", "type": "to_do", "to_do": { "rich_text": [{ "text": { "content": "[VFX] 2페이즈 진입 시 배경에 독 안개 파티클 밀도 200% 증가" } }] } },
      { "object": "block", "type": "to_do", "to_do": { "rich_text": [{ "text": { "content": "[SFX] 날개짓 소리에 리버브(Reverb) 추가하여 거대함 강조" } }] } }
    ];

    await axios.patch(`https://api.notion.com/v1/blocks/${pageId}/children`, { children: chunk2 }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    console.log("Chunk 2 added.");

  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

upgradeBossMasterSheet();
