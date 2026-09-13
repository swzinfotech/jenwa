// Progressive enhancement: all comparison topics remain readable without JS.
document.querySelectorAll('[data-house-explorer]').forEach(explorer => {
  const buttons = [...explorer.querySelectorAll('[data-house-topic]')];
  const panels = [...explorer.querySelectorAll('[data-house-panel]')];
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  let transition;
  function select(index) {
    transition?.cancel();
    buttons.forEach((button, i) => button.setAttribute('aria-pressed', String(i === index)));
    panels.forEach((panel, i) => { panel.hidden = i !== index; });
    if (!reducedMotion.matches) transition = panels[index].animate(
      [{ opacity: 0, transform: 'translateY(10px)' }, { opacity: 1, transform: 'translateY(0)' }],
      { duration: 280, easing: 'ease-out' }
    );
  }
  buttons.forEach((button, index) => {
    button.addEventListener('click', () => select(index));
    button.addEventListener('keydown', event => {
      let next;
      if (event.key === 'ArrowRight' || event.key === 'ArrowDown') next = (index + 1) % buttons.length;
      if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') next = (index + buttons.length - 1) % buttons.length;
      if (event.key === 'Home') next = 0;
      if (event.key === 'End') next = buttons.length - 1;
      if (next === undefined) return;
      event.preventDefault();
      buttons[next].focus();
      select(next);
    });
  });
  explorer.classList.add('is-interactive');
  select(0);
});

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
// Inquiry form: submits to the Azure Function that notifies the LINE bot (03_AzureFunction).
// The consultation sheet download stays available and remains entirely local.
(() => {
  const form = document.querySelector('#inquiry-form');
  if (!form) return;
  const status = document.querySelector('#form-status');
  const endpoint = document.querySelector('meta[name="inquiry-api"]')?.content || '';
  const labels = {name:'姓名',phone:'聯絡電話',email:'電子信箱',location:'基地縣市／地區',purpose:'規劃用途',message:'需求說明'};
  // How long the visitor spent on the form; the server treats an instant submit as automated.
  const openedAt = performance.now();
  const setStatus = (text, state) => { status.textContent = text; if (state) status.dataset.state = state; else delete status.dataset.state; };

  form.querySelector('[data-download]')?.addEventListener('click', () => {
    if (!form.reportValidity()) return;
    const data = new FormData(form);
    const text = ['易立構｜場勘諮詢單', '此檔案由您自行保存，尚未送出申請。', '', ...Object.entries(labels).map(([key,label]) => `${label}：${data.get(key) || '未填寫'}`)].join('\r\n');
    const url = URL.createObjectURL(new Blob(['\uFEFF',text], {type:'text/plain;charset=utf-8'}));
    const link = document.createElement('a'); link.href = url; link.download = '易立構-場勘諮詢單.txt'; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
    setStatus('諮詢單已產生，請保存下載檔案。此動作不會送出資料；要預約場勘請按「送出場勘諮詢」。');
  });

  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    if (!endpoint) { setStatus('目前無法送出，請改按「下載諮詢單」保存需求。', 'error'); return; }
    const data = new FormData(form);
    const payload = {elapsed: Math.round(performance.now() - openedAt)};
    for (const key of [...Object.keys(labels), 'company_url']) payload[key] = (data.get(key) || '').toString().trim();

    const submit = form.querySelector('button[type="submit"]');
    const label = submit.innerHTML;
    submit.disabled = true; submit.textContent = '送出中…';
    setStatus('正在送出您的需求…');
    try {
      const response = await fetch(endpoint, {method:'POST', headers:{'content-type':'application/json'}, body: JSON.stringify(payload)});
      const result = await response.json().catch(() => ({}));
      if (response.ok) {
        form.reset();
        setStatus('已收到您的諮詢，我們會盡快與您聯繫確認場勘時間。', 'ok');
      } else {
        setStatus(result.error || '送出失敗，請稍後再試，或改按「下載諮詢單」保存需求。', 'error');
      }
    } catch (error) {
      // fetch rejects identically for an offline network and for a blocked CORS response,
      // so log the real cause: a wrong Origin is otherwise indistinguishable from being offline.
      console.error('諮詢表單送出失敗', endpoint, error);
      setStatus('目前無法連線，請稍後再試，或改按「下載諮詢單」保存需求。', 'error');
    } finally {
      submit.disabled = false; submit.innerHTML = label;
    }
  });
})();

// Homepage: five image dots, styled for each site.
(() => {
  const hero = document.querySelector('main > .hero');
  const img = hero?.querySelector('img');
  if (!img) return;
  const slides = ["assets/hero/064c6f25-f428-4624-90b1-db024db8bfd8-right.png", "assets/hero/53b711f2-6464-48be-8b0e-d01b4af01e80-right.png", "assets/hero/595aa949-f336-4101-ba1e-92ed74d235e0-right.png", "assets/hero/hero-blue-house.png", "assets/hero/hero-courtyard.png"];
  const caption = hero.querySelector('figcaption');
  const bottom = hero.querySelector('.hero-bottom');
  const indexLabel = hero.querySelector('.hero-index > span');
  hero.classList.add('has-carousel');
  hero.setAttribute('aria-roledescription', '輪播');
  hero.setAttribute('aria-label', '生活空間主視覺');
  const controls = document.createElement('div');
  controls.className = 'hero-carousel-controls';
  controls.setAttribute('role', 'group');
  controls.setAttribute('aria-label', '選擇主視覺圖片');
  controls.innerHTML = slides.map((slide, i) => `<button type="button" data-slide="${i}" aria-label="第 ${i + 1} 張，共 5 張" aria-pressed="${i === 0}"></button>`).join('');
  hero.append(controls);
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  let current = 0, timer, hovering = false, request = 0, paused = reduced.matches;
  const syncTimer = () => {
    clearInterval(timer);
    if (!paused && !hovering && !document.hidden && !hero.contains(document.activeElement)) timer = setInterval(() => show(current + 1), 6000);
  };
  const render = () => {
    img.dataset.composition = 'right';
    img.src = slides[current];
    img.alt = `組合屋建築情境示意 ${current + 1}`;
    const number = String(current + 1).padStart(2, '0');
    controls.querySelectorAll('[data-slide]').forEach((button, i) => button.setAttribute('aria-pressed', String(i === current)));
    if (indexLabel) indexLabel.textContent = `${number} — 05`;
    if (caption) caption.innerHTML = `<span>SPACE STUDY / ${number}</span><span>組合屋建築情境示意</span>`;
    if (bottom) bottom.innerHTML = `<span>${number} <span class="line"></span> THE WAY WE LIVE</span><span>組合屋建築情境示意<br>SCROLL TO EXPLORE ↓</span>`;
  };
  const show = async (next) => {
    const token = ++request;
    const target = (next + slides.length) % slides.length;
    const preload = new Image();
    preload.src = slides[target];
    try { await preload.decode(); } catch { return; }
    if (token !== request) return;
    current = target;
    render();
    if (!reduced.matches) img.animate([{transform: 'translateX(-100%)', opacity: .35}, {transform: 'translateX(0)', opacity: 1}], {duration: 650, easing: 'ease-out'});
    syncTimer();
  };
  controls.querySelectorAll('[data-slide]').forEach(button => button.addEventListener('click', () => {
    paused = true;
    syncTimer();
    show(Number(button.dataset.slide));
  }));
  controls.addEventListener('keydown', event => {
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault();
      const next = (Number(event.target.dataset.slide) + (event.key === 'ArrowLeft' ? -1 : 1) + slides.length) % slides.length;
      const button = controls.querySelector(`[data-slide="${next}"]`);
      button.focus();
      button.click();
    }
  });
  hero.addEventListener('mouseenter', () => { hovering = true; syncTimer(); });
  hero.addEventListener('mouseleave', () => { hovering = false; syncTimer(); });
  hero.addEventListener('focusin', syncTimer);
  hero.addEventListener('focusout', () => setTimeout(syncTimer, 0));
  document.addEventListener('visibilitychange', syncTimer);
  reduced.addEventListener('change', () => { paused = reduced.matches; syncTimer(); });
  render();
  syncTimer();
})();
