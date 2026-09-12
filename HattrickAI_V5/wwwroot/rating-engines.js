(function () {
  'use strict';

  const API = {
    engines: '/api/v5/rating-engines',
    selection: '/api/v5/rating-engine/selection',
    selected: '/api/v5/rating-engine/selected',
    compare: '/api/v5/rating-engines/compare'
  };

  let comparison = null;

  async function json(url, options) {
    const response = await fetch(url, { cache: 'no-store', credentials: 'same-origin', ...(options || {}) });
    const text = await response.text();
    let data = {};
    try { data = text ? JSON.parse(text) : {}; } catch (_) { throw new Error('Rating engine yanıtı geçersiz.'); }
    if (!response.ok) throw new Error(data.message || data.title || ('HTTP ' + response.status));
    return data;
  }

  function esc(value) {
    return String(value == null ? '' : value).replace(/[&<>\"']/g, function (m) {
      return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '\"': '&quot;', "'": '&#039;' })[m];
    });
  }

  function fmt(value) {
    if (value == null || !Number.isFinite(Number(value))) return '—';
    return Number(value).toFixed(2).replace(/\.00$/, '');
  }

  function panel() {
    let el = document.getElementById('ratingEnginePanel');
    if (el) return el;
    const host = document.querySelector('section.panel.analysis');
    if (!host) return null;
    el = document.createElement('section');
    el.id = 'ratingEnginePanel';
    el.className = 'panel';
    el.style.cssText = 'margin-top:14px;overflow:hidden;background:#fff;border-radius:15px;box-shadow:0 2px 9px #0002';
    el.innerHTML = '<div style="padding:14px 18px;border-bottom:1px solid #edf1ee;display:flex;align-items:center;justify-content:space-between"><div><div style="font-size:10px;font-weight:900;letter-spacing:.07em;color:#7c837f">RATING ENGINE</div><div style="font-size:20px;font-weight:800;margin-top:3px">Motor seçimi</div></div><select id="ratingEngineSelect" style="border:1px solid #cfdad3;border-radius:9px;background:#f7faf8;padding:9px 10px;font-size:12px;font-weight:800;color:#27322d"></select></div><div style="padding:13px 15px"><div id="ratingEngineSummary" style="font-size:11px;color:#6f7772;margin-bottom:9px">V5 varsayılan motor olarak kalır.</div><div style="overflow:auto"><table style="width:100%;border-collapse:collapse;font-size:11px;min-width:680px"><thead><tr style="text-align:center;background:#f5f8f5"><th style="padding:8px;text-align:left">Motor</th><th>DEF-L</th><th>DEF-C</th><th>DEF-R</th><th>MID</th><th>ATT-L</th><th>ATT-C</th><th>ATT-R</th><th>HatStats</th><th>Loddar</th></tr></thead><tbody id="ratingEngineRows"></tbody></table></div></div>';
    host.insertAdjacentElement('afterend', el);
    return el;
  }

  function render() {
    const el = panel();
    if (!el || !comparison) return;
    const select = el.querySelector('#ratingEngineSelect');
    const summary = el.querySelector('#ratingEngineSummary');
    const rows = el.querySelector('#ratingEngineRows');
    if (!select.options.length) {
      select.innerHTML = comparison.rows.map(function (row) {
        return '<option value="' + esc(row.engine) + '">' + esc(row.name) + '</option>';
      }).join('');
    }
    select.value = comparison.selected || 'V5';
    const selectedRow = comparison.rows.find(function (row) { return row.engine === select.value; }) || comparison.rows[0];
    summary.textContent = selectedRow ? selectedRow.name + ' seçili • aynı XI ve aynı rating context üzerinde hesaplandı. V5 referans olarak korunuyor.' : 'V5 varsayılan motor olarak kalır.';
    rows.innerHTML = comparison.rows.map(function (row) {
      const active = row.engine === select.value;
      return '<tr style="border-top:1px solid #edf1ee;background:' + (active ? '#eef7f0' : '#fff') + '">' +
        '<td style="padding:8px;font-weight:900">' + esc(row.name) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.leftDefence) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.centralDefence) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.rightDefence) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.midfield) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.leftAttack) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.centralAttack) + '</td>' +
        '<td style="text-align:center">' + fmt(row.rating.rightAttack) + '</td>' +
        '<td style="text-align:center">' + fmt(row.hatStats) + '</td>' +
        '<td style="text-align:center">' + fmt(row.loddarStats) + '</td>' +
        '</tr>';
    }).join('');
  }

  async function load() {
    try {
      const engines = await json(API.engines);
      const host = panel();
      if (!host) return;
      const select = host.querySelector('#ratingEngineSelect');
      if (!select.options.length) select.innerHTML = (engines.engines || []).map(function (item) {
        return '<option value="' + esc(item.id) + '">' + esc(item.name) + '</option>';
      }).join('');
      const state = await json(API.selection);
      select.value = state.selected || engines.default || 'V5';
    } catch (_) {}
  }

  async function refreshComparison() {
    try {
      comparison = await json(API.compare);
      render();
      window.__v5RatingEngineComparison = comparison;
    } catch (_) {
      comparison = null;
    }
  }

  async function choose(engine) {
    try {
      await json(API.selection, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ engine: engine }) });
      await refreshComparison();
      const result = await json(API.selected);
      const analysis = window.__v5LastAnalysis;
      if (analysis && result && result.rating && typeof window.makePitch === 'function') {
        window.makePitch('ownPitch', analysis.own, result.rating);
        const rt = document.getElementById('runtime');
        if (rt) rt.textContent = result.engine + ' • aynı XI yeniden rating edildi';
      }
    } catch (error) {
      const errorBox = document.getElementById('error');
      if (errorBox) { errorBox.textContent = error.message; errorBox.style.display = 'block'; }
    }
  }

  function installAnalysisCapture() {
    const originalFetch = window.fetch.bind(window);
    window.fetch = async function (input, init) {
      const response = await originalFetch(input, init);
      try {
        const url = typeof input === 'string' ? input : (input && input.url) || '';
        if (url.indexOf('/api/v5/analysis') !== -1 && response.ok) {
          response.clone().json().then(function (data) {
            window.__v5LastAnalysis = data;
            window.setTimeout(refreshComparison, 50);
          }).catch(function () {});
        }
      } catch (_) {}
      return response;
    };
  }

  function boot() {
    installAnalysisCapture();
    panel();
    load().then(function () {
      const el = document.getElementById('ratingEnginePanel');
      const select = el && el.querySelector('#ratingEngineSelect');
      if (select && !select.dataset.bound) {
        select.dataset.bound = '1';
        select.addEventListener('change', function () { choose(select.value); });
      }
    });
    window.setTimeout(refreshComparison, 1000);
    window.setInterval(refreshComparison, 10000);
  }

  window.v5RefreshRatingEngines = refreshComparison;
  window.v5RenderRatingEngines = render;
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot, { once: true });
  else boot();
})();
