const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const HUB_PAGE_ID = "35214490-6c2d-816e-9969-ca8dda94a64b";

async function createDevStatusDB() {
  try {
    // 1. 데이터베이스 생성
    const dbRes = await axios.post('https://api.notion.com/v1/databases', {
      parent: { type: "page_id", page_id: HUB_PAGE_ID },
      title: [{ type: "text", text: { content: "[DB] 개발 현황 / 예정" } }],
      properties: {
        "항목명": { "title": {} },
        "상태": { "select": { "options": [
          { "name": "✅ 완료", "color": "green" },
          { "name": "🚧 진행 중", "color": "blue" },
          { "name": "📌 예정", "color": "gray" },
          { "name": "⚠️ 보류", "color": "red" }
        ]}},
        "카테고리": { "select": { "options": [
          { "name": "시스템", "color": "purple" },
          { "name": "콘텐츠", "color": "orange" },
          { "name": "기술/최적화", "color": "blue" },
          { "name": "아트/연출", "color": "pink" },
          { "name": "서사", "color": "yellow" }
        ]}},
        "중요도": { "select": { "options": [
          { "name": "🔥 상", "color": "red" },
          { "name": "⚡ 중", "color": "yellow" },
          { "name": "☁️ 하", "color": "gray" }
        ]}},
        "상세 내용": { "rich_text": {} }
      }
    }, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
    });

    const dbId = dbRes.data.id;
    console.log("Database created:", dbId);

    // 2. 초기 데이터 등록
    const devItems = [
      { name: "핵심 FSM 아키텍처 수립 (Player/Pet)", status: "✅ 완료", cat: "시스템", priority: "🔥 상", detail: "확장 가능한 상태 머신 베이스 클래스 및 전환 로직 구축" },
      { name: "JSON 데이터 매니저 (Save/Load)", status: "🚧 진행 중", cat: "시스템", priority: "🔥 상", detail: "HashSet/List 기반 고속 세이브 데이터 처리 및 암호화 준비" },
      { name: "세키로식 패링 및 타격감 극한 폴리싱", status: "📌 예정", cat: "시스템", priority: "🔥 상", detail: "프레임 단위 판정 최적화 및 히트스탑/카메라 셰이크 연동" },
      { name: "14~16개 거대 바이옴 레벨 디자인", status: "📌 예정", cat: "콘텐츠", priority: "🔥 상", detail: "메트로배니아식 어빌리티 게이팅 및 탐험 루트 설계" },
      { name: "7대죄 보스전 기믹 및 AI 스크립팅", status: "📌 예정", cat: "콘텐츠", priority: "⚡ 중", detail: "각 보스별 고유 페이즈 전환 및 특수 연출 시스템" },
      { name: "비동기 씬 로딩 및 스위치 최적화", status: "📌 예정", cat: "기술/최적화", priority: "🔥 상", detail: "어드레서블 에셋 기반 메모리 관리 및 드로우콜 최적화" },
      { name: "폼 변신(정령/기사) 전투 시너지 시스템", status: "📌 예정", cat: "시스템", priority: "⚡ 중", detail: "실시간 변신을 통한 콤보 연계 및 전략적 전투 기믹" },
      { name: "글로벌 로컬라이징 파이프라인 구축", status: "📌 예정", cat: "서사", priority: "☁️ 하", detail: "다국어 텍스트 시트 연동 및 폰트 렌더링 최적화" }
    ];

    for (const item of devItems) {
      await axios.post('https://api.notion.com/v1/pages', {
        parent: { database_id: dbId },
        properties: {
          "항목명": { title: [{ text: { content: item.name } }] },
          "상태": { select: { name: item.status } },
          "카테고리": { select: { name: item.cat } },
          "중요도": { select: { name: item.priority } },
          "상세 내용": { rich_text: [{ text: { content: item.detail } }] }
        }
      }, {
        headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
      });
    }
    console.log("Initial dev items registered.");
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

createDevStatusDB();
