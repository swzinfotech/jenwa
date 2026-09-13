import { mkdir, writeFile, copyFile, readFile, access } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { renderSite } from './render-site.mjs';
const root=path.dirname(fileURLToPath(import.meta.url));
const data=JSON.parse(await readFile(path.join(root,'official-content.json'),'utf8'));
const pages=renderSite(data);
await mkdir(path.join(root,'dist/client'),{recursive:true});
await mkdir(path.join(root,'dist/server'),{recursive:true});
for(const [name,html] of Object.entries(pages)) {
  await writeFile(path.join(root,`${name}.html`),html);
  await writeFile(path.join(root,'dist/client',`${name}.html`),html);
}
const assets=new Set(['styles.css','app.js']);
let checked=0;
for(const [name,html] of Object.entries(pages)) {
  const ids=[...html.matchAll(/\bid="([^"]+)"/g)].map(match=>match[1]);
  if(new Set(ids).size!==ids.length) throw Error(`Duplicate IDs in ${name}`);
  if(/src="https?:/.test(html)) throw Error(`Remote image/script in ${name}`);
  for(const [,href] of html.matchAll(/(?:href|src)="([^"]+)"/g)) {
    if(/^(https?:|tel:|mailto:|data:)/.test(href))continue;
    const [file,fragment]=href.split('#');
    const target=path.resolve(root,file||`${name}.html`);
    if(!target.startsWith(root+path.sep))throw Error(`Invalid path: ${href}`);
    await access(target);
    if(fragment){const content=await readFile(target,'utf8');if(!content.includes(`id="${fragment}"`))throw Error(`Missing fragment: ${name}: ${href}`);}
    if(file&&!file.endsWith('.html'))assets.add(file);
    checked++;
  }
}
for(const file of assets){await mkdir(path.dirname(path.join(root,'dist/client',file)),{recursive:true});await copyFile(path.join(root,file),path.join(root,'dist/client',file));}
await writeFile(path.join(root,'dist/server/index.js'),`export default { async fetch(request, env) { return env.ASSETS.fetch(request); } };\n`);
const casesCount=(pages.cases.match(/class="case-card"/g)||[]).length;
if(casesCount!==5)throw Error(`Expected 5 featured cases, got ${casesCount}`);
await writeFile(path.join(root,'validation-report.json'),JSON.stringify({pages:Object.keys(pages),featuredCases:casesCount,localReferences:checked,assets:assets.size,faq:data.faq.reduce((total,g)=>total+g.items.length,0),process:data.process.length,models:data.models.length,patents:data.patents.length,remoteImages:0},null,2));
console.log(`Built ${Object.keys(pages).length} pages; ${casesCount} featured cases; checked ${checked} local references; packaged ${assets.size} assets.`);