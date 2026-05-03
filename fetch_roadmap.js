const axios = require('axios');

const NOTION_TOKEN = "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl";
const DATABASE_ID = "35214490-6c2d-81ef-9b62-fce8d71e017a";

async function fetchRoadmap() {
  try {
    const response = await axios.post(`https://api.notion.com/v1/databases/${DATABASE_ID}/query`, {}, {
      headers: {
        'Authorization': `Bearer ${NOTION_TOKEN}`,
        'Notion-Version': '2022-06-28'
      }
    });
    const results = response.data.results.map(item => ({
      title: item.properties["항목명"].title[0]?.plain_text,
      date: item.properties["기간"].date,
      status: item.properties["상태"].status?.name
    }));
    console.log(JSON.stringify(results, null, 2));
  } catch (error) {
    console.error("Error:", error.response ? error.response.data : error.message);
  }
}

fetchRoadmap();
