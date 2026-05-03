const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

const roadmapUpdates = [
  { step: "3단계", newTitle: "3단계: 월드 기초 조립 및 주요 이동 기믹 구현", start: "2027-09-01", end: "2028-02-29", detail: "자연계/마계 1~4번 바이옴 기초 배치 및 활공/그래플링 기믹 완성", playtime: "5시간" },
  { step: "4단계", newTitle: "4단계: 바이옴 확장 및 환경 아트 통합 (디자이너 병렬)", start: "2028-03-01", end: "2028-08-31", detail: "5~8번 바이옴 확장 및 배경/라이팅 고도화. 디자이너 4인 병렬 작업", playtime: "10시간" },
  { step: "5단계", newTitle: "5단계: 월드 연결 및 서사 흐름 구축 (Alpha)", start: "2028-09-01", end: "2029-02-28", detail: "9~12번 바이옴 완성 및 전체 구역 연결. 탐험 루트 밸런싱", playtime: "20시간 (Alpha)" },
  { step: "6단계", newTitle: "6단계: 중반부 콘텐츠 및 보석 스킬 시스템 구축", start: "2029-03-01", end: "2029-06-30", detail: "릴리 연계 보석 장착 시스템 및 상점/마을 연출 통합", playtime: "22시간" },
  { step: "7단계", newTitle: "7단계: 7대죄 보스전 기믹 설계 및 스크립팅 (Beta)", start: "2029-07-01", end: "2029-12-31", detail: "메인 보스 7종 및 중보스 패턴 완성. 변신 기믹 전투 최적화", playtime: "25시간 (Beta)" },
  { step: "8단계", newTitle: "8단계: 최종 콘텐츠 완성 및 다중 엔딩 시스템", start: "2030-01-01", end: "2030-03-31", detail: "최종 보스전 및 엔딩 연출. 플레이타임 30시간 최종 검증", playtime: "30시간" },
  { step: "9단계", newTitle: "9단계: 조기 최적화 및 플랫폼 빌드 테스트 (Switch)", start: "2030-04-01", end: "2030-05-31", detail: "스위치/PC 조기 최적화 및 맵 데이터 최적화", playtime: "30시간" },
  { step: "10단계", newTitle: "10단계: 집중 밸런싱 및 하드코어 유저 FGT", start: "2030-06-01", end: "2030-07-31", detail: "하드코어 유저 대상 FGT 진행 및 난이도 미세 조정", playtime: "30시간" }
];

async function updateRoadmap() {
  try {
    const response = await axios.post(`https://api.notion.com/v1/databases/${DATABASE_ID}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const items = response.data.results;

    for (const update of roadmapUpdates) {
      const item = items.find(i => i.properties["항목명"].title[0]?.plain_text.includes(update.step));
      if (item) {
        await axios.patch(`https://api.notion.com/v1/pages/${item.id}`, {
          properties: {
            "항목명": { title: [{ text: { content: update.newTitle } }] },
            "기간": { date: { start: update.start, end: update.end } },
            "상세 내용": { rich_text: [{ text: { content: update.detail } }] },
            "목표 플레이타임": { rich_text: [{ text: { content: update.playtime } }] }
          }
        }, {
          headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
        });
        console.log(`Updated: ${update.newTitle}`);
      }
    }
  } catch (error) {
    console.error("Error updating roadmap:", error.response ? error.response.data : error.message);
  }
}

updateRoadmap();
