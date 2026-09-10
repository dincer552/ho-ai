(function(){
  'use strict';
  const benchKey='hattrickai.v5.benchSelections';
  const slots=['Kaleci','Göbek defans','Bek','İç orta saha','Forvet','Kanat','Ekstra'];
  const esc=s=>String(s??'').replace(/[&<>\"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;',"'":'&#039;'}[m]));
  const loadBench=()=>{try{return JSON.parse(localStorage.getItem(benchKey)||'{}');}catch(_){return {};}};
  const tacticNames={Normal:'Normal',CounterAttack:'Kontra atak',LongShots:'Uzun şutlar',AttackMiddle:'Ortadan hücum',AttackWings:'Kanatlardan hücum',Creative:'Yaratıcı',Pressing:'Pres'};
  function host(){const p=document.getElementById('ownPitch');return p&&p.closest('.lineup-card');}
  function ensure(){
    const h=host();if(!h)return null;
    let box=document.getElementById('chppExportBox');if(box)return box;
    box=document.createElement('section');box.id='chppExportBox';box.style.cssText='margin-top:12px;background:#fff;border:1px solid #dfe5e1;border-radius:15px;box-shadow:0 2px 9px #0002;overflow:hidden';
    box.innerHTML='<div style="padding:14px 16px"><div style="font:900 10px Arial;letter-spacing:.07em;color:#7c837f">CHPP AKTARIM</div><div id="chppTarget" style="font:800 15px Arial;color:#27322d;margin-top:4px">Hedef maç kontrol ediliyor…</div><div id="chppPermission" style="font:11px Arial;color:#747c76;margin-top:4px"></div><button id="chppPreview" type="button" style="width:100%;margin-top:10px;height:46px;border:0;border-radius:10px;background:#21804a;color:#fff;font:900 13px Arial;cursor:pointer">HATTRICK’E AKTAR</button></div><div id="chppPreviewBody" style="display:none;border-top:1px solid #e5ebe7;padding:12px 14px"></div>';
    h.parentNode.insertBefore(box,h.nextSibling);
    document.getElementById('chppPreview').onclick=preview;
    return box;
  }
  function current(){return window.__v5LastAnalysis||null;}
  function request(){
    const a=current();if(!a)throw Error('Önce maç analizini tamamla.');
    const lineup=a.ownLineup||a.own||{};const source=Array.isArray(lineup.slots)?lineup.slots:[];
    const starter=source.filter(x=>Number(x.playerId)>0).slice(0,11);
    if(starter.length!==11)throw Error('İlk 11 hazır değil.');
    const bench=loadBench();const missing=slots.filter(s=>!(Number(bench[s])>0));
    if(missing.length)throw Error('7 yedek tamamlanmalı: '+missing.join(', '));
    let tactic=a.selectedTactic||'Normal';
    if(typeof tactic==='object')tactic=tactic.name||tactic.value||'Normal';
    let attitude=(a.appliedQuestionnaire&&a.appliedQuestionnaire.matchImportance)||'Normal';
    if(typeof attitude==='number')attitude=['Normal','MatchOfTheSeason','PlayItCool','Auto'][attitude]||'Normal';
    if(attitude==='Auto')attitude='Normal';
    return {Formation:String(lineup.formation||a.ownFormation||''),Tactic:String(tactic),Attitude:String(attitude),Slots:starter.map(x=>({Code:String(x.code),PlayerId:Number(x.playerId),Order:Number(x.order||0)})),Bench:bench};
  }
  async function preview(){
    const body=document.getElementById('chppPreviewBody'),button=document.getElementById('chppPreview');
    try{
      const req=request();button.disabled=true;button.textContent='KONTROL EDİLİYOR…';
      const r=await fetch('/api/v5/chpp-export/target?ts='+Date.now(),{cache:'no-store'});const d=await r.json();if(!r.ok)throw Error(d.message||'Hedef maç alınamadı.');
      body.style.display='block';body.innerHTML='<div style="font:900 12px Arial;color:#27322d">'+esc(d.homeTeamName)+' – '+esc(d.awayTeamName)+'</div><div style="font:11px Arial;color:#747c76;margin-top:4px">Başlangıç: '+esc(new Date(d.matchDate).toLocaleString('tr-TR'))+'</div><div style="font:11px Arial;color:#747c76;margin-top:4px">Diziliş: <b>'+esc(req.Formation)+'</b> • Taktik: <b>'+esc(tacticNames[req.Tactic]||req.Tactic)+'</b></div><div style="font:11px Arial;color:#747c76;margin-top:4px">11 ilk oyuncu + 7 yedek hazırlanacak.</div><div id="chppExportState" style="margin-top:10px;font:800 11px Arial;color:#267448"></div><button id="chppConfirm" type="button" style="width:100%;margin-top:10px;height:46px;border:0;border-radius:10px;background:#1f6f43;color:#fff;font:900 13px Arial;cursor:pointer">ONAYLA VE HATTRICK’E AKTAR</button>';
      document.getElementById('chppConfirm').onclick=()=>send(req);
      document.getElementById('chppPermission').textContent=d.canWrite?'set_matchorder yetkisi hazır.':'set_matchorder yetkisi yok — aktarım engellenecek.';
      document.getElementById('chppPermission').style.color=d.canWrite?'#267448':'#b33b32';
    }catch(e){body.style.display='block';body.innerHTML='<div style="font:800 12px Arial;color:#b33b32">'+esc(e.message)+'</div>';}
    finally{button.disabled=false;button.textContent='HATTRICK’E AKTAR';}
  }
  async function send(req){
    const state=document.getElementById('chppExportState'),button=document.getElementById('chppConfirm');if(!button)return;
    button.disabled=true;state.textContent='CHPP aktarımı yapılıyor…';
    try{
      const r=await fetch('/api/v5/chpp-export',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(req)});const d=await r.json().catch(()=>({}));if(!r.ok)throw Error(d.message||'CHPP aktarımı başarısız.');
      state.textContent=d.verified?'✓ Aktarıldı ve Hattrick’ten doğrulandı.':'⚠ Aktarım yapıldı fakat read-back doğrulaması başarısız.';state.style.color=d.verified?'#267448':'#b33b32';button.textContent=d.verified?'AKTARIM DOĞRULANDI':'DOĞRULAMA BAŞARISIZ';
    }catch(e){state.textContent='✕ '+e.message;state.style.color='#b33b32';button.disabled=false;}
  }
  function watch(){
    const runtime=document.getElementById('runtime');ensure();
    if(runtime){let last='';const check=()=>{const t=runtime.textContent||'';if(t!==last){last=t;if(/analiz tamamlandı/i.test(t)){ensure();}}};new MutationObserver(check).observe(runtime,{subtree:true,childList:true,characterData:true});}
  }
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',watch);else watch();
})();
