const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

const roadmapUpdates = [
  { step: "1단계: 기초 시스템 및 워크플로우 구축", newTitle: "1단계: 기초 시스템 및 AI 노드 에디터 구축", start: "2026-05-01", end: "2026-08-31", detail: "디자이너 전용 AI 패턴 에디터 개발 및 프로그래머 병목 방지 파이프라인 구축", playtime: "1시간 (튜토리얼)" },
  { step: "2단계: 프로토타입 및 수직적 절단면 완성", newTitle: "2단계: 핵심 프로토타입 및 하드코어 전투 검증", start: "2026-09-01", end: "2027-08-31", detail: "패링/변신 '손맛' 조정 및 AI 노드 에디터 완성. 수직적 절단면(Vertical Slice) 확보", playtime: "3시간 (VS)" },
  { step: "3단계: 대규모 월드 확장 및 바이옴 병렬 제작 (Alpha)", newTitle: "3단계: 대규모 월드 확장 (14~16개 바이옴 Alpha)", start: "2027-09-01", end: "2029-04-30", detail: "4인 디자이너 병렬 작업으로 30시간 분량 월드 조립. 16개 바이옴 통폐합 및 고밀도화", playtime: "20시간 (Alpha)" },
  { step: "4단계: 고난도 보스전 설계 및 시스템 통합 (Beta)", newTitle: "4단계: 보스전 완성 및 조기 최적화 착수 (Beta)", start: "2029-05-01", end: "2030-01-31", detail: "7대죄 보스 기믹 구현. 스위치/PC 조기 빌드 테스트 및 성능 최적화 시작", playtime: "25시간 (Beta)" },
  { step: "5단계: 최종 폴리싱 및 밸런스 조정", newTitle: "5단계: 집중 밸런싱 및 FGT (난이도 미세 조정)", start: "2030-02-01", end: "2030-05-31", detail: "하드코어 유저 대상 FGT 진행 및 난이도 밸런싱. 타격감 및 연출 극대화", playtime: "30시간 (Final)" },
  { step: "6단계: 플랫폼 최적화 및 포팅 (PC/Switch)", newTitle: "6단계: 플랫폼 최적화 및 최종 포팅", start: "2030-06-01", end: "2030-07-15", detail: "스위치 고정 60프레임 목표 최적화 및 플랫폼별 전용 기능 구현", playtime: "30시간" },
  { step: "7단계: 글로벌 로컬라이징 및 QA", newTitle: "7단계: 글로벌 로컬라이징 및 최종 QA", start: "2030-07-16", end: "2030-08-15", detail: "30시간 분량 텍스트 다국어 지원 및 최종 버그 수정", playtime: "30시간" },
  { step: "8단계: 마케팅 캠페인 및 최종 데모 출시", newTitle: "8단계: 마케팅 및 출시 준비", start: "2030-08-16", end: "2030-08-31", detail: "Steam Next Fest 참여 및 최종 데모 공개", playtime: "-" },
  { step: "9단계: 글로벌 정식 출시 (2030.09)", newTitle: "9단계: 글로벌 정식 출시 (2030.09)", start: "2030-09-01", end: "2030-09-30", detail: "30시간의 장대한 여정 전 세계 동시 출시", playtime: "30시간+" }
];

async function updateRoadmap() {
  try {
    const response = await axios.post(`https://api.notion.com/v1/databases/${DATABASE_ID}/query`, {}, {
      headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
    });

    const items = response.data.results;

    for (const update of roadmapUpdates) {
      const item = items.find(i => i.properties["항목명"].title[0]?.plain_text === update.step);
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
