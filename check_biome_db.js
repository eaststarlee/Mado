const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const BIOME_DB_ID = "35214490-6c2d-81bc-bab3-e78091901a22";

async function checkDB() {
  const res = await axios.get(`https://api.notion.com/v1/databases/${BIOME_DB_ID}`, {
    headers: { 'Authorization': `Bearer ${NOTION_TOKEN}`, 'Notion-Version': '2022-06-28' }
  });
  console.log(JSON.stringify(res.data.properties, null, 2));
}

checkDB();
