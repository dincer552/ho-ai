(() => {
  const names = { Normal:'Normal', CounterAttack:'Kontra Atak', AttackMiddle:'Orta Saldırı', AttackWings:'Kanat Saldırısı', LongShots:'Uzun Şutlar', Pressing:'Pres', Creative:'Yaratıcı' };
  const pct = v => Number.isFinite(Number(v)) ? `${(Number(v) * 100).toFixed(1)}%` : '—';
  const num = v => Number.isFinite(Number(v)) ? Number(v).toFixed(2) : '—';
  const esc = v => String(v ?? '').replace(/[&<>\"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;',"'":'&#39;'}[c]));

  function ensureStyle() {
    if (document.getElementById('v5TacticComparisonStyle')) return;
    const s = document.createElement('style'); s.id = 'v5TacticComparisonStyle';
    s.textContent = `
      #v5TacticComparison{margin:18px 0;padding:16px;border:1px solid rgba(127,127,127,.28);border-radius:14px;background:rgba(127,127,127,.06);font-size:14px}
      #v5TacticComparison .v5tc-head{display:flex;justify-content:space-between;gap:12px;align-items:center;margin-bottom:10px;flex-wrap:wrap}
      #v5TacticComparison h3{margin:0;font-size:18px}
      #v5TacticComparison .v5tc-best{font-weight:700}
      #v5TacticComparison table{width:100%;border-collapse:collapse}
      #v5TacticComparison th,#v5TacticComparison td{padding:8px 7px;border-bottom:1px solid rgba(127,127,127,.18);text-align:right;white-space:nowrap}
      #v5TacticComparison th:first-child,#v5TacticComparison td:first-child{text-align:left}
      #v5TacticComparison tr.v5tc-best{font-weight:700}
      #v5TacticComparison tr.v5tc-selected{outline:1px solid currentColor;outline-offset:-1px}
      #v5TacticComparison .v5tc-note{margin-top:10px;opacity:.78;font-size:12px;line-height:1.45}
      #v5TacticComparison .v5tc-explain{white-space:normal;text-align:left;max-width:520px}
      @media(max-width:720px){#v5TacticComparison{overflow-x:auto}#v5TacticComparison table{min-width:680px}}
    `;
    document.head.appendChild(s);
  }

  function render(data) {
    const rows = Array.isArray(data?.tacticComparisons) ? data.tacticComparisons.filter(x => x && x.tacticEligible !== false) : [];
    if (!rows.length) return;
    ensureStyle();
    let panel = document.getElementById('v5TacticComparison');
    if (!panel) {
      panel = document.createElement('section'); panel.id = 'v5TacticComparison';
      (document.querySelector('main') || document.body).appendChild(panel);
    }
    const sorted = [...rows].sort((a,b) => Number(b.expectedPoints ?? -Infinity) - Number(a.expectedPoints ?? -Infinity));
    const best = sorted[0];
    const selected = String(data.selectedTactic || '');
    panel.innerHTML = `
      <div class="v5tc-head">
        <h3>⚽ Taktik Karşılaştırması — final XI</h3>
        <div class="v5tc-best">En iyi beklenen puan: ${esc(names[best.tactic] || best.tactic)} · ${num(best.expectedPoints)} puan</div>
      </div>
      <table><thead><tr><th>Taktik</th><th>Kazanma</th><th>Beklenen puan</th><th>Taktik fit</th><th>Ana metrik</th><th>Trade-off</th><th>Açıklama</th></tr></thead><tbody>
        ${sorted.map(r => `
          <tr class="${r === best ? 'v5tc-best ' : ''}${String(r.tactic) === selected ? 'v5tc-selected' : ''}">
            <td>${esc(names[r.tactic] || r.tactic)}${String(r.tactic) === selected ? ' ★' : ''}</td>
            <td>${pct(r.winProbability)}</td>
            <td>${num(r.expectedPoints)}</td>
            <td>${pct(r.tacticFitScore)}</td>
            <td>${pct(r.tacticPrimaryMetric)}</td>
            <td>${pct(r.tacticTradeoffCost)}</td>
            <td class="v5tc-explain">${esc(r.tacticExplanation || '')}</td>
          </tr>`).join('')}
      </tbody></table>
      <div class="v5tc-note">PDF/M8 referansı: Pres için taktik dönüşüm/suppression katsayısı %5–%41 aralığında; M8 bu paper katsayısını RT→TCR eşlemesi üzerinden uyguluyor. Bu panel seçimden bağımsız olarak final XI üzerindeki 7 taktiği sonuç metrikleriyle gösterir.</div>`;
  }

  const originalFetch = window.fetch;
  if (!originalFetch || window.__v5TacticComparisonInstalled) return;
  window.__v5TacticComparisonInstalled = true;
  window.fetch = async function(...args) {
    const response = await originalFetch.apply(this, args);
    try {
      const url = typeof args[0] === 'string' ? args[0] : args[0]?.url || '';
      if (String(url).includes('/api/v5/analysis')) response.clone().json().then(render).catch(() => {});
    } catch (_) {}
    return response;
  };
})();