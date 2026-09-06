const menu = document.querySelector('.menu-toggle');
const nav = document.querySelector('.nav');
function closeMenu() { nav.classList.remove('is-open'); menu.setAttribute('aria-expanded', 'false'); menu.textContent = '選單 ☰'; }
menu.addEventListener('click', () => { const open = nav.classList.toggle('is-open'); menu.setAttribute('aria-expanded', String(open)); menu.textContent = open ? '關閉 ×' : '選單 ☰'; });
document.addEventListener('keydown', event => { if (event.key === 'Escape' && nav.classList.contains('is-open')) { closeMenu(); menu.focus(); } });
nav.addEventListener('click', event => { if (event.target.closest('a')) closeMenu(); });
function filterCases() {
  const active=document.querySelector('.filter[aria-pressed="true"]')?.dataset.filter || 'all';
  const query=document.querySelector('#case-search')?.value.trim() || '';
  document.querySelectorAll('[data-category]').forEach(card => { card.hidden=(active!=='all' && card.dataset.category!==active) || !card.textContent.includes(query); });
  const count=document.querySelectorAll('[data-category]:not([hidden])').length;
  const status=document.querySelector('#filter-status');
  if(status)status.textContent=`顯示 ${count} 個案例`;
  const empty=document.querySelector('#case-empty');
  if(empty)empty.hidden=count!==0;
}
document.querySelectorAll('.filter').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('.filter').forEach(item => item.setAttribute('aria-pressed', String(item===button)));
  filterCases();
}));
document.querySelector('#case-search')?.addEventListener('input',filterCases);
document.querySelectorAll('[data-slide-to]').forEach(button=>button.addEventListener('click',()=>{
  document.querySelectorAll('[data-slide]').forEach(slide=>{slide.hidden=slide.dataset.slide!==button.dataset.slideTo;});
  document.querySelectorAll('[data-slide-to]').forEach(item=>item.setAttribute('aria-pressed',String(item===button)));
}));
const dialog=document.querySelector('#gallery-dialog');
let galleryTrigger=null;
function openGallery(id,trigger=null) {
  const template=document.getElementById(id);
  if(!(template instanceof HTMLTemplateElement)||!dialog)return;
  galleryTrigger=trigger;
  document.querySelector('#gallery-title').textContent=template.dataset.title;
  document.querySelector('#gallery-body').replaceChildren(template.content.cloneNode(true));
  if(!dialog.open)dialog.showModal();
  document.body.classList.add('dialog-open');
}
document.querySelectorAll('[data-open-gallery]').forEach(link=>link.addEventListener('click',event=>{
  event.preventDefault();
  history.replaceState(null,'',`#${link.dataset.openGallery}`);
  openGallery(link.dataset.openGallery,link);
}));
dialog?.querySelector('[data-close-dialog]').addEventListener('click',()=>dialog.close());
dialog?.addEventListener('click',event=>{if(event.target===dialog){const rect=dialog.getBoundingClientRect();if(event.clientX<rect.left||event.clientX>rect.right||event.clientY<rect.top||event.clientY>rect.bottom)dialog.close();}});
dialog?.addEventListener('close',()=>{
  document.body.classList.remove('dialog-open');
  if(document.getElementById(location.hash.slice(1)) instanceof HTMLTemplateElement)history.replaceState(null,'',location.pathname+location.search);
  galleryTrigger?.focus({preventScroll:true});
});
if(location.hash)openGallery(location.hash.slice(1));
window.addEventListener('hashchange',()=>{if(location.hash)openGallery(location.hash.slice(1));});
document.querySelector('#inquiry-form')?.addEventListener('submit', event => {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;
  const data = new FormData(form);
  const labels = {name:'姓名',phone:'聯絡電話',email:'電子信箱',location:'基地縣市／地區',purpose:'規劃用途',message:'需求說明'};
  const text = ['易立構｜場勘諮詢單', '此檔案由您自行保存，尚未送出申請。', '請透過 LINE @mop1712q 或 container@jenwa.com.tw 提供諮詢單，並確認場勘安排。', '聯絡電話：07-783-0770／0911-608-070', '', ...Object.entries(labels).map(([key,label]) => `${label}：${data.get(key) || '未填寫'}`)].join('\r\n');
  const url = URL.createObjectURL(new Blob(['\uFEFF',text], {type:'text/plain;charset=utf-8'}));
  const link = document.createElement('a'); link.href = url; link.download = '易立構-場勘諮詢單.txt'; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
  document.querySelector('#form-status').textContent = '諮詢單已產生。請將下載檔案透過 LINE @mop1712q 或 container@jenwa.com.tw 提供給我們；尚未自動送出場勘申請。';
});
