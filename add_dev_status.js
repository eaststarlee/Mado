const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";

async function addDevStatus() {
  try {
    const blocks = [
      {
        "object": "block",
        "type": "heading_1",
        "heading_1": { "rich_text": [{ "type": "text", "text": { "content": "🚀 개발 현황 및 향후 계획 (Status & Plans)" } }], "color": "orange_background" }
      },
      {
        "object": "block",
        "type": "column_list",
        "column_list": {
          "children": [
            {
              "type": "column",
              "column": {
                "children": [
                  { "type": "heading_3", "heading_3": { "rich_text": [{ "text": { "content": "✅ 현재 개발 현황" } }] } },
                  { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "핵심 FSM 아키텍처 수립 (Player/Pet)" }, "annotations": { "bold": true } }] } },
                  { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "JSON 기반 데이터 매니저(Save/Load) 기초 완료" } }] } },
                  { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "12단계 초정밀 개발 로드맵 수립 완료" } }] } }
                ]
              }
            },
            {
              "type": "column",
              "column": {
                "children": [
                  { "type": "heading_3", "heading_3": { "rich_text": [{ "text": { "content": "📌 향후 개발 예정" } }] } },
                  { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "세키로식 패링 및 타격감 극한 폴리싱" }, "annotations": { "bold": true } }] } },
                  { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "14~16개 거대 바이옴 레벨 디자인 & 조립" } }] } },
                  { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "7대죄 보스전 기믹 및 AI 패턴 스크립팅" } }] } }
                ]
              }
            }
          ]
        }
      },
      {
        "object": "block",
        "type": "toggle",
        "toggle": {
          "rich_text": [{ "type": "text", "text": { "content": "🔍 전체 개발 백로그 (Detailed Backlog)" }, "annotations": { "bold": true } }],
          "children": [
            { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "[시스템] 입력 버퍼링, 물리 최적화, 폼 변신 시너지 로직" } }] } },
            { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "[레벨] 어빌리티 게이팅(이단 점프, 대시 등) 기반 숏컷 설계" } }] } },
            { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "[UI/UX] 실시간 맵 시스템, 보석 슬롯 스킬 커스터마이징" } }] } },
            { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "[기술] 비동기 씬 로딩 최적화, Switch 포팅 최적화" } }] } }
          ]
        }
      },
      { "object": "block", "type": "divider", "divider": {} }
    ];

    await axios.patch(`https://api.notion.com/v1/blocks/${HUB_PAGE_ID}/children`, {
      children: blocks
    }, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
      }
    });

    console.log("Dev Status & Plans added to Hub page.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

addDevStatus();
