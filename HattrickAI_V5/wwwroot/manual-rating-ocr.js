function cropSlot(source, bounds, nx, ny, boxW=0.14, boxH=0.09){
  const cx = bounds.x + nx * bounds.width;
  const cy = bounds.y + ny * bounds.height;
  const bw = Math.max(24, Math.round(bounds.width * boxW));
  const bh = Math.max(18, Math.round(bounds.height * boxH));
  const sx = Math.max(0, Math.round(cx - bw/2));
  const sy = Math.max(0, Math.round(cy - bh/2));
  const sw = Math.min(source.width - sx, bw);
  const sh = Math.min(source.height - sy, bh);
  const c = document.createElement('canvas');
  c.width = Math.max(1, sw * 2);
  c.height = Math.max(1, sh * 2);
  const ctx = c.getContext('2d');
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(source, sx, sy, sw, sh, 0, 0, c.width, c.height);
  return {canvas:c, rect:{x:sx,y:sy,w:sw,h:sh}};
}
async function ocrCanvas(canvas, worker){
  const modes = ['contrast','threshold','color'];
  let bestText = '', bestConf = -1;
  for(const mode of modes){
    const v = mode==='color' ? canvas : imageVariant(canvas, mode, 1);
    try{
      const { data } = await worker.recognize(v);
      const text = (data?.text||'').replace(/\s+/g,' ').trim();
      const conf = Number(data?.confidence||0);
      if(text && conf >= bestConf){ bestConf = conf; bestText = text; }
      if(bestConf >= 70 && bestText.split(' ').filter(Boolean).length >= 1) break;
    }catch(_){}
  }
  return {text:bestText, confidence:bestConf};
}
function renderOcrDebug(diag){
  const body = document.getElementById('ocrDebugBody');
  if(!body || !diag) return;
  const rows = (diag.slots||[]).map(s=>{
    const status = s.matched ? `<span class="ok">OK ${(s.score*100|0)}%</span>` : `<span class="bad">YOK</span>`;
    const name = s.matched ? esc(s.matched.name) : '—';
    return `<div class="ocr-slot"><b>${esc(s.code)}</b> ${status} → ${name}<br><code>${esc(s.raw||'(boş)')}</code><br>crop: ${s.rect?`${s.rect.x},${s.rect.y} ${s.rect.w}x${s.rect.h}`:'—'} · conf ${Number(s.confidence||0).toFixed(0)}</div>`;
  }).join('');
  body.innerHTML = `<div class="ocr-meta">Formasyon tahmini: <b>${esc(diag.formation||'?')}</b> · Eşleşen: <b>${diag.matchedCount||0}</b> / ${(diag.slots||[]).length} · Süre: ${diag.ms||0} ms</div>${rows}`;
  document.getElementById('ocrDebug').open = true;
}
function drawOcrPreview(imgUrl, bounds, slotsDiag){
  const wrap = document.getElementById('ocrPreview');
  const img = document.getElementById('ocrPreviewImage');
  const canvas = document.getElementById('ocrPreviewCanvas');
  wrap.style.display = 'block';
  img.onload = ()=>{
    canvas.width = img.naturalWidth;
    canvas.height = img.naturalHeight;
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0,0,canvas.width,canvas.height);
    if(bounds){
      ctx.strokeStyle = '#ffcc00';
      ctx.lineWidth = 3;
      ctx.strokeRect(bounds.x, bounds.y, bounds.width, bounds.height);
    }
    for(const s of (slotsDiag||[])){
      if(!s.rect) continue;
      ctx.strokeStyle = s.matched ? '#22cc55' : '#ff3344';
      ctx.lineWidth = 2;
      ctx.strokeRect(s.rect.x, s.rect.y, s.rect.w, s.rect.h);
      ctx.fillStyle = s.matched ? 'rgba(34,204,85,.25)' : 'rgba(255,51,68,.15)';
      ctx.fillRect(s.rect.x, s.rect.y, s.rect.w, s.rect.h);
    }
  };
  img.src = imgUrl;
}
async function importLineupFromImage(file){
  const status = document.getElementById('imageStatus');
  const err = document.getElementById('error');
  err.style.display = 'none';
  if(!players.length){
    err.textContent = 'Önce CHPP oyuncuları yüklenmeli.';
    err.style.display = 'block';
    return;
  }
  status.textContent = 'Resim okunuyor…';
  const t0 = performance.now();
  const url = URL.createObjectURL(file);
  try{
    const img = await new Promise((res,rej)=>{
      const i = new Image();
      i.onload=()=>res(i);
      i.onerror=()=>rej(new Error('Resim yüklenemedi'));
      i.src = url;
    });
    const full = document.createElement('canvas');
    full.width = img.naturalWidth || img.width;
    full.height = img.naturalHeight || img.height;
    full.getContext('2d').drawImage(img,0,0);
    status.textContent = 'Saha sınırı tespit ediliyor…';
    const bounds = detectPitchBounds(full);
    status.textContent = 'OCR başlıyor (Tesseract)…';
    const worker = await Tesseract.createWorker('eng+tur', 1, { logger: m => {
      if(m.status==='recognizing text' && m.progress!=null){
        status.textContent = 'OCR… ' + Math.round(m.progress*100) + '%';
      }
    }});
    const diagSlots = [];
    const blocked = new Set();
    selected.clear();
    for(const [code, pt] of Object.entries(OCR_SLOT_POINTS)){
      status.textContent = 'OCR: ' + code + '…';
      const {canvas, rect} = cropSlot(full, bounds, pt[0], pt[1]);
      const {text, confidence} = await ocrCanvas(canvas, worker);
      const cleaned = (text||'').replace(/[^A-Za-zÀ-ÿğüşıöçĞÜŞİÖÇ.\-'\s]/g,' ').replace(/\s+/g,' ').trim();
      const match = cleaned ? bestOcrPlayer(cleaned, blocked) : null;
      const entry = {code, raw:cleaned, confidence, rect, matched:null, score:0};
      if(match){
        entry.matched = {id:match.player.id, name:match.player.name};
        entry.score = match.score;
        selected.set(code, match.player);
        blocked.add(match.player.id);
      }
      diagSlots.push(entry);
    }
    await worker.terminate();
    const formation = (typeof detectFormation==='function') ? detectFormation() : '4-4-2';
    lastOcrDiagnostics = {
      schema:'hattrickai-ocr-lineup-v1',
      at: new Date().toISOString(),
      fileName: file.name,
      imageSize: {w:full.width,h:full.height},
      bounds,
      formation,
      matchedCount: selected.size,
      ms: Math.round(performance.now()-t0),
      slots: diagSlots
    };
    if(typeof setOcrJsonEnabled==='function') setOcrJsonEnabled(true);
    renderOcrDebug(lastOcrDiagnostics);
    drawOcrPreview(url, bounds, diagSlots);
    updateCount();
    renderPitch(null);
    lastRating = null;
    lastCalculation = null;
    if(typeof setJsonEnabled==='function') setJsonEnabled(false);
    document.getElementById('result').innerHTML = '';
    status.textContent = `OCR tamam: ${selected.size} oyuncu yerleştirildi · formasyon ${formation}`;
    document.getElementById('status').textContent = status.textContent;
    if(selected.size >= 7 && typeof calc==='function'){
      await calc();
    }
  }catch(e){
    err.textContent = 'Resim OCR hatası: ' + (e.message||e);
    err.style.display = 'block';
    status.textContent = 'OCR başarısız';
    document.getElementById('status').textContent = 'OCR başarısız';
  }
}
document.getElementById('imageImport').onclick = ()=> document.getElementById('imageFile').click();
document.getElementById('imageFile').onchange = ev => {
  const f = ev.target.files && ev.target.files[0];
  if(f) importLineupFromImage(f);
  ev.target.value = '';
};
function downloadOcrJson(){
  if(!lastOcrDiagnostics){ alert('Önce bir resim yükleyip OCR çalıştır.'); return; }
  const blob = new Blob([JSON.stringify(lastOcrDiagnostics,null,2)],{type:'application/json;charset=utf-8'});
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'hattrickai-ocr-lineup-'+new Date().toISOString().replace(/[:.]/g,'-')+'.json';
  document.body.appendChild(a); a.click(); a.remove();
  setTimeout(()=>URL.revokeObjectURL(url),1000);
}
document.getElementById('downloadOcrJson').onclick = downloadOcrJson;
