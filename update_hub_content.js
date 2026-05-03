const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";

const blocks = [
  {
    "object": "block",
    "type": "toggle",
    "toggle": {
      "rich_text": [{ "type": "text", "text": { "content": "👥 1. 팀 개요 (Team Overview)" }, "annotations": { "bold": true, "color": "blue" } }],
      "children": [
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "🔹 팀명: EAST STAR STUDIO", "link": null }, "annotations": { "bold": true } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "💻 이동규 (Lead Programmer): 메인 프로그래머, 시스템 아키텍처 총괄" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🎨 조은비 (Lead Designer): 아트 디렉션 및 비주얼 가이드" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🖌️ 박현우 (Designer): 레벨 디자인 및 환경 구축" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "✨ 신규 디자이너 2명: 에셋 생산 및 월드 데이터 조립" } }] } },
        { "type": "callout", "callout": { "rich_text": [{ "text": { "content": "💡 핵심 전략: 프로그래머 1인 병목 현상 방지를 위한 '데이터 주도형' 설계 지향" } }], "icon": { "emoji": "⚡" }, "color": "blue_background" } }
      ]
    }
  },
  {
    "object": "block",
    "type": "toggle",
    "toggle": {
      "rich_text": [{ "type": "text", "text": { "content": "🎯 2. 프로젝트 목표 및 개요 (Project Purpose & Overview)" }, "annotations": { "bold": true, "color": "green" } }],
      "children": [
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "📌 프로젝트명: Mado : Shadow Walker", "link": null }, "annotations": { "bold": true } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🕹️ 장르: 2D 메트로배니아 액션 판타지" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "📅 출시 목표: 2030년 9월 (PC Steam, Nintendo Switch)" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🚀 목표 스코프: 10~15시간 분량의 고밀도 하드코어 액션" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🏆 벤치마크: Hollow Knight, Ori 시리즈, Nine Sols" } }] } }
      ]
    }
  },
  {
    "object": "block",
    "type": "toggle",
    "toggle": {
      "rich_text": [{ "type": "text", "text": { "content": "⚔️ 3. 핵심 게임 시스템 (Core Mechanics)" }, "annotations": { "bold": true, "color": "orange" } }],
      "children": [
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "🌀 두 개의 세상과 균열 (Two Worlds & Rifts)", "link": null }, "annotations": { "bold": true, "color": "purple" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "자연계(정령)와 마계(악마)를 실시간으로 교차하며 탐험" } }] } },
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "🎭 폼 자유 변신 시스템 (Form Transformation)", "link": null }, "annotations": { "bold": true, "color": "red" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "노말 폼(정령): 속도 중심, 짧은 리치 / 악마 폼(기사): 파괴력 중심, 묵직한 공격" } }] } },
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "🛡️ 세키로식 전투 (Combat & Parrying)", "link": null }, "annotations": { "bold": true, "color": "orange" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "정밀 패링을 통한 게이지 충전 및 특수 공격 시전" } }] } },
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "🧚 동료 소악마 '릴리' (Companion Lily)", "link": null }, "annotations": { "bold": true, "color": "yellow" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "길 안내, 스토리텔링, 보석 장착을 통한 스킬 및 패시브 제공" } }] } }
      ]
    }
  },
  {
    "object": "block",
    "type": "toggle",
    "toggle": {
      "rich_text": [{ "type": "text", "text": { "content": "🏗️ 4. 기술 아키텍처 (Technical Architecture)" }, "annotations": { "bold": true, "color": "yellow" } }],
      "children": [
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🧩 Modular FSM: 상태(Idle, Move, Dash 등)를 모듈화하여 관리" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "🧠 Blackboard AI: 3-Layer(Interrupt, Decision, Execution) 적 AI 시스템" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "📦 Additive Scene Loading: Master 씬 기반의 비동기 맵 로드 시스템" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "💾 Save System: JSON 기반 고속 데이터 저장 및 복구(SaveManager)" } }] } }
      ]
    }
  },
  {
    "object": "block",
    "type": "toggle",
    "toggle": {
      "rich_text": [{ "type": "text", "text": { "content": "📜 5. 서사 및 레벨 디자인 스코프 (Narrative & Level Scope)" }, "annotations": { "bold": true, "color": "purple" } }],
      "children": [
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "📖 메인 서사", "link": null }, "annotations": { "bold": true } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "배신자 '넬'의 음모를 막기 위해 3개의 유물을 찾아 7대죄 악마를 처치하는 여정" } }] } },
        { "type": "paragraph", "paragraph": { "rich_text": [{ "text": { "content": "🗺️ 레벨 디자인 스코프", "link": null }, "annotations": { "bold": true } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "당초 16개 바이옴에서 10~12개 내외의 고밀도 바이옴으로 집중 통폐합" } }] } },
        { "type": "bulleted_list_item", "bulleted_list_item": { "rich_text": [{ "text": { "content": "마을 시스템 간소화: 필수 상점 기능 위주 연출로 리소스 확보" } }] } }
      ]
    }
  }
];

async function updatePage() {
  try {
    await axios.patch(`https://api.notion.com/v1/blocks/${PAGE_ID}/children`, {
      children: blocks
    }, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
      }
    });
    console.log("Page updated with toggles.");
  } catch (error) {
    console.error("Error updating page:", error.response ? error.response.data : error.message);
  }
}

updatePage();
