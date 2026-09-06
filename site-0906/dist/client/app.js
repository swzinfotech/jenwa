const menu = document.querySelector('.menu-toggle');
const nav = document.querySelector('.nav');
function closeMenu() { nav.classList.remove('is-open'); menu.setAttribute('aria-expanded', 'false'); menu.textContent = '選單 ☰'; }
menu.addEventListener('click', () => { const open = nav.classList.toggle('is-open'); menu.setAttribute('aria-expanded', String(open)); menu.textContent = open ? '關閉 ×' : '選單 ☰'; });
document.addEventListener('keydown', event => { if (event.key === 'Escape' && nav.classList.contains('is-open')) { closeMenu(); menu.focus(); } });
nav.addEventListener('click', event => { if (event.target.closest('a')) closeMenu(); });
document.querySelectorAll('.filter').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('.filter').forEach(item => item.setAttribute('aria-pressed', String(item === button)));
  document.querySelectorAll('[data-category]').forEach(card => { card.hidden = button.dataset.filter !== 'all' && card.dataset.category !== button.dataset.filter; });
  document.querySelector('#filter-status').textContent = `顯示 ${document.querySelectorAll('[data-category]:not([hidden])').length} 個空間提案`;
}));
document.querySelector('#inquiry-form')?.addEventListener('submit', event => {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;
  const data = new FormData(form);
  const labels = {name:'姓名',phone:'聯絡電話',email:'電子信箱',location:'基地縣市／地區',purpose:'規劃用途',message:'需求說明'};
  const text = ['易立構｜場勘諮詢單', '此檔案由您自行保存，尚未送出申請。', '', ...Object.entries(labels).map(([key,label]) => `${label}：${data.get(key) || '未填寫'}`)].join('\r\n');
  const url = URL.createObjectURL(new Blob(['\uFEFF',text], {type:'text/plain;charset=utf-8'}));
  const link = document.createElement('a'); link.href = url; link.download = '易立構-場勘諮詢單.txt'; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
  document.querySelector('#form-status').textContent = '諮詢單已產生，請保存下載檔案。目前尚未送出場勘申請，您的資料不會傳送至伺服器。';
});

// Homepage hero carousel: preserve each design's original image and layout.
(() => {
  const hero = document.querySelector('main > .hero');
  const img = hero?.querySelector('img');
  if (!img) return;
  const caption = hero.querySelector('figcaption');
  const bottom = hero.querySelector('.hero-bottom');
  const indexLabel = hero.querySelector('.hero-index > span');
  const originalCaption = caption?.innerHTML;
  const originalBottom = bottom?.innerHTML;
  const slides = [
    { src: img.getAttribute('src'), alt: img.alt },
    { src: 'assets/hero-blue-house.png', alt: '藍色雙層組合屋、陽台與木柵欄的素色庭院建築情境示意' }
  ];
  hero.classList.add('has-carousel');
  hero.setAttribute('aria-roledescription', '輪播');
  hero.setAttribute('aria-label', '生活空間主視覺');
  const controls = document.createElement('div');
  controls.className = 'hero-carousel-controls';
  controls.innerHTML = '<button type="button" data-prev aria-label="上一張">←</button><button type="button" data-slide="0" aria-label="第 1 張" aria-pressed="true">01</button><button type="button" data-slide="1" aria-label="第 2 張" aria-pressed="false">02</button><button type="button" data-next aria-label="下一張">→</button><button type="button" data-pause>暫停</button>';
  hero.append(controls);
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  let current = 0, paused = reduced.matches, timer, hovering = false, request = 0;
  const pause = controls.querySelector('[data-pause]');
  const syncTimer = () => {
    clearInterval(timer);
    pause.textContent = paused ? '播放' : '暫停';
    pause.setAttribute('aria-label', paused ? '播放輪播' : '暫停輪播');
    if (!paused && !hovering && !document.hidden && !hero.contains(document.activeElement)) timer = setInterval(() => show(current + 1), 6000);
  };
  const show = async (next) => {
    const token = ++request;
    const target = (next + slides.length) % slides.length;
    const preload = new Image();
    preload.src = slides[target].src;
    try { await preload.decode(); } catch { return; }
    if (token !== request) return;
    current = target;
    img.src = slides[current].src;
    img.alt = slides[current].alt;
    img.classList.toggle('is-blue-house', current === 1);
    if (!reduced.matches) img.animate([{ opacity: .35 }, { opacity: 1 }], { duration: 650 });
    controls.querySelectorAll('[data-slide]').forEach((button, i) => button.setAttribute('aria-pressed', String(i === current)));
    if (indexLabel) indexLabel.textContent = '0' + (current + 1) + ' — 02';
    if (caption) caption.innerHTML = current ? '<span>SPACE STUDY / 02</span><span>雙層住宅 · 建築情境示意</span>' : originalCaption;
    if (bottom) bottom.innerHTML = current ? '<span>02 <span class="line"></span> THE WAY WE LIVE</span><span>雙層住宅・建築情境示意<br>SCROLL TO EXPLORE ↓</span>' : originalBottom;
    syncTimer();
  };
  if (indexLabel) indexLabel.textContent = '01 — 02';
  controls.querySelector('[data-prev]').addEventListener('click', () => show(current - 1));
  controls.querySelector('[data-next]').addEventListener('click', () => show(current + 1));
  controls.querySelectorAll('[data-slide]').forEach(button => button.addEventListener('click', () => show(Number(button.dataset.slide))));
  pause.addEventListener('click', () => { paused = !paused; syncTimer(); });
  controls.addEventListener('keydown', event => {
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault();
      show(current + (event.key === 'ArrowLeft' ? -1 : 1));
    }
  });
  hero.addEventListener('mouseenter', () => { hovering = true; syncTimer(); });
  hero.addEventListener('mouseleave', () => { hovering = false; syncTimer(); });
  hero.addEventListener('focusin', syncTimer);
  hero.addEventListener('focusout', () => setTimeout(syncTimer, 0));
  document.addEventListener('visibilitychange', syncTimer);
  reduced.addEventListener('change', () => { paused = reduced.matches; syncTimer(); });
  const preload = new Image();
  preload.src = slides[1].src;
  syncTimer();
})();
