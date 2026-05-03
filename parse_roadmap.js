const fs = require('fs');

try {
  let content = fs.readFileSync('roadmap_current.json', 'utf8');
  // BOM 제거
  content = content.replace(/^\uFEFF/, '');
  const data = JSON.parse(content);
  const results = data.results.map(item => {
    return {
      id: item.id,
      title: item.properties["항목명"].title[0]?.plain_text,
      date: item.properties["기간"].date,
      status: item.properties["상태"].status?.name
    };
  });
  console.log(JSON.stringify(results, null, 2));
} catch (e) {
  console.error("Error:", e.message);
}
