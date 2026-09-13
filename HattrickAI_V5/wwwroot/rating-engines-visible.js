(function () {
  'use strict';
  function boot() {
    if (typeof window.v5RefreshRatingEngines === 'function') window.v5RefreshRatingEngines();
    var own = document.getElementById('ownPitch');
    if (!own) return;
    var card = own.closest('.lineup-card');
    if (!card) return;
    var panel = document.getElementById('ratingEnginePanel');
    if (!panel) {
      panel = document.createElement('section');
      panel.id = 'ratingEnginePanel';
      panel.className = 'panel';
      panel.style.cssText = 'margin-bottom:14px;overflow:hidden;background:#fff;border-radius:15px;box-shadow:0 2px 9px #0002;display:block';
      panel.innerHTML = '<div style="padding:14px 18px;border-bottom:1px solid #edf1ee"><div style="font-size:10px;font-weight:900;letter-spacing:.07em;color:#7c837f">RATING ENGINE</div><div style="font-size:20px;font-weight:800;margin-top:3px">Önerilen kadronun motor karşılaştırması</div><div id="ratingEngineSummary" style="font-size:11px;color:#6f7772;margin-top:6px">Analiz sonrası motor sonuçları getiriliyor…</div></div><div style="padding:13px 15px;overflow:auto"><table style="width:100%;border-collapse:collapse;font-size:11px;min-width:760px"><thead><tr style="text-align:center;background:#f5f8f5"><th style="padding:8px;text-align:left">Motor</th><th>DEF-L</th><th>DEF-C</th><th>DEF-R</th><th>MID</th><th>ATT-L</th><th>ATT-C</th><th>ATT-R</th><th>HatStats</th><th>Loddar</th></tr></thead><tbody id="ratingEngineRows"><tr><td colspan="10" style="padding:12px;text-align:center;color:#7a827d">Bekleniyor…</td></tr></tbody></table></div>';
      card.insertAdjacentElement('afterend', panel);
    } else {
      panel.style.display = 'block';
    }
    if (typeof window.v5RefreshRatingEngines === 'function') window.v5RefreshRatingEngines();
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { setTimeout(boot, 250); }, { once: true });
  else setTimeout(boot, 250);
})();
