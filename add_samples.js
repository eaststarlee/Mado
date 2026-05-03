const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const MILESTONE_DB = "35214490-6c2d-8165-afc6-cd6671f75af2";
const AI_DB = "35214490-6c2d-8108-8e38-f72acddd42e2";

async function addMilestone() {
  await axios.post('https://api.notion.com/v1/pages', {
    parent: { database_id: MILESTONE_DB },
    properties: {
      "목표명": { title: [{ text: { content: "디자이너용 에디터 툴 구축 (Tilemap & Spawner)" } }] },
      "우선순위": { select: { name: "High" } },
      "상태": { status: { name: "In progress" } }
    }
  }, {
    headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
  });
}

async function addAIPattern() {
  await axios.post('https://api.notion.com/v1/pages', {
    parent: { database_id: AI_DB },
    properties: {
      "패턴 명": { title: [{ text: { content: "기본 적 AI (Patrol & Chase Module)" } }] },
      "복잡도": { select: { name: "Mid" } },
      "개발 상태": { select: { name: "Ready" } },
      "담당자": { select: { name: "이동규" } }
    }
  }, {
    headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Content-Type': 'application/json', 'Notion-Version': '2022-06-28' }
  });
}

async function run() {
  await addMilestone();
  await addAIPattern();
  console.log("Samples added.");
}

run();
