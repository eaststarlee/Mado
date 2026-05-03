const fs = require('fs');
const data = JSON.parse(fs.readFileSync('db_list_new.json', 'utf8').replace(/^\uFEFF/, ''));
const dbs = data.results.map(db => ({
  title: db.title[0]?.plain_text,
  id: db.id
}));
console.log(JSON.stringify(dbs, null, 2));
