const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";

async function upgradeBiomeDB() {
  try {
    // 1. 기존 속성 이름 변경 및 신규 속성 추가
    await axios.patch(`https://api.notion.com/v1/databases/${BIOME_DB_ID}`, {
      title: [{ type: "text", text: { content: "[DB] 월드 가이드 (바이옴 도감)" } }],
      properties: {
        "바이옴 명": { "name": "바이옴명" }, // 이름 변경
        "상태": { "name": "기획 상태", "status": { "options": [
          { "name": "📝 기획 중", "color": "gray" },
          { "name": "✨ 컨셉 확정", "color": "blue" },
          { "name": "🧱 레벨 조립 중", "color": "orange" },
          { "name": "✅ 최종 폴리싱", "color": "green" }
        ]}},
        "상세": { "name": "핵심 기믹" }, // 이름 변경
        "배경 담당": { "select": { "options": [] } },
        "기획 담당": { "select": { "options": [] } },
        "위험도": { "select": { "options": [
          { "name": "⭐", "color": "gray" },
          { "name": "⭐⭐", "color": "yellow" },
          { "name": "⭐⭐⭐", "color": "orange" },
          { "name": "⭐⭐⭐⭐", "color": "red" },
          { "name": "⭐⭐⭐⭐⭐", "color": "purple" }
        ]}}
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });

    // 2. '깊은숲' 샘플 항목 생성
    const pageRes = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: BIOME_DB_ID },
      properties: {
        "바이옴명": { title: [{ text: { content: "🌳 깊은숲 (Deep Forest)" } }] },
        "기획 상태": { status: { name: "✨ 컨셉 확정" } },
        "핵심 기믹": { rich_text: [{ text: { content: "독 안개 및 덩굴 그래플링 액션" } }] },
        "위험도": { select: { name: "⭐⭐" } }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });
    const pageId = pageRes.data.id;

    // 3. '깊은숲' 상세 기획 템플릿 삽입
    const blocks = [
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "📍 지역 개요" } }] }
      },
      {
        "object": "block",
        "type": "callout",
        "callout": {
          "rich_text": [{ "type": "text", "text": { "content": "오래전 정령들이 살았으나 현재는 마계의 기운에 침식된 거대 원시림. 빛이 거의 들지 않으며, 축축하고 어두운 분위기." } }],
          "icon": { "emoji": "🌲" },
          "color": "green_background"
        }
      },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "⚙️ 핵심 기믹" } }] }
      },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "type": "text", "text": { "content": "독 안개 구역: 특정 구역 진입 시 지속 대미지, 정령 폼으로 정화 가능." }, "annotations": { "bold": true } }] } },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "type": "text", "text": { "content": "덩굴 그래플링: 채찍이나 정령의 기운을 이용해 덩굴을 타고 이동." } }] } },
      { "object": "block", "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "type": "text", "text": { "content": "흔들리는 발판: 밟으면 3초 뒤 무너지는 이끼 낀 돌판." } }] } },
      {
        "object": "block",
        "type": "heading_2",
        "heading_2": { "rich_text": [{ "type": "text", "text": { "content": "⚔️ 주요 적 & 보스" } }] }
      },
      { "object": "block", "type": "to_do", "to_do": { "rich_text": [{ "type": "text", "text": { "content": "일반 적: 침식된 덩굴 괴물, 독 나방" } }] } },
      { "object": "block", "type": "to_do", "to_do": { "rich_text": [{ "type": "text", "text": { "content": "보스: [숲의 주인] 거대 포자 괴물" }, "annotations": { "color": "red" } }] } }
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

    console.log("Biome DB and Deep Forest sample setup complete.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

upgradeBiomeDB();
