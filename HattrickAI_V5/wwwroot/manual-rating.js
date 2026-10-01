
const SLOTS=[['GK','gk','GK'],['WB-L','dl','WB-L'],['DEF-CL','dcl','DEF-L'],['DEF-C','dc','DEF-C'],['DEF-CR','dcr','DEF-R'],['WB-R','dr','WB-R'],['W-L','wl','W-L'],['IM-L','ml','IM-L'],['IM-C','mc','IM-C'],['IM-R','mr','IM-R'],['W-R','wr','W-R'],['FW-L','fl','FW-L'],['FW-C','fc','FW-C'],['FW-R','fr','FW-R']];
const SKILLS=[['keeper','GK'],['defending','DEF'],['playmaking','PM'],['passing','PAS'],['winger','WI'],['scoring','SC'],['setPieces','SP'],['stamina','STA'],['form','FRM']];
const FORMATIONS={'3-5-2':['GK','DEF-CL','DEF-C','DEF-CR','W-L','IM-L','IM-C','IM-R','W-R','FW-L','FW-R'],'4-4-2':['GK','WB-L','DEF-CL','DEF-CR','WB-R','W-L','IM-L','IM-R','W-R','FW-L','FW-R'],'4-3-3':['GK','WB-L','DEF-CL','DEF-CR','WB-R','IM-L','IM-C','IM-R','FW-L','FW-C','FW-R'],'4-5-1':['GK','WB-L','DEF-CL','DEF-CR','WB-R','W-L','IM-L','IM-C','IM-R','W-R','FW-C'],'5-4-1':['GK','WB-L','DEF-CL','DEF-C','DEF-CR','WB-R','W-L','IM-L','IM-R','W-R','FW-C'],'5-3-2':['GK','WB-L','DEF-CL','DEF-C','DEF-CR','WB-R','IM-L','IM-C','IM-R','FW-L','FW-R'],'3-4-3':['GK','DEF-CL','DEF-C','DEF-CR','W-L','IM-L','IM-R','W-R','FW-L','FW-C','FW-R'],'5-5-0':['GK','WB-L','DEF-CL','DEF-C','DEF-CR','WB-R','W-L','IM-L','IM-C','IM-R','W-R'],'2-5-3':['GK','DEF-CL','DEF-CR','W-L','IM-L','IM-C','IM-R','W-R','FW-L','FW-C','FW-R']};
const POS={'GK':[50,90],'WB-L':[12,72],'DEF-CL':[32,74],'DEF-C':[50,76],'DEF-CR':[68,74],'WB-R':[88,72],'W-L':[12,48],'IM-L':[32,50],'IM-C':[50,50],'IM-R':[68,50],'W-R':[88,48],'FW-L':[28,22],'FW-C':[50,18],'FW-R':[72,22]};
let players=[],selected=new Map(),lastCalculation=null,lastOcr=null;
function fmt(v){const n=Number(v);return Number.isFinite(n)?n.toFixed(2).replace(/\\.?0+$/,''):'—'}
function currentSlots(){return FORMATIONS[document.getElementById('formation').value]||FORMATIONS['3-5-2']}
function setJsonEnabled(on){document.getElementById('downloadJson').disabled=!on}
function setOcrJsonEnabled(on){document.getElementById('downloadOcrJson').disabled=!on}
function renderPlayers(){
  const q=(document.getElementById('search').value||'').toLowerCase();
  const box=document.getElementById('players');
  const list=players.filter(p=>!q||String(p.name||'').toLowerCase().includes(q));
  if(!list.length){box.innerHTML='<div class="empty">Oyuncu bulunamadı.</div>';return}
  box.innerHTML=list.map(p=>{
    const skills=SKILLS.map(([k,l])=>`<div class="skill"><b>${p[k]??0}</b><span>${l}</span></div>`).join('');
    const moves=currentSlots().map(s=>`<button class="move" data-id="${p.id}" data-slot="${s}">${s}</button>`).join('');
    return `<div class="player"><div class="player-main"><div class="pname">${p.name||('#'+p.id)}</div><div class="pmeta">#${p.id}</div></div><div class="skills">${skills}</div><div class="movegrid">${moves}</div></div>`;
  }).join('');
  box.querySelectorAll('.move').forEach(btn=>btn.onclick=()=>assignPlayer(Number(btn.dataset.id), btn.dataset.slot));
}
function assignPlayer(id, slot){
  const p=players.find(x=>Number(x.id)===Number(id)); if(!p||!slot) return;
  for(const [s,pl] of [...selected]){ if(Number(pl.id)===Number(id)) selected.delete(s); }
  selected.set(slot,p);
  renderPitch(null); updateCount(); renderPlayers();
}
function renderPitch(rating){
  const slots=currentSlots();
  const root=document.querySelector('.slots');
  if(!root) return;
  root.innerHTML='';
  slots.forEach(code=>{
    const pos=POS[code]||[50,50];
    const pl=selected.get(code);
    const el=document.createElement('div');
    el.className='pslot';
    el.style.left=pos[0]+'%'; el.style.top=pos[1]+'%';
    el.innerHTML=pl
      ? `<div class="chip"><small>${code}</small><b>${String(pl.name||'').split(' ').slice(-1)[0]}</b><button class="x" data-slot="${code}">×</button></div>`
      : `<div class="chip empty"><small>${code}</small><b>boş</b></div>`;
    root.appendChild(el);
  });
  root.querySelectorAll('.x').forEach(b=>b.onclick=()=>{selected.delete(b.dataset.slot);renderPitch(null);updateCount();renderPlayers()});
  const board=document.getElementById('board');
  if(rating){
    board.innerHTML=[['LD',rating.leftDefence],['CD',rating.centralDefence],['RD',rating.rightDefence],['MID',rating.midfield],['LA',rating.leftAttack],['CA',rating.centralAttack],['RA',rating.rightAttack]].map(x=>`<div><span>${x[0]}</span> <b>${fmt(x[1])}</b></div>`).join('');
  } else board.innerHTML='<div style="opacity:.7">Henüz hesap yok</div>';
}
function updateCount(){
  document.getElementById('count').textContent=selected.size+' / 11 oyuncu';
  document.getElementById('calculate').disabled=!selected.size;
}
function payload(){
  const formation=document.getElementById('formation').value;
  const slots=[...selected].map(([code,p])=>({code, playerId:Number(p.id), order:'Normal'}));
  const used=new Set([...selected.values()].map(p=>Number(p.id)));
  return {
    lineup:{ teamName: document.getElementById('teamTitle').textContent||'Takım', formation, slots },
    players: players.filter(p=>used.has(Number(p.id))).map(p=>({
      id:Number(p.id), name:p.name,
      keeper:Number(p.keeper||0), defending:Number(p.defending||0),
      playmaking:Number(p.playmaking||0), passing:Number(p.passing||0), winger:Number(p.winger||0),
      scoring:Number(p.scoring||0), setPieces:Number(p.setPieces||0), stamina:Number(p.stamina||0),
      form:Number(p.form||0), experience:Number(p.experience||0), loyalty:Number(p.loyalty||0)
    })),
    context:{ matchLocation:'Home', attitude:'Normal', tactic:'Normal' },
    hoContext:{ coachModifier:0 }
  };
}
async function calc(){
  const btn=document.getElementById('calculate'), err=document.getElementById('error');
  err.style.display='none'; btn.disabled=true;
  document.getElementById('status').textContent='V5 motoru hesaplıyor…';
  try{
    const request=payload();
    const r=await fetch('/api/v5/rating-engine/manual',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(request)});
    const d=await r.json();
    if(!r.ok) throw Error(d.message||d.detail||'V5 hesaplama hatası');
    const v=d.rating||d;
    renderPitch(v);
    document.getElementById('result').innerHTML='<div class="eyebrow">V5 SONUCU</div><div class="ratings">'+[['DEF-L',v.leftDefence],['DEF-C',v.centralDefence],['DEF-R',v.rightDefence],['MID',v.midfield],['ATT-L',v.leftAttack],['ATT-C',v.centralAttack],['ATT-R',v.rightAttack]].map(x=>`<div class="rating"><b>${fmt(x[1])}</b><span>${x[0]}</span></div>`).join('')+'</div>';
    lastCalculation={schema:'hattrickai-v5-manual-rating-v1',engine:'HatFor',calculatedAt:new Date().toISOString(),endpoint:'/api/v5/rating-engine/manual',request,response:d,selectedSlots:[...selected].map(([slot,p])=>({slot,playerId:p.id,playerName:p.name}))};
    setJsonEnabled(true);
    document.getElementById('status').textContent='Hesaplandı • '+selected.size+' oyuncu';
  }catch(e){
    lastCalculation=null; setJsonEnabled(false);
    err.textContent=e.message; err.style.display='block';
    document.getElementById('status').textContent='Hesaplama başarısız';
  }finally{ btn.disabled=!selected.size; }
}
function downloadManualJson(){
  if(!lastCalculation){alert('Önce hesaplamayı çalıştır.');return}
  const blob=new Blob([JSON.stringify(lastCalculation,null,2)],{type:'application/json;charset=utf-8'});
  const url=URL.createObjectURL(blob);
  const a=document.createElement('a'); a.href=url; a.download='hattrickai-manual-rating-'+Date.now()+'.json';
  document.body.appendChild(a); a.click(); a.remove(); setTimeout(()=>URL.revokeObjectURL(url),1000);
}
function downloadOcrJson(){
  if(!lastOcr){alert('Önce OCR çalıştır.');return}
  const blob=new Blob([JSON.stringify(lastOcr,null,2)],{type:'application/json;charset=utf-8'});
  const url=URL.createObjectURL(blob);
  const a=document.createElement('a'); a.href=url; a.download='hattrickai-ocr-'+Date.now()+'.json';
  document.body.appendChild(a); a.click(); a.remove(); setTimeout(()=>URL.revokeObjectURL(url),1000);
}
async function load(){
  try{
    const r=await fetch('/api/v5/team-player-export',{cache:'no-store'});
    const d=await r.json();
    if(!r.ok) throw Error(d.message||d.detail||'Oyuncu verisi alınamadı');
    players=(d.players||[]).map(p=>({
      id:Number(p.id||p.playerId), name:p.name||('#'+(p.id||'')),
      keeper:Number(p.keeper??p.goalkeeping??0), defending:Number(p.defending??0),
      playmaking:Number(p.playmaking??0), passing:Number(p.passing??0), winger:Number(p.winger??0),
      scoring:Number(p.scoring??0), setPieces:Number(p.setPieces??0), stamina:Number(p.stamina??0),
      form:Number(p.form??0), experience:Number(p.experience??0), loyalty:Number(p.loyalty??0)
    }));
    document.getElementById('teamTitle').textContent=(d.team&&d.team.teamName)||d.teamName||'Takım kadrosu';
    document.getElementById('status').textContent=players.length+' oyuncu CHPP’den yüklendi.';
    renderPlayers(); renderPitch(null); updateCount();
  }catch(e){
    document.getElementById('error').textContent=e.message+' • Önce CHPP bağlantısını aç.';
    document.getElementById('error').style.display='block';
    document.getElementById('players').innerHTML='<div class="empty">Oyuncular yüklenemedi.</div>';
  }
}
document.getElementById('calculate').onclick=calc;
document.getElementById('clear').onclick=()=>{selected.clear();renderPitch(null);updateCount();renderPlayers();document.getElementById('result').innerHTML='';setJsonEnabled(false)};
document.getElementById('formation').onchange=()=>{const keep=new Map(selected);selected.clear();const slots=currentSlots();for(const [s,p] of keep){if(slots.includes(s))selected.set(s,p)}renderPitch(null);updateCount();renderPlayers()};
document.getElementById('search').oninput=renderPlayers;
document.getElementById('downloadJson').onclick=downloadManualJson;
document.getElementById('downloadOcrJson').onclick=downloadOcrJson;
setJsonEnabled(false);setOcrJsonEnabled(false);load();
