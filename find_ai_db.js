const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";

async function findAIDB() {
  const res = await axios.post('https://api.notion.com/v1/search', {
    query: "AI",
    filter: { value: "database", property: "object" }
  }, {
    headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
  });
  
  console.log(JSON.stringify(res.data.results.map(db => ({ title: db.title[0]?.plain_text, id: db.id })), null, 2));
}

findAIDB();
