const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

const phases = [
  { name: "Phase 2: Prototype & Vertical Slice", select: "Phase 2: Prototype", start: "2026-11-01", end: "2027-04-30" },
  { name: "Phase 3: Production & Parallel Design", select: "Phase 3: Production", start: "2027-05-01", end: "2029-04-30" },
  { name: "Phase 4: Boss & Combat Master", select: "Phase 4: Boss & Combat", start: "2029-05-01", end: "2030-01-31" },
  { name: "Phase 5: Polish & Global Launch", select: "Phase 5: Polish & Launch", start: "2030-02-01", end: "2030-09-30" }
];

async function addPhase(phase) {
  try {
    const response = await axios.post('https://api.notion.com/v1/pages', {
      parent: { database_id: DATABASE_ID },
      properties: {
        "항목명": { title: [{ text: { content: phase.name } }] },
        "단계": { select: { name: phase.select } },
        "기간": { date: { start: phase.start, end: phase.end } },
        "상태": { status: { name: "Not started" } }
      }
    }, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
      }
    });
    console.log(`Added: ${phase.name}`);
  } catch (error) {
    console.error(`Error adding ${phase.name}:`, error.response ? error.response.data : error.message);
  }
}

async function run() {
  for (const phase of phases) {
    await addPhase(phase);
  }
}

run();
