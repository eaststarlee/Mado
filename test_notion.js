const { Client } = require('@notionhq/client');

const notion = new Client({
  auth: "ntn_X5445282861aXRZrtqX0EBegiMzx9Qs6as1uDcjN3bU1tl"
});

(async () => {
  try {
    const response = await notion.search({
      query: "East Star Studio",
      filter: {
        property: "object",
        value: "page"
      }
    });
    console.log(JSON.stringify(response, null, 2));
  } catch (error) {
    console.error(error);
  }
})();
