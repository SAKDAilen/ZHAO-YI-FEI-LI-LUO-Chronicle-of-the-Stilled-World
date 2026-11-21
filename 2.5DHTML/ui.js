/* 招亦废丽罗 · 界面与叙事层 */
(function () {
  'use strict';
  const $ = s => document.querySelector(s);
  const esc = s => String(s == null ? '' : s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
  const clamp = (v, a, b) => v < a ? a : v > b ? b : v;

  const UI = window.UI = {
    G: null, acc: 0, last: 0, t: 0,
    dialogueOn: false, panelOn: false
  };

  UI.init = function (g) {
    UI.G = g;
    UI.el = {
      region: $('#region'), clock: $('#clock'), weather: $('#weather'),
      level: $('#level-badge'), xp: $('#xp-fill'), crystal: $('#hud-crystal'), iron: $('#hud-iron'),
      quest: $('#quest-tracker'), target: $('#target'), targetName: $('#target-name'),
      targetFill: $('#target-fill'), targetMeta: $('#target-meta'),
      hpFill: $('#hp-fill'), hpText: $('#hp-text'), stFill: $('#st-fill'), stText: $('#st-text'),
      lawFill: $('#law-fill'), lawText: $('#law-text'), weapon: $('#weapon-line'), charm: $('#charm-line'),
      prompt: $('#prompt'), promptText: $('#prompt-text'), combo: $('#combo'), comboN: $('#combo .n'),
      slowmo: $('#slowmo'), lowhp: $('#lowhp'), toasts: $('#toasts'), fps: $('#fps'),
      dialogue: $('#dialogue'), dlgName: $('.dlg-name'), dlgTitle: $('.dlg-title'),
      dlgText: $('.dlg-text'), dlgChoices: $('.dlg-choices'),
      panel: $('#panel'), panelTitle: $('.panel-head h2'), panelSub: $('.panel-head .sub'),
      panelBody: $('.panel-body'), panelFootL: $('.foot-l'), panelFootR: $('.foot-r'),
      title: $('#title'), death: $('#death'), ending: $('#ending'),
      endingTitle: $('#ending h2'), endingText: $('#ending p'), endingBtn: $('#ending-btn'),
      loading: $('#loading'), loadBar: $('.lg-bar i'), loadTip: $('.lg-tip'),
      fade: $('#fade'), minimap: $('#minimap')
    };
    UI.ctx = UI.el.minimap.getContext('2d');
    UI.mctx = document.createElement('canvas');
    UI.mctx.width = 160; UI.mctx.height = 160;
    UI.mctx = UI.mctx.getContext('2d');

    $('#btn-start').addEventListener('click', () => { SND.ensure(); g.startNew(); });
    $('#btn-continue').addEventListener('click', () => { SND.ensure(); g.continueGame(); });
    $('.panel-close').addEventListener('click', () => g.closePanel());
    $('#ending-btn').addEventListener('click', () => { UI.hideEnding(); });

    UI.el.dlgChoices.addEventListener('click', e => {
      const row = e.target.closest('.dlg-choice');
      if (!row) return;
      SND.sfx('ui');
      g.chooseDialogue(parseInt(row.dataset.i, 10));
    });
    UI.el.panelBody.addEventListener('click', e => {
      const row = e.target.closest('[data-action]');
      if (!row || row.classList.contains('locked')) return;
      SND.sfx('ui');
      g.action(row.dataset.action, parseInt(row.dataset.i || '0', 10));
    });
    window.addEventListener('keydown', e => {
      if (e.key === 'Escape' && UI.dialogueOn) { /* game handles */ }
    });
  };

  UI.loading = function (p, tip) {
    UI.el.loading.classList.remove('hidden');
    UI.el.loadBar.style.width = Math.round(p * 100) + '%';
    if (tip) UI.el.loadTip.textContent = tip;
  };
  UI.hideLoading = function () {
    UI.el.loading.style.transition = 'opacity .5s';
    UI.el.loading.style.opacity = '0';
    setTimeout(() => UI.el.loading.classList.add('hidden'), 520);
  };

  UI.showTitle = function (hasSave) {
    UI.el.title.classList.remove('hidden');
    $('#btn-continue').classList.toggle('hidden', !hasSave);
    document.body.classList.remove('playing');
  };
  UI.hideTitle = function () {
    UI.el.title.classList.add('hidden');
    document.body.classList.add('playing');
    // 兜底：个别环境（headless/省电模式）会冻结 opacity 过渡，确保 HUD 可见
    setTimeout(() => {
      const h = document.getElementById('hud');
      if (h && parseFloat(getComputedStyle(h).opacity) < 0.9) {
        h.style.transition = 'none';
        h.style.opacity = '1';
      }
    }, 700);
  };

  UI.toast = function (title, desc, kind) {
    const d = document.createElement('div');
    d.className = 'toast' + (kind ? ' ' + kind : '');
    d.innerHTML = '<div class="tt">' + esc(title) + '</div>' + (desc ? '<div class="td">' + esc(desc) + '</div>' : '');
    UI.el.toasts.appendChild(d);
    while (UI.el.toasts.children.length > 5) UI.el.toasts.removeChild(UI.el.toasts.firstChild);
    setTimeout(() => { d.classList.add('out'); setTimeout(() => d.remove(), 520); }, 3800);
  };

  UI.setPrompt = function (text) {
    if (text) {
      UI.el.promptText.textContent = text;
      UI.el.prompt.classList.add('show');
    } else UI.el.prompt.classList.remove('show');
  };

  UI.setCombo = function (n) {
    if (n > 1) {
      UI.el.comboN.textContent = n;
      UI.el.combo.classList.add('show');
    } else UI.el.combo.classList.remove('show');
  };

  UI.death = function (on) {
    UI.el.death.classList.toggle('hidden', !on);
    document.body.classList.toggle('dialogue-open', on);
  };

  UI.showEnding = function (title, text) {
    UI.el.endingTitle.textContent = title;
    UI.el.endingText.textContent = text;
    UI.el.ending.classList.remove('hidden');
  };
  UI.hideEnding = function () { UI.el.ending.classList.add('hidden'); };

  UI.fade = function (on, dur) {
    UI.el.fade.style.transition = 'opacity ' + (dur || 0.5) + 's';
    UI.el.fade.classList.toggle('show', on);
  };

  // ---------------- 对话 ----------------
  UI.showDialogue = function (name, title, text, choices, onPick) {
    UI.dialogueOn = true;
    UI.el.dialogue.classList.remove('hidden');
    document.body.classList.add('dialogue-open');
    UI.setPrompt(null);
    UI.el.dlgName.textContent = name || '';
    UI.el.dlgTitle.textContent = title || '';
    UI.el.dlgText.textContent = text || '';
    UI.el.dlgChoices.innerHTML = '';
    (choices || []).forEach((c, i) => {
      const d = document.createElement('div');
      d.className = 'dlg-choice' + (c.disabled ? ' locked' : '');
      d.dataset.i = i;
      d.innerHTML = '<span class="idx">' + (i + 1) + '</span><span>' + esc(c.text) + '</span><span class="arr">›</span>';
      UI.el.dlgChoices.appendChild(d);
    });
    UI.onPick = onPick;
  };
  UI.closeDialogue = function () {
    UI.dialogueOn = false;
    UI.el.dialogue.classList.add('hidden');
    document.body.classList.remove('dialogue-open');
  };

  // ---------------- 面板 ----------------
  const PANEL_TITLE = { gear: '装 备', map: '荒 原 图 志', journal: '手 记', campfire: '营 火' };
  const PANEL_SUB = {
    gear: '武器 · 护符 · 行囊', map: '五座楔坛与营火可以传送',
    journal: '成就 · 图鉴 · 战斗记录', campfire: '休息 · 强化 · 调配'
  };

  UI.openPanel = function (kind) {
    UI.panelKind = kind;
    UI.panelOn = true;
    const s = UI.G.s;
    UI.el.panel.classList.remove('hidden');
    document.body.classList.add('panel-open');
    UI.setPrompt(null);
    UI.el.panelTitle.textContent = PANEL_TITLE[kind] || '';
    UI.el.panelSub.textContent = PANEL_SUB[kind] || '';
    UI.el.panelFootL.textContent = '按 I / M / J 切换 · ESC 关闭';
    UI.el.panelFootR.textContent = '';
    UI.renderPanel();
  };

  UI.closePanel = function () {
    UI.panelOn = false;
    UI.panelKind = null;
    UI.el.panel.classList.add('hidden');
    document.body.classList.remove('panel-open');
  };

  UI.panelKey = function (n) {
    const row = UI.el.panelBody.querySelector('[data-key="' + n + '"]');
    if (row && !row.classList.contains('locked')) {
      SND.sfx('ui');
      UI.G.action(row.dataset.action, parseInt(row.dataset.i || '0', 10));
      return true;
    }
    return false;
  };

  UI.renderPanel = function () {
    const g = UI.G, s = g.s, kind = UI.panelKind;
    if (kind === 'gear') UI.renderGear(g, s);
    else if (kind === 'map') UI.renderMap(g, s);
    else if (kind === 'journal') UI.renderJournal(g, s);
    else if (kind === 'campfire') UI.renderCampfire(g, s);
  };

  UI.renderGear = function (g, s) {
    const W = GEAR.weapons, C = GEAR.charms;
    let h = '<div class="panel-cols"><div>';
    h += '<div class="sect-title">武 器</div>';
    W.forEach((w, i) => {
      const owned = s.owned[i];
      const lv = s.weaponLv[i];
      h += '<div class="row' + (s.weapon === i ? ' active' : '') + (owned ? '' : ' locked') + '" data-action="weapon" data-i="' + i + '" data-key="' + (i + 1) + '">' +
        '<span class="num">' + (i + 1) + '</span>' +
        '<span class="icon">' + (i === 0 ? '刃' : i === 1 ? '霜' : '熔') + '</span>' +
        '<span class="mid"><span class="nm">' + esc(w.name) + '<small>' + esc(w.style) + ' · ' + esc(w.elementName) + '</small></span>' +
        '<span class="ds">' + esc(w.desc) + '</span></span>' +
        '<span class="right">' + (owned ? '<b>' + (w.dmg + lv * 4) + '</b> 伤害<br>+' + lv + ' 强化' : '未获得') + '</span></div>';
    });
    h += '<div class="sect-title" style="margin-top:26px">护 符 · 只能佩带一枚</div>';
    C.forEach((c, i) => {
      const owned = s.charms[i];
      h += '<div class="row' + (s.charm === i ? ' active' : '') + (owned ? '' : ' locked') + '" data-action="charm" data-i="' + i + '" data-key="' + (i + 4) + '">' +
        '<span class="num">' + (i + 4) + '</span>' +
        '<span class="icon">' + c.name.charAt(c.name.length - 1) + '</span>' +
        '<span class="mid"><span class="nm">' + esc(c.name) + '</span><span class="ds">' + esc(c.desc) + '</span></span>' +
        '<span class="right">' + (owned ? (s.charm === i ? '已佩带' : '可佩带') : '未获得') + '</span></div>';
    });
    h += '</div><div>';

    const d = g.derived();
    h += '<div class="sect-title">状 态</div>';
    h += kv('等级', 'Lv. ' + s.level + ' <span class="small">(' + s.xp + ' / ' + GEAR.xpNeed(s.level) + ')</span>');
    h += kv('生命', Math.round(d.maxHp));
    h += kv('耐力', Math.round(d.maxStamina));
    h += kv('攻击', Math.round(d.damage) + ' <span class="small">(武器 ' + d.weaponDmg + ' + 强化 ' + (s.weaponLv[s.weapon] * 4) + ' + 律令 ' + s.damageBonus + ')</span>');
    h += kv('律令获取', '×' + d.lawGain.toFixed(2));
    h += kv('移动速度', '×' + d.move.toFixed(2));

    h += '<div class="sect-title" style="margin-top:24px">行 囊</div>';
    h += '<div class="mat-grid">' + GEAR.matKeys.map(k =>
      '<span class="mat">' + GEAR.mats[k].name + '<b>' + (s.mats[k] || 0) + '</b></span>').join('') + '</div>';
    h += kv('药剂', s.potions + ' 瓶 <span class="small">按 Q 饮用，回复 45</span>');
    h += kv('楔坛共鸣', s.shrines.filter(Boolean).length + ' / 5');
    h += kv('护符', s.charms.filter(Boolean).length + ' / 5');
    h += kv('成就', s.achievements.length + ' / ' + GEAR.achievements.length);

    h += '<div class="sect-title" style="margin-top:24px">战 绩</div>';
    h += kv('击杀 / 死亡', s.kills + ' / ' + s.deaths);
    h += kv('格挡 · 弹反', s.blocks + ' · ' + s.parries);
    h += kv('完美闪避 · 重击', s.dodges + ' · ' + s.heavies);
    h += kv('开启遗物匣', s.chests);
    h += '</div></div>';
    UI.el.panelBody.innerHTML = h;
  };

  UI.renderMap = function (g, s) {
    let h = '<div class="map-wrap"><div><canvas id="map-canvas" width="780" height="600"></canvas></div><div>';
    h += '<div class="sect-title">传 送 点</div><div class="travel-list">';
    const pts = g.travelPoints();
    if (!pts.length) h += '<div class="small">尚未点亮任何营火或楔坛。</div>';
    pts.forEach((p, i) => {
      h += '<div class="row' + (p.unlocked ? '' : ' locked') + '" data-action="travel" data-i="' + i + '" data-key="' + (i + 1) + '">' +
        '<span class="num">' + (i + 1) + '</span><span class="mid"><span class="nm">' + esc(p.name) + '</span>' +
        '<span class="ds">' + (p.unlocked ? p.kind + ' · ' + Math.round(p.dist) + ' m' : '尚未抵达') + '</span></span>' +
        '<span class="right">' + (p.unlocked ? '传送' : '—') + '</span></div>';
    });
    h += '</div><div class="sect-title" style="margin-top:26px">图 例</div>' +
      '<div class="kv"><span>金色菱形</span><b>楔之祭坛</b></div>' +
      '<div class="kv"><span>橙色圆点</span><b>营火</b></div>' +
      '<div class="kv"><span>青色圆点</span><b>可交谈者</b></div>' +
      '<div class="kv"><span>红色圆点</span><b>游荡的敌人</b></div>' +
      '<div class="small" style="margin-top:14px;line-height:1.8">传送会消耗十分钟，并清空落点附近的敌人。荒原 8192 × 8192 米，楔之峰位于世界中心。</div>';
    h += '</div></div>';
    UI.el.panelBody.innerHTML = h;
    UI.drawMap(g);
  };

  UI.drawMap = function (g) {
    const cv = document.getElementById('map-canvas');
    if (!cv) return;
    const ctx = cv.getContext('2d');
    const W = cv.width, H = cv.height;
    ctx.fillStyle = '#080b0a'; ctx.fillRect(0, 0, W, H);
    const cx = 235, cz = 235, range = 560;
    const scale = Math.min(W, H) / (range * 2);
    const px = x => W / 2 + (x - cx) * scale;
    const py = z => H / 2 + (z - cz) * scale;
    const step = 8;
    for (let z = -range; z < range; z += step) {
      for (let x = -range; x < range; x += step) {
        ctx.fillStyle = WORLD.miniColor(cx + x, cz + z);
        const a = 1 - clamp(Math.sqrt(x * x + z * z) / range, 0, 1) * 0.45;
        ctx.globalAlpha = a;
        ctx.fillRect(px(cx + x), py(cz + z), step * scale + 1, step * scale + 1);
      }
    }
    ctx.globalAlpha = 1;
    // 湖
    ctx.beginPath();
    ctx.ellipse(px(0), py(-540), 205 * scale, 276 * scale, 0, 0, 6.283);
    ctx.strokeStyle = 'rgba(120,180,180,.35)'; ctx.lineWidth = 1; ctx.stroke();
    // 峰
    ctx.beginPath();
    ctx.arc(px(0), py(0), 70 * scale, 0, 6.283);
    ctx.strokeStyle = 'rgba(200,163,92,.35)'; ctx.stroke();
    ctx.fillStyle = 'rgba(230,220,196,.55)'; ctx.font = '11px "Microsoft YaHei"'; ctx.textAlign = 'center';
    ctx.fillText('楔之峰', px(0), py(0) - 80 * scale - 6);
    ctx.fillText('泪湖', px(0), py(-540));
    ctx.fillText('青铜林', px(470), py(470) + 22);

    // POI
    g.pois.forEach(p => {
      if (p.hidden) return;
      const x = px(p.x), y = py(p.z);
      if (x < 0 || x > W || y < 0 || y > H) return;
      if (p.kind === 'shrine') {
        ctx.save(); ctx.translate(x, y); ctx.rotate(Math.PI / 4);
        ctx.fillStyle = p.used ? '#7e6a3a' : '#e2c27a';
        ctx.fillRect(-3, -3, 6, 6); ctx.restore();
      } else if (p.kind === 'campfire') {
        ctx.fillStyle = '#d07a4a'; ctx.beginPath(); ctx.arc(x, y, 3, 0, 6.283); ctx.fill();
      } else if (p.kind === 'npc') {
        ctx.fillStyle = '#7fc1b0'; ctx.beginPath(); ctx.arc(x, y, 2.5, 0, 6.283); ctx.fill();
      } else if (p.kind === 'altar') {
        ctx.strokeStyle = '#e2c27a'; ctx.lineWidth = 1; ctx.beginPath(); ctx.arc(x, y, 5, 0, 6.283); ctx.stroke();
      }
    });
    g.enemies.forEach(e => {
      if (e.dead) return;
      ctx.fillStyle = 'rgba(200,70,50,.85)';
      ctx.beginPath(); ctx.arc(px(e.x), py(e.z), 2, 0, 6.283); ctx.fill();
    });
    if (g.p) {
      ctx.save(); ctx.translate(px(g.p.x), py(g.p.z)); ctx.rotate(g.p.yaw + Math.PI / 2);
      ctx.fillStyle = '#f4e8cf';
      ctx.beginPath(); ctx.moveTo(0, -7); ctx.lineTo(5, 6); ctx.lineTo(0, 3); ctx.lineTo(-5, 6); ctx.closePath(); ctx.fill();
      ctx.restore();
    }
    ctx.strokeStyle = 'rgba(200,163,92,.25)';
    ctx.strokeRect(1, 1, W - 2, H - 2);
  };

  UI.renderJournal = function (g, s) {
    const A = GEAR.achievements;
    let h = '<div class="panel-cols"><div><div class="sect-title">成 就</div>';
    A.forEach(a => {
      const done = s.achievements.indexOf(a.key) >= 0;
      h += '<div class="ach' + (done ? ' done' : '') + '"><span class="dot"></span><div><div class="an">' + (done ? '✔ ' : '') + esc(a.name) + '</div><div class="ad">' + esc(a.desc) + '</div></div></div>';
    });
    h += '</div><div>';
    h += '<div class="sect-title">图 鉴</div>';
    h += kv('构造机偶', s.bestiary.construct || 0);
    h += kv('飞翼幽魂', s.bestiary.winged || 0);
    h += kv('熔岩兽', s.bestiary.lava || 0);
    h += '<div class="sect-title" style="margin-top:24px">战 斗 记 录</div>';
    h += kv('击杀', s.kills);
    h += kv('死亡', s.deaths);
    h += kv('弹反', s.parries);
    h += kv('完美闪避', s.dodges);
    h += kv('重击命中', s.heavies);
    h += kv('击碎晶簇', s.verses);
    h += kv('夜间行走', Math.round(s.nightMeters) + ' m');
    h += kv('游玩时间', Math.floor(s.playtime / 60) + ' 分');
    h += '<div class="sect-title" style="margin-top:24px">碑 文</div>';
    h += kv('已读', (s.flags.steles_read || 0) + ' / 8');
    h += '<div class="small" style="margin-top:10px;line-height:1.8">「法则高于一切，包括写下法则的人。」</div>';
    h += '</div></div>';
    UI.el.panelBody.innerHTML = h;
  };

  UI.renderCampfire = function (g, s) {
    const W = GEAR.weapons[s.weapon], lv = s.weaponLv[s.weapon];
    const costC = GEAR.upgradeCrystal(lv), costI = GEAR.upgradeIron(lv);
    const canUp = lv < 5 && s.mats.crystal >= costC && s.mats.iron >= costI;
    let h = '<div class="panel-cols"><div>';
    h += '<div class="sect-title">营 火</div>';
    h += rowAction('camp', 0, 1, '休 息', '回满生命与耐力，把这里设为重生点。世界时间推进一小时。', '按 1', true);
    h += '<div class="row' + (canUp ? '' : ' locked') + '" data-action="camp" data-i="1" data-key="2">' +
      '<span class="num">2</span><span class="icon">锻</span><span class="mid"><span class="nm">强 化 ' + esc(W.name) + '<small>+' + lv + ' → +' + Math.min(5, lv + 1) + '</small></span>' +
      '<span class="ds">' + (lv >= 5 ? '已达强化上限。' : '需要 晶尘 ' + costC + ' · 楔铁 ' + costI) + '</span></span>' +
      '<span class="right">' + (lv >= 5 ? '上限' : '<b>' + s.mats.crystal + '</b> / ' + costC + '<br><b>' + s.mats.iron + '</b> / ' + costI) + '</span></div>';
    h += '<div class="sect-title" style="margin-top:26px">调 配</div>';
    GEAR.recipes.forEach((r, i) => {
      const ok = g.canCraft(r);
      h += '<div class="row' + (ok ? '' : ' locked') + '" data-action="craft" data-i="' + i + '" data-key="' + (i + 3) + '">' +
        '<span class="num">' + (i + 3) + '</span><span class="icon">' + r.icon + '</span>' +
        '<span class="mid"><span class="nm">' + esc(r.name) + '</span><span class="ds">' + esc(r.desc) + '</span></span>' +
        '<span class="right">' + GEAR.recipeCostText(r) + '</span></div>';
    });
    h += '</div><div>';
    h += '<div class="sect-title">行 囊</div>';
    h += '<div class="mat-grid">' + GEAR.matKeys.map(k =>
      '<span class="mat">' + GEAR.mats[k].name + '<b>' + (s.mats[k] || 0) + '</b></span>').join('') + '</div>';
    h += kv('药剂', s.potions + ' 瓶');
    h += kv('当前武器', W.name + ' +' + lv);
    h += kv('攻击力', Math.round(g.derived().damage));
    h += '<div class="sect-title" style="margin-top:24px">篝 火 旁</div>';
    h += '<div class="small" style="line-height:1.9">火光把影子拉得很长。远处有东西在移动，但暂时不敢靠近。<br><br>「休息不是停下，是为了让世界重新开始。」</div>';
    h += '</div></div>';
    UI.el.panelBody.innerHTML = h;
  };

  function rowAction(action, i, key, name, desc, right, enabled) {
    return '<div class="row' + (enabled ? '' : ' locked') + '" data-action="' + action + '" data-i="' + i + '" data-key="' + key + '">' +
      '<span class="num">' + key + '</span><span class="icon">火</span><span class="mid"><span class="nm">' + name + '</span>' +
      '<span class="ds">' + desc + '</span></span><span class="right">' + right + '</span></div>';
  }
  function kv(k, v) {
    return '<div class="kv"><span>' + k + '</span><b>' + v + '</b></div>';
  }

  // ---------------- 每帧 HUD ----------------
  UI.frame = function (g) {
    UI.t += 1;
    UI.acc++;
    const el = UI.el;
    const p = g.p, s = g.s;
    if (!p) return;

    if (UI.acc % 6 === 0) {
      const d = g.derived();
      el.hpFill.style.width = clamp(p.hp / d.maxHp * 100, 0, 100) + '%';
      el.hpText.textContent = Math.max(0, Math.ceil(p.hp)) + ' / ' + Math.round(d.maxHp);
      el.stFill.style.width = clamp(p.stamina / d.maxStamina * 100, 0, 100) + '%';
      el.stText.textContent = Math.max(0, Math.ceil(p.stamina)) + ' / ' + Math.round(d.maxStamina);
      el.lawFill.style.width = clamp(p.law / 100 * 100, 0, 100) + '%';
      el.lawText.textContent = p.law >= 100 ? '可发动 · R' : Math.round(p.law) + '%';
      el.lawFill.classList.toggle('ready', p.law >= 100);
      el.level.textContent = 'Lv. ' + s.level;
      el.xp.style.width = clamp(s.xp / GEAR.xpNeed(s.level) * 100, 0, 100) + '%';
      el.crystal.textContent = s.mats.crystal | 0;
      el.iron.textContent = s.mats.iron | 0;
      el.region.textContent = g.regionName();
      el.clock.innerHTML = '第 <b>' + g.s.day + '</b> 日 · <b>' + g.clockText() + '</b><span id="weather"> · ' + g.weatherName() + '</span>';
      const w = GEAR.weapons[s.weapon];
      el.weapon.innerHTML = w.name + '<em>+' + s.weaponLv[s.weapon] + ' · ' + w.style + '</em>';
      el.charm.textContent = s.charm >= 0 ? '护符 · ' + GEAR.charms[s.charm].name : '护符 · 无';
      el.fps.textContent = g.fps + ' FPS · ' + g.entCount() + ' 实体';

      const q = g.questInfo();
      if (q) {
        el.quest.innerHTML = '<div class="q-title">' + esc(q.title) + '</div>' +
          '<div class="q-stage">' + esc(q.stage) + '</div>' +
          (q.progress ? '<div class="q-prog">' + esc(q.progress) + '</div>' : '') +
          (q.hint ? '<div class="q-hint">' + esc(q.hint) + '</div>' : '');
      } else el.quest.innerHTML = '';

      el.lowhp.classList.toggle('show', p.hp / d.maxHp < 0.3 && !g.dead);
      el.slowmo.classList.toggle('show', g.slowT > 0);
      el.combo.classList.toggle('show', g.comboCount > 1 && g.comboT > 0);
      if (g.comboCount > 1) el.comboN.textContent = g.comboCount;
    }

    // 目标
    const t = g.target;
    if (t && !t.dead) {
      el.target.classList.add('show');
      el.targetName.innerHTML = (t.affixTitle ? '<span class="affix">' + esc(t.affixTitle) + ' · </span>' : '') +
        esc(t.name) + ' <span style="color:#8d846f;font-size:12px">Lv.' + t.level + '</span>';
      el.targetFill.style.width = clamp(t.hp / t.maxHp * 100, 0, 100) + '%';
      const st = [];
      if (t.chillT > 0) st.push('<span style="color:#9fd8e8">霜蚀</span>');
      if (t.burnT > 0) st.push('<span style="color:#e6a06a">灼烧</span>');
      if (t.stunT > 0) st.push('<span style="color:#e2c27a">眩晕</span>');
      el.targetMeta.innerHTML = st.join(' · ') || '锁 定';
    } else el.target.classList.remove('show');

    UI.drawMinimap(g);
  };

  UI.drawMinimap = function (g) {
    const ctx = UI.ctx, cv = UI.el.minimap;
    const W = cv.width, H = cv.height;
    if (!UI.mini || performance.now() - UI.mini > 380) {
      UI.mini = performance.now();
      const c = UI.mctx, N = 160, range = 320;
      const step = range * 2 / N;
      for (let y = 0; y < N; y++) {
        for (let x = 0; x < N; x++) {
          c.fillStyle = WORLD.miniColor(g.p.x + (x - N / 2) * step, g.p.z + (y - N / 2) * step);
          c.fillRect(x, y, 1, 1);
        }
      }
    }
    ctx.clearRect(0, 0, W, H);
    ctx.save();
    ctx.beginPath(); ctx.arc(W / 2, H / 2, W / 2 - 1, 0, 6.283); ctx.clip();
    ctx.drawImage(UI.mctx.canvas, 0, 0, W, H);
    const range = 320, sc = W / (range * 2);
    const px = x => W / 2 + (x - g.p.x) * sc;
    const py = z => H / 2 + (z - g.p.z) * sc;
    g.pois.forEach(p => {
      if (p.hidden) return;
      const x = px(p.x), y = py(p.z);
      if (x < -10 || x > W + 10 || y < -10 || y > H + 10) return;
      if (p.kind === 'shrine') {
        ctx.save(); ctx.translate(x, y); ctx.rotate(Math.PI / 4);
        ctx.fillStyle = p.used ? 'rgba(200,163,92,.35)' : '#e2c27a';
        ctx.fillRect(-3.5, -3.5, 7, 7); ctx.restore();
      } else if (p.kind === 'campfire') {
        ctx.fillStyle = p.used ? '#7e6a3a' : '#d07a4a';
        ctx.beginPath(); ctx.arc(x, y, 3.5, 0, 6.283); ctx.fill();
      } else if (p.kind === 'npc') {
        ctx.fillStyle = '#7fc1b0'; ctx.beginPath(); ctx.arc(x, y, 3, 0, 6.283); ctx.fill();
      } else if (p.kind === 'altar') {
        ctx.strokeStyle = '#e2c27a'; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.arc(x, y, 6, 0, 6.283); ctx.stroke();
      }
    });
    g.enemies.forEach(e => {
      if (e.dead) return;
      const x = px(e.x), y = py(e.z);
      if (x < 0 || x > W || y < 0 || y > H) return;
      ctx.fillStyle = e.elite ? '#f08a5a' : 'rgba(200,70,50,.9)';
      ctx.beginPath(); ctx.arc(x, y, e.elite ? 3.5 : 2.4, 0, 6.283); ctx.fill();
    });
    // 玩家箭头
    ctx.save(); ctx.translate(W / 2, H / 2); ctx.rotate(g.p.yaw + Math.PI / 2);
    ctx.fillStyle = '#f8eed6';
    ctx.beginPath(); ctx.moveTo(0, -9); ctx.lineTo(6, 7); ctx.lineTo(0, 4); ctx.lineTo(-6, 7); ctx.closePath(); ctx.fill();
    ctx.restore();
    ctx.fillStyle = 'rgba(230,220,196,.55)';
    ctx.font = '11px "Microsoft YaHei"'; ctx.textAlign = 'center';
    ctx.fillText('北', W / 2, 15);
    ctx.restore();
    ctx.strokeStyle = 'rgba(200,163,92,.3)'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.arc(W / 2, H / 2, W / 2 - 1, 0, 6.283); ctx.stroke();
    // 中央准星
    ctx.strokeStyle = 'rgba(230,220,196,.25)'; ctx.lineWidth = 1;
    ctx.beginPath(); ctx.moveTo(W / 2 - 4, H / 2); ctx.lineTo(W / 2 + 4, H / 2);
    ctx.moveTo(W / 2, H / 2 - 4); ctx.lineTo(W / 2, H / 2 + 4); ctx.stroke();
  };

  UI.showEnding = function (title, text) {
    UI.el.endingTitle.textContent = title;
    UI.el.endingText.textContent = text;
    UI.el.ending.classList.remove('hidden');
  };
})();
