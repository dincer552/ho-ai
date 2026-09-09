(() => {
  const names = ['Normal','Kontra Atak','Orta Saldırı','Kanat Saldırısı','Uzun Şutlar','Pres','Yaratıcı'];
  const tacticName = value => {
    const n = Number(value);
    return Number.isInteger(n) && n >= 0 && n < names.length ? names[n] : String(value ?? '');
  };
  const pct = v => Number.isFinite(Number(v)) ? `${(Number(v) * 100).toFixed(1)}%` : '—';
  const num = v => Number.isFinite(Number(v)) ? Number(v).toFixed(2) : '—';
  const esc = v => String(v ?? '').replace(/[&<>\"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;',"'":'&#39;'}[c]));

  function ensureStyle() {
    if (document.getElementById('v5TacticComparisonStyle')) return;
    const s = document.createElement('style'); s.id = 'v5TacticComparisonStyle';
    s.textContent = `
      #v5TacticComparison{margin:0 0 14px;padding:14px 16px;border:1px solid #dce4df;border-radius:12px;background:#f7f9f7;font-size:13px}
      #v5TacticComparison .v5tc-head{display:block;margin-bottom:9px}
      #v5TacticComparison h3{margin:0 0 6px;font-size:16px}
      #v5TacticComparison .v5tc-best{font-weight:800;color:#21804a}
      #v5TacticComparison table{width:100%;border-collapse:collapse;table-layout:auto}
      #v5TacticComparison th,#v5TacticComparison td{padding:8px 6px;border-bottom:1px solid #e1e7e3;text-align:right;white-space:nowrap}
      #v5TacticComparison th:first-child,#v5TacticComparison td:first-child{text-align:left}
      #v5TacticComparison tr.v5tc-best{font-weight:800;background:#edf7ef}
      #v5TacticComparison tr.v5tc-selected{outline:1px solid #21804a;outline-offset:-1px}
      @media(max-width:720px){
        #v5TacticComparison{overflow:visible;padding:12px 10px}
        #v5TacticComparison table{min-width:0;width:100%;table-layout:fixed}
        #v5TacticComparison th,#v5TacticComparison td{padding:8px 4px;font-size:11px}
        #v5TacticComparison th:nth-child(1),#v5TacticComparison td:nth-child(1){width:32%;text-align:left;white-space:normal;overflow-wrap:anywhere}
        #v5TacticComparison th:nth-child(2),#v5TacticComparison td:nth-child(2){width:19%}
        #v5TacticComparison th:nth-child(3),#v5TacticComparison td:nth-child(3){width:23%}
        #v5TacticComparison th:nth-child(4),#v5TacticComparison td:nth-child(4){width:26%}
        #v5TacticComparison th:nth-child(n+5),#v5TacticComparison td:nth-child(n+5){display:none}
      }
    `;
    document.head.appendChild(s);
  }

  function mountPanel(panel) {
    const tacticRow = document.querySelector('.lineup-card:first-of-type .lineup-tactic');
    if (tacticRow && tacticRow.parentNode) {
      tacticRow.parentNode.insertBefore(panel, tacticRow.nextSibling);
      return;
    }
    const host = document.querySelector('main.page') || document.querySelector('main') || document.body;
    host.appendChild(panel);
  }

  function render(data) {
    const rows = Array.isArray(data?.tacticComparisons)
      ? data.tacticComparisons.filter(x => x && x.tacticEligible !== false)
      : [];
    if (!rows.length) return;
    ensureStyle();
    let panel = document.getElementById('v5TacticComparison');
    if (!panel) {
      panel = document.createElement('section');
      panel.id = 'v5TacticComparison';
    }
    mountPanel(panel);

    const sorted = [...rows].sort((a,b) =>
      Number(b.expectedPoints ?? -Infinity) - Number(a.expectedPoints ?? -Infinity) ||
      Number(b.winProbability ?? -Infinity) - Number(a.winProbability ?? -Infinity) ||
      Number(b.tacticFitScore ?? -Infinity) - Number(a.tacticFitScore ?? -Infinity));
    const best = sorted[0];
    const selected = String(data.selectedTactic || '');
    panel.innerHTML = `
      <div class="v5tc-head">
        <h3>⚽ Taktik Karşılaştırması</h3>
        <div class="v5tc-best">En yüksek beklenen puan: ${esc(tacticName(best.tactic))} · ${num(best.expectedPoints)}</div>
      </div>
      <table><thead><tr><th>Taktik</th><th>Kazanma</th><th>Beklenen puan</th><th>Taktik fit</th><th>Trade-off</th></tr></thead><tbody>
        ${sorted.map(r => `
          <tr class="${r === best ? 'v5tc-best ' : ''}${String(r.tactic) === selected ? 'v5tc-selected' : ''}">
            <td>${esc(tacticName(r.tactic))}${String(r.tactic) === selected ? ' ★' : ''}</td>
            <td>${pct(r.winProbability)}</td>
            <td>${num(r.expectedPoints)}</td>
            <td>${pct(r.tacticFitScore)}</td>
            <td>${pct(r.tacticTradeoffCost)}</td>
          </tr>`).join('')}
      </tbody></table>`;
  }

  if (window.__v5TacticComparisonInstalled) return;
  window.__v5TacticComparisonInstalled = true;
  window.addEventListener('v5:analysis-ready', e => render(e.detail));

  const originalFetch = window.fetch;
  if (!originalFetch) return;
  window.fetch = async function(...args) {
    const response = await originalFetch.apply(this, args);
    try {
      const url = typeof args[0] === 'string' ? args[0] : args[0]?.url || '';
      if (String(url).includes('/api/v5/analysis') && response.ok) {
        response.clone().json().then(data => {
          render(data);
          window.dispatchEvent(new CustomEvent('v5:analysis-ready', { detail:data }));
        }).catch(() => {});
      }
    } catch (_) {}
    return response;
  };
})();