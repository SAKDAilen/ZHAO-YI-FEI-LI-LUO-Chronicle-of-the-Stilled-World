/* ============================================================
   招亦废丽罗 · 律令之楔  —  WebEdition 主程序
   零依赖 Canvas 2D 俯视 2.5D 动作 RPG
   ============================================================ */
(function () {
  'use strict';

  const $ = s => document.querySelector(s);
  const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
  const lerp = (a, b, t) => a + (b - a) * t;
  const len = (x, z) => Math.sqrt(x * x + z * z);
  const TAU = Math.PI * 2;
  const SAVE_KEY = 'zhaoyi_web_save_v1';
  const FONT = '"Microsoft YaHei","PingFang SC",sans-serif';
  const SERIF = '"STSong","SimSun","Songti SC",serif';

  // ============================================================
  //  装备 / 材料 / 成就定义
  // ============================================================
  const GEAR = window.GEAR = {
    weapons: [
      { key: 'bronze', name: '青铜律刃', style: '直剑', elementName: '无', element: 0, dmg: 26, swing: 0.33, range: 3.6, arc: 0.78, knock: 2.2, icon: '刃',
        desc: '守楔人锻打的第一柄剑。刃口有细密的旧伤，每一道都记着一次重启。', glow: '#e8c98a' },
      { key: 'frost', name: '霜晶双刃', style: '双刃', elementName: '霜蚀', element: 1, dmg: 20, swing: 0.24, range: 3.2, arc: 0.88, knock: 1.3, icon: '霜',
        desc: '泪湖底长出的晶刃。挥动时空气里会留下白色的裂痕，被击中的东西会慢下来。', glow: '#a9dfe8' },
      { key: 'ember', name: '熔痕巨剑', style: '巨剑', elementName: '灼烧', element: 2, dmg: 44, swing: 0.52, range: 4.1, arc: 0.64, knock: 5.2, icon: '熔',
        desc: '从熔炉峡谷的余烬里拔出来的剑。很重，热得像还活着。', glow: '#f0a06a' }
    ],
    charms: [
      { key: 'nighteye', name: '守夜人之眼', desc: '最大耐力 +15%。夜里也能看清荒原的轮廓。', bonus: { stam: 0.15 } },
      { key: 'hourglass', name: '钟漏残片', desc: '律令获取 +35%。时间在你身上漏得慢一些。', bonus: { law: 0.35 } },
      { key: 'emberheart', name: '熔火之心', desc: '伤害 +18%，最大生命 -10%。', bonus: { dmg: 0.18, hp: -0.10 } },
      { key: 'echobell', name: '回声铃', desc: '移动速度 +8%。铃声总比脚步晚半拍。', bonus: { move: 0.08 } },
      { key: 'scavenger', name: '拾荒者徽', desc: '晶尘掉落 +40%，经验 +25%。', bonus: { loot: 0.40, xp: 0.25 } }
    ],
    recipes: [
      { key: 'potion', name: '调和药剂', icon: '药', result: 'potion', amount: 1, desc: '将苔草与晶尘压进瓶中，恢复 45 生命。', crystal: 1, herb: 2 },
      { key: 'vigor', name: '强健汤', icon: '体', result: 'max_hp', amount: 10, desc: '永久增加 10 点最大生命。', crystal: 2, herb: 3, ember: 1 },
      { key: 'whet', name: '磨刃膏', icon: '砺', result: 'damage', amount: 1, desc: '永久增加 1 点攻击。', ember: 2, bone: 1 }
    ],
    mats: {
      crystal: { name: '晶尘', color: '#a9dfe8' },
      iron: { name: '楔铁', color: '#b8b2a4' },
      bone: { name: '兽骨', color: '#d9ceb6' },
      ember: { name: '火髓', color: '#f0a06a' },
      herb: { name: '苔草', color: '#8fbf88' }
    },
    matKeys: ['crystal', 'iron', 'bone', 'ember', 'herb'],
    achievements: [
      { key: 'first_blood', name: '第一次呼吸', desc: '击败第一个游荡的构装体。' },
      { key: 'ten_kills', name: '十次告别', desc: '累计击败 10 名敌人。' },
      { key: 'fifty_kills', name: '荒原清道夫', desc: '累计击败 50 名敌人。' },
      { key: 'parry', name: '以刃承刃', desc: '成功弹反一次攻击。' },
      { key: 'dodge', name: '差之毫厘', desc: '在翻滚末段完美闪避一次攻击。' },
      { key: 'shrine5', name: '五楔共鸣', desc: '点亮全部五座楔之祭坛。' },
      { key: 'verse8', name: '读碑人', desc: '读完八块碑文。' },
      { key: 'charms4', name: '拾荒者的收藏', desc: '获得四枚护符。' },
      { key: 'peak', name: '登上楔之峰', desc: '抵达海拔 120 米以上。' },
      { key: 'level10', name: '律令的学徒', desc: '升到 10 级。' },
      { key: 'weapons', name: '三刃在手', desc: '集齐三柄武器。' },
      { key: 'night', name: '夜行者', desc: '在夜间行走 500 米。' }
    ],
    xpNeed(lv) { return 55 + (lv - 1) * 50; },
    upgradeCrystal(lv) { return 3 + lv * 3; },
    upgradeIron(lv) { return 1 + lv * 2; },
    recipeCostText(r) {
      const out = [];
      if (r.crystal) out.push('晶尘 ' + r.crystal);
      if (r.iron) out.push('楔铁 ' + r.iron);
      if (r.bone) out.push('兽骨 ' + r.bone);
      if (r.ember) out.push('火髓 ' + r.ember);
      if (r.herb) out.push('苔草 ' + r.herb);
      return out.join(' · ');
    }
  };

  // ============================================================
  //  状态
  // ============================================================
  function defaultState() {
    return {
      v: 5, started: false,
      time: 8.5 * 60, day: 1,
      hp: 100, stamina: 100, law: 0,
      hpBase: 100, staminaBase: 100,
      level: 1, xp: 0, weapon: 0, weaponLv: [0, 0, 0], owned: [true, false, false],
      charm: -1, charms: [false, false, false, false, false], potions: 2,
      mats: { crystal: 0, iron: 0, bone: 0, ember: 0, herb: 0 },
      items: { fragment: 0, memory: 0, relic: 0, part: 0 },
      damageBonus: 0, regen: 0, lawBonus: 0,
      kills: 0, deaths: 0, blocks: 0, parries: 0, dodges: 0, heavies: 0,
      chests: 0, ores: 0, verses: 0, nightMeters: 0, playtime: 0,
      shrines: [false, false, false, false, false],
      achievements: [], flags: {}, quests: {}, used: {},
      bestiary: { construct: 0, winged: 0, lava: 0 },
      respawn: { x: DATA.spawn[0], z: DATA.spawn[1] },
      weather: 'clear', weatherTimer: 150,
      lastEvent: '你在一片青铜林里醒来。风里有铁锈和潮气。',
      endingSeen: false, watchStartKills: 0,
      intro: false
    };
  }

  const g = window.GAME = {
    s: defaultState(),
    p: null, enemies: [], pois: [], npcs: [],
    cam: { x: 470, z: 470 + 30, zoom: 27, shakeX: 0, shakeY: 0 },
    keys: {}, mouse: { x: 0, y: 0, down: false },
    started: false, dead: false, deadT: 0,
    slowT: 0, comboT: 0, comboCount: 0,
    target: null, prompt: null, promptPoi: null,
    dlg: null, panelKind: null, fps: 60,
    particles: [], dmgNums: [], floaters: [],
    eventT: 95, spawnT: 2, weatherBlend: 1, fpsAcc: 0, fpsN: 0,
    shake: 0, timeScale: 1, W: innerWidth, H: innerHeight,
    atlas: { light: null }, campGlow: 0,
    hasSave: false, lastWeather: '', lastCamp: false,
    introT: 0, stepPhase: 0, hitStop: 0
  };
  g.s = defaultState();

  function hasSave() {
    try { return !!localStorage.getItem(SAVE_KEY); } catch (e) { return false; }
  }

  // ============================================================
  //  世界与 POI
  // ============================================================
  function landNear(x, z) {
    if (!WORLD.isWater(x, z)) return [x, z];
    for (let r = 8; r <= 80; r += 8) {
      for (let a = 0; a < 12; a++) {
        const ang = a / 12 * TAU;
        const nx = x + Math.cos(ang) * r, nz = z + Math.sin(ang) * r;
        if (!WORLD.isWater(nx, nz)) return [nx, nz];
      }
    }
    return [x, z];
  }

  function addPoi(kind, id, x, z, extra) {
    const p = landNear(x, z);
    const o = Object.assign({
      kind, id, x: p[0], z: p[1], used: false, hidden: false, respawnT: 0, r: 2.4,
      name: '', prompt: '', data: {}
    }, extra || {});
    g.pois.push(o);
    if (kind !== 'npc') WORLD.exclusions.push({ x: o.x, z: o.z, r: kind === 'shrine' ? 11 : kind === 'campfire' ? 9 : 6.5 });
    return o;
  }

  function buildPOIs() {
    g.pois.length = 0;
    const S = DATA.shrines;
    // 营地：出生点营地 + 三处野营
    const camps = [
      ['camp_0', 470, 478, '青铜林营地'],
      ['camp_1', 392, 352, '回声山营地'],
      ['camp_2', 612, 708, '熔炉峡谷营地'],
      ['camp_3', 268, 596, '泪湖东岸营地']
    ];
    camps.forEach((c, i) => addPoi('campfire', c[0], c[1], c[2], {
      r: 3.1, name: c[3], prompt: '休息 · ' + c[3], data: { idx: i }
    }));
    // 五坛
    const shr = [[520, 396], [342, 300], [706, 604], [348, 742], [62, 62]];
    S.forEach((s, i) => addPoi('shrine', s.id, shr[i][0], shr[i][1], {
      r: 3.6, name: s.name, prompt: '触碰 · ' + s.name, data: { idx: i, desc: s.desc, bonus: s.bonus }
    }));
    // 八碑
    const steles = [[474, 452], [448, 432], [500, 440], [430, 486], [512, 500], [420, 520], [548, 520], [492, 552]];
    steles.forEach((q, i) => addPoi('stele', 'stele_' + i, q[0], q[1], {
      r: 2.7, prompt: '阅读碑文', name: '碑文之' + (i + 1), data: { idx: i }
    }));
    // 晶簇
    const crystals = [
      [486, 458], [512, 466], [456, 512], [530, 492], [444, 468], [498, 528],
      [410, 452], [556, 468], [470, 560], [586, 540], [382, 500], [612, 452],
      [530, 420], [430, 560], [350, 480], [660, 500]
    ];
    crystals.forEach((q, i) => addPoi('crystal', 'crystal_' + i, q[0], q[1], {
      r: 2.0, prompt: '击碎晶簇', name: '野生晶簇', respawnT: 80, data: { size: 0.8 + (i % 4) * 0.15 }
    }));
    // 遗物匣
    const chests = [
      ['chest_0', 440, 512, 0], ['chest_1', 546, 470, 0], ['chest_2', 406, 408, 0],
      ['chest_3', 566, 556, 0], ['chest_4', 360, 460, 0],
      ['chest_rare_0', 470, 300, 1], ['chest_rare_1', 690, 700, 1], ['chest_rare_2', 90, 110, 1]
    ];
    chests.forEach(c => addPoi('chest', c[0], c[1], c[2], {
      r: 2.3, prompt: c[2] ? '开启 · 稀有遗物匣' : '开启 · 遗物匣', name: c[2] ? '稀有遗物匣' : '遗物匣', data: { tier: c[2] }
    }));
    // 灵泉
    [[452, 540], [548, 420], [600, 600], [300, 380]].forEach((q, i) => addPoi('vent', 'vent_' + i, q[0], q[1], {
      r: 2.9, prompt: '汲取 · 灵泉', name: '灵泉', respawnT: 45
    }));
    // 矿脉
    const ores = [[600, 640], [650, 660], [700, 560], [560, 760], [720, 700], [640, 720],
      [380, 800], [300, 760], [250, 700], [180, 620], [760, 640], [520, 820]];
    ores.forEach((q, i) => addPoi('ore', 'ore_' + i, q[0], q[1], {
      r: 2.2, prompt: '开采 · 楔铁矿脉', name: '楔铁矿脉', respawnT: 95
    }));
    // 苔草
    const herbs = [[460, 462], [486, 486], [432, 478], [512, 458], [536, 512], [420, 540],
      [500, 410], [382, 420], [560, 430], [470, 600], [340, 520], [600, 490]];
    herbs.forEach((q, i) => addPoi('herb', 'herb_' + i, q[0], q[1], {
      r: 1.9, prompt: '采集 · 苔草', name: '苔草', respawnT: 75
    }));
    // 峰顶祭坛
    addPoi('altar', 'altar_peak', 14, 14, { r: 4.5, prompt: '触碰 · 律令之楔', name: '律令之楔' });
    // NPC
    const npcs = [
      { key: 'zhonglou', x: 478, z: 460, yaw: 2.4 },
      { key: 'shouye', x: 452, z: 494, yaw: 0.7 },
      { key: 'tao', x: 452, z: 456, yaw: 1.6 },
      { key: 'huisheng', x: 540, z: 470, yaw: 3.2 }
    ];
    g.npcs = npcs;
    npcs.forEach(n => {
      const def = DATA.npcs[n.key];
      addPoi('npc', 'npc_' + n.key, n.x, n.z, {
        r: 3.4, prompt: '交谈 · ' + def.name, name: def.name, npcKey: n.key
      });
    });
  }

  function resetPois() {
    g.pois.forEach(o => { o.used = false; o.hidden = false; o.respawnT = 0; });
  }

  // ============================================================
  //  玩家
  // ============================================================
  function makePlayer() {
    const s = g.s;
    g.p = {
      x: DATA.spawn[0], z: DATA.spawn[1], y: 0, yaw: Math.PI,
      vx: 0, vz: 0, hp: s.hp, stamina: s.stamina, law: s.law,
      walk: 0, bob: 0, swingT: -1, swingIdx: 0, swingDur: 0.33,
      swingYaw: 0, swingHits: new Set(), attackCd: 0, combo: 0,
      heavyT: -1, heavyDone: false, rollT: 0, rollElapsed: 0, rollX: 0, rollZ: 0,
      blocking: false, blockT: 0, parryT: 0, blockBreak: 0,
      invuln: 0, flinch: 0, hurtT: 0, lawT: 0, regenT: 0,
      dead: false, dust: 0, trail: []
    };
    g.p.y = WORLD.heightAt(g.p.x, g.p.z);
    g.cam.x = g.p.x; g.cam.z = g.p.z + 16;
  }

  function derived() {
    const s = g.s;
    const c = s.charm >= 0 ? GEAR.charms[s.charm] : null;
    const b = c ? c.bonus : {};
    const maxHp = Math.round(s.hpBase * (1 + (b.hp || 0)));
    const maxStamina = Math.round(s.staminaBase * (1 + (b.stam || 0)));
    const weaponDmg = GEAR.weapons[s.weapon].dmg + s.weaponLv[s.weapon] * 4;
    const damage = Math.round((weaponDmg + s.damageBonus) * (1 + (b.dmg || 0)));
    return {
      maxHp, maxStamina, weaponDmg, damage,
      lawGain: 1 + (b.law || 0) + s.shrines.filter(Boolean).length * 0.04,
      move: 1 + (b.move || 0),
      loot: 1 + (b.loot || 0),
      xpGain: 1 + (b.xp || 0),
      regen: s.regen * 0.35,
      swing: GEAR.weapons[s.weapon].swing * (1 - s.weaponLv[s.weapon] * 0.03)
    };
  }

  function applyGear() {
    const d = derived();
    if (g.p) {
      g.p.hp = clamp(g.p.hp, 0, d.maxHp);
      g.p.stamina = clamp(g.p.stamina, 0, d.maxStamina);
    }
  }

  function addMaxHp(n) {
    g.s.hpBase += n;
    applyGear();
    if (g.p) g.p.hp = Math.min(derived().maxHp, g.p.hp + n);
  }
  function addMaxStamina(n) { g.s.staminaBase += n; applyGear(); }

  function addXp(n) {
    const s = g.s;
    const gain = Math.max(1, Math.round(n * derived().xpGain));
    s.xp += gain;
    while (s.level < 15 && s.xp >= GEAR.xpNeed(s.level)) {
      s.xp -= GEAR.xpNeed(s.level);
      s.level++;
      s.hpBase += 6; s.staminaBase += 4;
      if (g.p) { g.p.hp = derived().maxHp; g.p.stamina = derived().maxStamina; }
      SND.sfx('level');
      toast('升 级 · Lv.' + s.level, '生命 +6 · 耐力 +4', 'ach');
      burst(g.p.x, g.p.z, '#e8c98a', 26, 'level');
      if (s.level === 2) unlockWeapon(1);
      if (s.level === 4) unlockWeapon(2);
      if (s.level >= 10) unlockAch('level10');
    }
    applyGear();
    return gain;
  }

  // ============================================================
  //  UI / 提示
  // ============================================================
  function toast(t, d, kind) { UI.toast(t, d, kind); }

  function unlockAch(key) {
    if (g.s.achievements.indexOf(key) >= 0) return;
    const a = GEAR.achievements.find(x => x.key === key);
    if (!a) return;
    g.s.achievements.push(key);
    toast('成 就 · ' + a.name, a.desc, 'ach');
    SND.sfx('quest');
  }

  function unlockWeapon(i) {
    if (g.s.owned[i]) return;
    g.s.owned[i] = true;
    toast('获得武器 · ' + GEAR.weapons[i].name, GEAR.weapons[i].desc, 'item');
    SND.sfx('level');
    if (g.s.owned.every(Boolean)) unlockAch('weapons');
  }

  function unlockCharm(i) {
    if (g.s.charms[i]) return;
    g.s.charms[i] = true;
    toast('获得护符 · ' + GEAR.charms[i].name, GEAR.charms[i].desc, 'item');
    SND.sfx('crystal');
    if (g.s.charms.filter(Boolean).length >= 4) unlockAch('charms4');
  }

  function floating(x, z, text, color, size) {
    g.dmgNums.push({ x, z, y: 0, vy: 26, life: 1.2, max: 1.2, text, color: color || '#f3e6c8', size: size || 15 });
  }

  // ============================================================
  //  粒子
  // ============================================================
  function burst(x, z, color, n, type) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * TAU, sp = 1 + Math.random() * 4;
      g.particles.push({
        x, z, y: 10 + Math.random() * 30, vx: Math.cos(a) * sp, vz: Math.sin(a) * sp,
        vy: 20 + Math.random() * 55, life: 0.5 + Math.random() * 0.6, max: 1,
        color, size: 1.5 + Math.random() * 2.6, type: type || 'spark', grav: 60
      });
    }
  }

  function dust(x, z, n, color) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * TAU, sp = 0.5 + Math.random() * 1.4;
      g.particles.push({
        x, z, y: 2 + Math.random() * 5, vx: Math.cos(a) * sp, vz: Math.sin(a) * sp,
        vy: 8 + Math.random() * 12, life: 0.35 + Math.random() * 0.35, max: 0.8,
        color: color || 'rgba(190,175,140,0.5)', size: 1 + Math.random() * 2, type: 'dust', grav: 18
      });
    }
  }

  // ============================================================
  //  敌 人
  // ============================================================
  const ENEMY_DEF = {
    construct: { name: '构造机偶', hp: 82, spd: 2.5, dmg: 12, range: 2.3, r: 0.75, wind: 0.55, color: '#9a8a6a' },
    winged: { name: '飞翼幽魂', hp: 52, spd: 4.2, dmg: 9, range: 2.0, r: 0.6, wind: 0.42, color: '#7fc1b0' },
    lava: { name: '熔岩兽', hp: 118, spd: 2.1, dmg: 17, range: 2.8, r: 0.95, wind: 0.72, color: '#c06a45' }
  };

  function spawnEnemy(kind, x, z, level, affix) {
    const d = ENEMY_DEF[kind] || ENEMY_DEF.construct;
    const L = level || 1;
    const a = affix || 0;
    const e = {
      kind, x, z, y: WORLD.heightAt(x, z), yaw: 0,
      hp: Math.round(d.hp * (0.7 + L * 0.32)), maxHp: 0, speed: d.spd, dmg: d.dmg + L * 1.6,
      range: d.range, r: d.r, level: L, affix: a, name: d.name,
      windup: 0, windMax: d.wind, attackCd: 1 + Math.random(), stunT: 0, chillT: 0, burnT: 0, burnTick: 0,
      hitFlash: 0, dead: false, deathT: 0, rewarded: false, anim: Math.random() * 10,
      kx: 0, kz: 0, wanderA: Math.random() * TAU, wanderT: 0, bob: Math.random() * 6,
      size: 1, elite: a > 0
    };
    if (a === 1) { e.speed *= 1.3; e.title = '迅捷'; }
    else if (a === 2) { e.hp = Math.round(e.hp * 1.6); e.speed *= 0.85; e.size = 1.12; e.title = '重甲'; }
    else if (a === 3) { e.dmg *= 1.3; e.title = '嗜血'; }
    e.maxHp = e.hp;
    e.bob = Math.random() * 6;
    e.windMax = d.wind * (a === 1 ? 0.75 : 1);
    g.enemies.push(e);
    return e;
  }

  function pickAffix(L) {
    if (L < 2) return 0;
    if (Math.random() > 0.10 + L * 0.045) return 0;
    return 1 + Math.floor(Math.random() * 3);
  }

  function spawnPointNear(x, z, minD, maxD) {
    for (let i = 0; i < 26; i++) {
      const a = Math.random() * TAU, rr = minD + Math.random() * (maxD - minD);
      const nx = x + Math.cos(a) * rr, nz = z + Math.sin(a) * rr;
      if (WORLD.isWater(nx, nz)) continue;
      if (Math.abs(WORLD.heightAt(nx, nz) - WORLD.heightAt(x, z)) > 26) continue;
      let bad = false;
      for (const p of g.pois) {
        if (p.kind === 'npc') continue;
        if (len(p.x - nx, p.z - nz) < (p.kind === 'shrine' ? 14 : 7)) { bad = true; break; }
      }
      if (bad) continue;
      return [nx, nz];
    }
    return [x + minD, z];
  }

  function spawnInitialEnemies() {
    g.enemies.length = 0;
    const types = ['construct', 'winged', 'construct', 'lava'];
    for (let i = 0; i < 4; i++) {
      const [x, z] = spawnPointNear(g.p.x, g.p.z, 19, 32);
      spawnEnemy(types[i], x, z, 1 + Math.floor(g.s.kills / 8), 0);
    }
  }

  function enemyAlive() { return g.enemies.filter(e => !e.dead).length; }

  function damageEnemy(e, amount, opts) {
    if (!e || e.dead) return;
    opts = opts || {};
    let dmg = amount;
    if (opts.backstab) dmg = Math.round(dmg * 1.55);
    if (opts.crit) dmg = Math.round(dmg * 1.5);
    e.hp -= dmg;
    e.hitFlash = 0.16;
    if (e.affix === 2) e.hitFlash = 0.22;
    const col = opts.color || (opts.crit ? '#ffd47a' : opts.backstab ? '#ffb070' : '#f3e6c8');
    floating(e.x, e.z, (opts.crit ? '暴击 ' : opts.backstab ? '背刺 ' : '') + dmg, col, opts.crit || opts.backstab ? 19 : 15);
    e.kx += (opts.kx || 0); e.kz += (opts.kz || 0);
    burst(e.x, e.z, opts.color || '#e8c98a', opts.crit ? 14 : 7, 'spark');
    SND.sfx(opts.heavy ? 'heavy' : 'hit', 0.8);
    if (opts.stun) { e.stunT = Math.max(e.stunT, opts.stun); }
    if (opts.status === 1) e.chillT = 2.6;
    if (opts.status === 2) e.burnT = 3.2;
    if (e.hp <= 0) killEnemy(e);
    else if (opts.heavy) { g.shake = Math.max(g.shake, 5); }
  }

  function killEnemy(e) {
    if (e.dead) return;
    e.dead = true; e.deathT = 1.15; e.hp = 0;
    g.s.kills++;
    g.s.bestiary[e.kind] = (g.s.bestiary[e.kind] || 0) + 1;
    const L = e.level;
    let xp = 12 + L * 6 + (e.affix ? 10 : 0);
    addXp(xp);
    const loot = derived().loot;
    const crystals = Math.max(1, Math.round((1 + Math.random() * 2) * loot));
    g.s.mats.crystal += crystals;
    if (Math.random() < 0.45) g.s.mats.iron += 1;
    if (Math.random() < 0.30) g.s.mats.bone += 1;
    if (Math.random() < 0.16) g.s.mats.ember += 1;
    floating(e.x, e.z - 1, '+' + crystals + ' 晶尘', '#a9dfe8', 13);
    burst(e.x, e.z, ENEMY_DEF[e.kind].color, 18, 'shatter');
    SND.sfx('crystal', 0.6);
    if (g.s.kills === 1) unlockAch('first_blood');
    if (g.s.kills >= 10) unlockAch('ten_kills');
    if (g.s.kills >= 50) unlockAch('fifty_kills');
    if (e.affix && Math.random() < 0.12) {
      const idx = Math.floor(Math.random() * 5);
      if (!g.s.charms[idx]) unlockCharm(idx);
    }
    if (g.s.quests.q_watch === 0 || g.s.quests.q_watch === 1) {
      if (g.s.kills - g.s.watchStartKills >= 5) toast('委托完成', '回到守夜人砌那里复命。', 'quest');
    }
  }

  // ============================================================
  //  玩 家 更 新
  // ============================================================
  function aimYaw() {
    const p = g.p;
    const [wx, wz] = WORLD.unproject(g.mouse.x, g.mouse.y, g.cam, g.W, g.H);
    return Math.atan2(wx - p.x, wz - p.z);
  }

  function movePlayer(dt) {
    const p = g.p, s = g.s;
    if (p.dead) return;
    let ix = 0, iz = 0;
    if (g.keys['KeyW'] || g.keys['ArrowUp']) iz -= 1;
    if (g.keys['KeyS'] || g.keys['ArrowDown']) iz += 1;
    if (g.keys['KeyA'] || g.keys['ArrowLeft']) ix -= 1;
    if (g.keys['KeyD'] || g.keys['ArrowRight']) ix += 1;
    const mag = len(ix, iz);
    if (mag > 0) { ix /= mag; iz /= mag; }

    const sprint = (g.keys['ShiftLeft'] || g.keys['ShiftRight']) && p.stamina > 4 && !p.blocking && p.rollT <= 0;
    let speed = 6.4 * derived().move;
    if (sprint) speed = 9.8 * derived().move;
    if (p.blocking) speed *= 0.42;
    if (p.swingT >= 0) speed *= 0.68;
    if (p.heavyT >= 0) speed *= 0.28;
    if (WORLD.isWater(p.x, p.z)) speed *= 0.55;
    const sl = WORLD.slopeAt(p.x, p.z);
    if (sl.s > 0.55) speed *= clamp(1.05 - sl.s * 0.5, 0.5, 1);
    if (p.rollT > 0) { ix = p.rollX; iz = p.rollZ; speed = 12.5; }

    p.vx = lerp(p.vx, ix * speed, clamp(dt * 12, 0, 1));
    p.vz = lerp(p.vz, iz * speed, clamp(dt * 12, 0, 1));
    p.x += p.vx * dt;
    p.z += p.vz * dt;
    p.x = clamp(p.x, -3980, 3980);
    p.z = clamp(p.z, -3980, 3980);

    // 朝向：移动时朝移动方向；攻击时朝鼠标
    const sp = len(p.vx, p.vz);
    if (p.swingT >= 0 || p.heavyT >= 0) {
      p.yaw = lerpAng(p.yaw, p.swingYaw, 0.35);
    } else if (sp > 0.4) {
      p.yaw = lerpAng(p.yaw, Math.atan2(p.vx, p.vz), clamp(dt * 13, 0, 1));
    }

    const speedNow = sp;
    p.walk += dt * (2.2 + speedNow * 1.25);
    p.bob = Math.sin(p.walk * 2) * Math.min(2.2, speedNow * 0.22);
    p.dust -= dt;
    if (speedNow > 3 && p.dust <= 0) {
      p.dust = 0.14;
      dust(p.x, p.z, 1);
    }
    if (sprint && speedNow > 6) {
      p.stamina = Math.max(0, p.stamina - 19 * dt);
      if (p.stamina <= 0) p.stamina = 0;
    }
    // 夜晚行走
    const hour = (s.time / 60) % 24;
    if (hour >= 20 || hour < 5) s.nightMeters += speedNow * dt;

    // 地形高度
    p.y = lerp(p.y, WORLD.heightAt(p.x, p.z), clamp(dt * 10, 0, 1));

    // 体力恢复
    if (p.regenT > 0) p.regenT -= dt;
    else if (!sprint && p.rollT <= 0) p.stamina = Math.min(derived().maxStamina, p.stamina + 17 * dt);
    else if (!sprint) p.stamina = Math.min(derived().maxStamina, p.stamina + 10 * dt);

    // 生命恢复
    if (p.hurtT > 0) p.hurtT -= dt;
    else if (p.hp > 0) p.hp = Math.min(derived().maxHp, p.hp + (0.45 + derived().regen) * dt);

    // 高海拔成就
    if (p.y > 120) unlockAch('peak');

    p.invuln = Math.max(0, p.invuln - dt);
    p.attackCd = Math.max(0, p.attackCd - dt);
    p.flinch = Math.max(0, p.flinch - dt);
    p.parryT = Math.max(0, p.parryT - dt);
    p.blockBreak = Math.max(0, p.blockBreak - dt);
    if (p.hurtT <= 0 && p.flinch > 0) p.flinch -= dt;

    p.blocking = g.blockKey && p.stamina > 1 && p.rollT <= 0 && p.heavyT < 0;
  }

  g.blockKey = false;

  function lerpAng(a, b, t) {
    let d = ((b - a + Math.PI * 3) % TAU) - Math.PI;
    return a + d * t;
  }

  function startLightAttack() {
    const p = g.p, s = g.s;
    if (p.dead || p.rollT > 0 || p.heavyT >= 0 || p.attackCd > 0 || p.blocking) return;
    if (p.stamina < 7) { SND.sfx('error'); return; }
    p.stamina -= 7; p.regenT = 0.55;
    p.attackCd = 0.16;
    p.swingT = 0;
    p.swingIdx = p.combo % 3;
    p.combo++;
    p.swingDur = derived().swing * (p.swingIdx === 2 ? 1.14 : 1);
    p.swingYaw = aimYaw();
    p.swingHits.clear();
    SND.sfx('swing');
    g.comboT = 3.4;
    // 第三段额外突进
    if (p.swingIdx === 2) {
      p.vx += Math.sin(p.swingYaw) * 4.5;
      p.vz += Math.cos(p.swingYaw) * 4.5;
    }
  }

  function startHeavy() {
    const p = g.p;
    if (p.dead || p.rollT > 0 || p.swingT >= 0 || p.heavyT >= 0 || p.stamina < 18) { SND.sfx('error'); return; }
    p.heavyT = 0; p.heavyDone = false; p.stamina -= 18; p.regenT = 0.8;
    p.swingYaw = aimYaw();
    SND.sfx('heavy');
  }

  function startRoll() {
    const p = g.p, s = g.s;
    if (p.dead || p.rollT > 0 || p.stamina < 15) { SND.sfx('error'); return; }
    let ix = 0, iz = 0;
    if (g.keys['KeyW']) iz -= 1; if (g.keys['KeyS']) iz += 1;
    if (g.keys['KeyA']) ix -= 1; if (g.keys['KeyD']) ix += 1;
    if (ix === 0 && iz === 0) { ix = Math.sin(p.yaw); iz = Math.cos(p.yaw); }
    const m = len(ix, iz); ix /= m; iz /= m;
    p.rollX = ix; p.rollZ = iz;
    p.rollT = 0.45; p.rollElapsed = 0;
    p.stamina -= 15; p.regenT = 0.45;
    p.invuln = Math.max(p.invuln, 0.42);
    SND.sfx('dodge');
    dust(p.x, p.z, 6);
  }

  function updatePlayerCombat(dt) {
    const p = g.p, s = g.s;
    if (p.dead) return;
    if (p.rollT > 0) {
      p.rollElapsed += dt;
      p.rollT -= dt;
      if (p.rollT <= 0) { p.rollT = 0; p.rollElapsed = 0; }
    }
    if (p.swingT >= 0) {
      p.swingT += dt / p.swingDur;
      if (p.swingT >= 0.18 && p.swingT <= 0.78) swingHits();
      if (p.swingT >= 1) { p.swingT = -1; p.combo = Math.max(0, p.combo); }
    }
    if (p.heavyT >= 0) {
      p.heavyT += dt / 0.62;
      if (p.heavyT >= 0.34 && !p.heavyDone) { p.heavyDone = true; heavyHit(); }
      if (p.heavyT >= 1) p.heavyT = -1;
    }
    if (p.lawT > 0) {
      p.lawT -= dt;
      if (p.lawT <= 0) { p.lawT = 0; toast('律令退去', '世界重新压回你的肩上。'); }
    }
    if (p.combo > 0 && p.swingT < 0) {
      p.comboT = (p.comboT || 0) - dt;
      if (p.comboT <= 0) p.combo = 0;
    }
    // 律令获取
    if (p.lawT > 0) p.law = 0;
    else p.law = clamp(p.law, 0, 100);
  }

  function swingHits() {
    const p = g.p, s = g.s;
    const w = GEAR.weapons[s.weapon];
    const dmg = playerDamage(p.swingIdx === 2 ? 1.22 : 1);
    for (const e of g.enemies) {
      if (e.dead || p.swingHits.has(e)) continue;
      const dx = e.x - p.x, dz = e.z - p.z;
      const dist = len(dx, dz);
      if (dist > w.range + e.r) continue;
      const ang = Math.atan2(dx, dz);
      let diff = Math.abs(((ang - p.swingYaw + Math.PI * 3) % TAU) - Math.PI);
      if (diff > w.arc) continue;
      p.swingHits.add(e);
      const back = Math.cos(ang - e.yaw) < -0.35;
      const crit = Math.random() < 0.08;
      damageEnemy(e, dmg, {
        crit, backstab: back, status: w.element, color: w.glow,
        kx: Math.sin(p.swingYaw) * w.knock, kz: Math.cos(p.swingYaw) * w.knock
      });
      registerHit();
      g.shake = Math.max(g.shake, crit ? 5 : 2.6);
      if (w.element === 1) burst(e.x, e.z, '#a9dfe8', 8, 'frost');
      if (w.element === 2) burst(e.x, e.z, '#f0a06a', 10, 'ember');
    }
  }

  function heavyHit() {
    const p = g.p;
    const dmg = playerDamage(1.9);
    for (const e of g.enemies) {
      if (e.dead) continue;
      const dx = e.x - p.x, dz = e.z - p.z;
      if (len(dx, dz) > 4.4 + e.r) continue;
      damageEnemy(e, dmg, { heavy: true, stun: 1.15, status: GEAR.weapons[g.s.weapon].element,
        kx: dx * 1.6, kz: dz * 1.6, color: '#ffd08a' });
      p.swingHits.add(e);
    }
    g.s.heavies++;
    g.shake = Math.max(g.shake, 8);
    SND.sfx('heavy');
    burst(p.x, p.z, '#e8c98a', 24, 'shatter');
    checkComboAchievements();
  }

  function playerDamage(mult) {
    const p = g.p, s = g.s;
    const d = derived();
    let dmg = d.damage * (mult || 1);
    const comboMul = 1 + Math.min(0.30, g.comboCount * 0.04);
    dmg *= comboMul;
    if (p.lawT > 0) dmg *= 1.5;
    return Math.round(dmg);
  }

  function registerHit() {
    g.comboCount++;
    g.comboT = 3.2;
    g.s.law = Math.min(100, g.s.law + 6 * derived().lawGain);
    checkComboAchievements();
  }

  function checkComboAchievements() {
    if (g.comboCount === 10) {
      g.s.law = Math.min(100, g.s.law + 10);
      UI.toast('连击 ×10', '律令能量 +10', 'good');
    }
  }

  // ============================================================
  //  受伤 / 死亡
  // ============================================================
  function hurtPlayer(amount, sx, sz) {
    const p = g.p, s = g.s;
    if (p.dead || p.invuln > 0) return;
    if (p.rollT > 0) {
      // 翻滚末段 0.22 秒 = 完美闪避
      if (p.rollElapsed > 0.23) {
        g.slowT = 0.45;
        p.stamina = Math.min(derived().maxStamina, p.stamina + 14);
        s.dodges++;
        unlockAch('dodge');
        floating(p.x, p.z, '完美闪避', '#a9dfe8', 18);
        g.shake = 3; SND.sfx('dodge');
        return;
      }
      return;
    }
    if (p.blocking && p.blockBreak <= 0) {
      if (p.parryT > 0) {
        p.parryT = 0;
        s.parries++;
        unlockAch('parry');
        g.slowT = 0.34;
        s.law = Math.min(100, s.law + 20 * derived().lawGain);
        let best = null, bd = 1e9;
        for (const e of g.enemies) {
          if (e.dead) continue;
          const d = len(e.x - p.x, e.z - p.z);
          if (d < bd) { bd = d; best = e; }
        }
        if (best) {
          best.stunT = Math.max(best.stunT, 1.5);
          damageEnemy(best, Math.round(12 + s.level * 2), { color: '#ffe2a8', stun: 1.5 });
        }
        floating(p.x, p.z - 2, '弹 反', '#ffe2a8', 20);
        g.shake = 6; SND.sfx('parry');
        return;
      }
      const absorbed = Math.round(amount * 0.22);
      p.stamina -= amount * 0.95;
      s.blocks++;
      if (p.stamina <= 0) {
        p.stamina = 0; p.blockBreak = 1.1; p.blocking = false;
        p.hp -= Math.round(amount * 0.65);
        floating(p.x, p.z, '破 防', '#e08a6a', 18);
        SND.sfx('blockBreak'); g.shake = 5;
      } else {
        p.hp -= absorbed;
        floating(p.x, p.z, '-' + absorbed, '#d9ceb6', 13);
        SND.sfx('block'); g.shake = 2;
      }
      p.invuln = 0.18; p.hurtT = 3;
      if (p.hp <= 0) die();
      return;
    }
    p.hp -= amount;
    p.invuln = 0.55; p.flinch = 0.3; p.hurtT = 4;
    floating(p.x, p.z, '-' + amount, '#e08a6a', 18);
    burst(p.x, p.z, '#b45a35', 8, 'blood');
    g.shake = 6; SND.sfx('hit');
    if (p.hp <= 0) die();
  }

  function die() {
    const p = g.p;
    if (p.dead) return;
    p.dead = true; p.hp = 0; p.swingT = -1; p.heavyT = -1;
    g.dead = true; g.deadT = 3.2;
    g.s.deaths++;
    UI.death(true);
    SND.sfx('death');
    toast('你 死 了', '世界会记得你，但不会等你。', 'bad');
  }

  function respawn() {
    const p = g.p, s = g.s;
    p.x = s.respawn.x; p.z = s.respawn.z;
    p.hp = derived().maxHp; p.stamina = derived().maxStamina;
    p.law = 0; p.lawT = 0; p.dead = false; p.invuln = 2.2;
    p.swingT = -1; p.heavyT = -1; p.rollT = 0; p.combo = 0;
    g.dead = false; g.deadT = 0;
    g.cam.x = p.x; g.cam.z = p.z;
    UI.death(false);
    UI.fade(true, 0.2);
    setTimeout(() => UI.fade(false, 0.9), 260);
    g.enemies.forEach(e => { if (!e.dead && len(e.x - p.x, e.z - p.z) < 10) { e.dead = true; e.deathT = 0.5; } });
    toast('在营火旁醒来', '生命与耐力已恢复。已死 ' + s.deaths + ' 次。');
  }

  // ============================================================
  //  敌人 AI
  // ============================================================
  function updateEnemies(dt) {
    const p = g.p;
    for (let i = g.enemies.length - 1; i >= 0; i--) {
      const e = g.enemies[i];
      e.anim += dt;
      if (e.hitFlash > 0) e.hitFlash -= dt;
      if (e.dead) {
        e.deathT -= dt;
        if (e.deathT <= 0) g.enemies.splice(i, 1);
        continue;
      }
      if (e.chillT > 0) e.chillT -= dt;
      if (e.stunT > 0) { e.stunT -= dt; continue; }
      if (e.burnT > 0) {
        e.burnT -= dt;
        e.burnTick -= dt;
        if (e.burnTick <= 0) {
          e.burnTick = 0.85;
          damageEnemy(e, 4 + e.level * 2, { color: '#f0a06a' });
          if (e.dead) continue;
        }
      }
      const dx = p.x - e.x, dz = p.z - e.z;
      const dist = len(dx, dz);
      const slow = e.chillT > 0 ? 0.55 : 1;
      const spd = e.speed * slow;
      e.attackCd = Math.max(0, e.attackCd - dt);
      // 击退
      if (Math.abs(e.kx) + Math.abs(e.kz) > 0.1) {
        e.x += e.kx * dt; e.z += e.kz * dt;
        e.kx *= 0.86; e.kz *= 0.86;
      }
      if (dist > 34) {
        e.wanderT -= dt;
        if (e.wanderT <= 0) { e.wanderA = Math.random() * TAU; e.wanderT = 2 + Math.random() * 3; }
        e.x += Math.sin(e.wanderA) * spd * 0.25 * dt;
        e.z += Math.cos(e.wanderA) * spd * 0.25 * dt;
        continue;
      }
      // 预警蓄力
      if (e.windup > 0) {
        e.windup -= dt;
        e.yaw = lerpAng(e.yaw, Math.atan2(dx, dz), clamp(dt * 8, 0, 1));
        if (e.windup <= 0) {
          e.windup = 0;
          e.attackCd = 1.15;
          const d2 = len(p.x - e.x, p.z - e.z);
          if (d2 < e.range + 1.5 && !p.dead) {
            hurtPlayer(Math.round(e.dmg), e.x, e.z);
            if (e.affix === 3) e.hp = Math.min(e.maxHp, e.hp + Math.round(e.dmg * 0.35));
            e.kx -= Math.sin(e.yaw) * 1.2; e.kz -= Math.cos(e.yaw) * 1.2;
          }
          burst(e.x + Math.sin(e.yaw) * 1.2, e.z + Math.cos(e.yaw) * 1.2, '#e08a6a', 6, 'spark');
        }
        continue;
      }
      // 追击 / 攻击
      if (dist > e.range * 0.92) {
        const ang = Math.atan2(dx, dz);
        e.yaw = lerpAng(e.yaw, ang, clamp(dt * 7, 0, 1));
        e.x += Math.sin(ang) * spd * dt;
        e.z += Math.cos(ang) * spd * dt;
      } else if (e.attackCd <= 0 && dist < e.range + 0.6) {
        e.windup = e.windMax;
        e.yaw = Math.atan2(dx, dz);
        SND.sfx('swing', 0.35);
      }
      // 分离
      for (const o of g.enemies) {
        if (o === e || o.dead) continue;
        const ox = e.x - o.x, oz = e.z - o.z;
        const d2 = len(ox, oz);
        if (d2 > 0.01 && d2 < e.r + o.r + 0.5) {
          const push = (e.r + o.r + 0.5 - d2) * 0.6;
          e.x += ox / d2 * push; e.z += oz / d2 * push;
        }
      }
      e.x = clamp(e.x, -3980, 3980); e.z = clamp(e.z, -3980, 3980);
      e.y = WORLD.heightAt(e.x, e.z);
    }
    // 补充
    g.spawnT -= dt;
    if (g.spawnT <= 0 && enemyAlive() < 5 && g.started) {
      g.spawnT = 5.5;
      const [sx, sz] = spawnPointNear(p.x, p.z, 24, 42);
      const kinds = ['construct', 'winged', 'lava', 'construct'];
      const kind = kinds[Math.floor(Math.random() * kinds.length)];
      const L = Math.min(8, 1 + Math.floor(g.s.kills / 8));
      spawnEnemy(kind, sx, sz, L, pickAffix(L));
      burst(sx, sz, '#8fbf88', 10, 'dust');
    }
  }

  // ============================================================
  //  交互
  // ============================================================
  function nearestPoi() {
    const p = g.p;
    let best = null, bd = 1e9;
    for (const o of g.pois) {
      if (o.hidden) continue;
      if (o.used && o.kind !== 'campfire' && o.kind !== 'stele' && o.kind !== 'ore') continue;
      if (o.used && o.kind === 'ore') continue;
      const d = len(o.x - p.x, o.z - p.z);
      if (d < o.r && d < bd) { bd = d; best = o; }
    }
    return best;
  }

  function interact() {
    const o = g.promptPoi;
    if (!o) return;
    SND.sfx('ui');
    if (o.kind === 'npc') { startDialogue(o.npcKey); return; }
    if (o.kind === 'campfire') {
      g.s.respawn = { x: o.x, z: o.z };
      g.s.flags['camp_' + o.id] = true;
      openPanel('campfire');
      return;
    }
    if (o.kind === 'shrine') { activateShrine(o); return; }
    if (o.kind === 'stele') { readStele(o); return; }
    if (o.kind === 'crystal') {
      o.used = true; o.respawnT = 80;
      const n = 2 + (Math.random() < 0.25 ? 1 : 0);
      g.s.mats.crystal += n;
      g.s.verses = g.s.verses;
      addXp(5);
      floating(o.x, o.z, '+' + n + ' 晶尘', '#a9dfe8', 15);
      burst(o.x, o.z, '#a9dfe8', 18, 'shatter');
      SND.sfx('crystal');
      return;
    }
    if (o.kind === 'chest') { openChest(o); return; }
    if (o.kind === 'vent') {
      o.used = true; o.respawnT = 45;
      g.p.stamina = derived().maxStamina;
      g.p.hp = Math.min(derived().maxHp, g.p.hp + 10);
      g.s.law = Math.min(100, g.s.law + 8);
      floating(o.x, o.z, '灵泉', '#8fd8c8', 16);
      burst(o.x, o.z, '#8fd8c8', 18, 'heal');
      SND.sfx('heal');
      return;
    }
    if (o.kind === 'ore') {
      o.used = true; o.respawnT = 95;
      const n = 1 + (Math.random() < 0.5 ? 1 : 0);
      g.s.mats.iron += n;
      g.s.ores++;
      if (Math.random() < 0.4) g.s.mats.crystal += 1;
      addXp(4);
      floating(o.x, o.z, '+' + n + ' 楔铁', '#cfc6b2', 15);
      burst(o.x, o.z, '#cfc6b2', 12, 'shatter');
      SND.sfx('pickup');
      return;
    }
    if (o.kind === 'herb') {
      o.used = true; o.respawnT = 75;
      g.s.mats.herb += 1;
      g.p.hp = Math.min(derived().maxHp, g.p.hp + 5);
      floating(o.x, o.z, '+1 苔草', '#8fbf88', 15);
      burst(o.x, o.z, '#8fbf88', 10, 'heal');
      SND.sfx('pickup');
      return;
    }
    if (o.kind === 'altar') { touchAltar(); return; }
  }

  function openChest(o) {
    o.used = true; o.hidden = true;
    const tier = o.data.tier || 0;
    g.s.chests++;
    addXp(10 + tier * 14);
    SND.sfx('crystal');
    burst(o.x, o.z, tier ? '#ffd47a' : '#e8c98a', 26, 'level');
    let msg;
    if (tier) {
      const lockedCharms = g.s.charms.map((v, i) => i).filter(i => !g.s.charms[i]);
      if (lockedCharms.length && Math.random() < 0.55) {
        const idx = lockedCharms[Math.floor(Math.random() * lockedCharms.length)];
        unlockCharm(idx);
        msg = '护符 · ' + GEAR.charms[idx].name;
      } else if (!g.s.owned[1]) {
        unlockWeapon(1);
        msg = '武器 · 霜晶双刃';
      } else {
        g.s.mats.ember += 2; g.s.mats.crystal += 6;
        msg = '火髓 ×2 · 晶尘 ×6';
      }
      toast('稀有遗物匣', msg, 'item');
    } else {
      const c = 4 + Math.floor(Math.random() * 4);
      const iron = 1 + Math.floor(Math.random() * 2);
      g.s.mats.crystal += c; g.s.mats.iron += iron;
      if (Math.random() < 0.35) { g.s.potions++; msg = '晶尘 ×' + c + ' · 楔铁 ×' + iron + ' · 药剂 ×1'; }
      else msg = '晶尘 ×' + c + ' · 楔铁 ×' + iron;
      toast('遗物匣', msg, 'item');
    }
  }

  function activateShrine(o) {
    const idx = o.data.idx;
    if (g.s.shrines[idx]) { toast(o.name, '祭坛已经与你共鸣。'); return; }
    o.used = true;
    g.s.shrines[idx] = true;
    const b = o.data.bonus || {};
    if (b.max_hp) addMaxHp(b.max_hp);
    if (b.max_stamina) addMaxStamina(b.max_stamina);
    if (b.damage_bonus) g.s.damageBonus += b.damage_bonus;
    if (b.regen) g.s.regen += b.regen;
    if (b.law) g.s.lawBonus += b.law;
    g.p.hp = derived().maxHp; g.p.stamina = derived().maxStamina;
    g.s.law = 100;
    addXp(25);
    SND.sfx('shrine');
    g.shake = 7;
    burst(o.x, o.z, '#e2c27a', 40, 'level');
    const n = g.s.shrines.filter(Boolean).length;
    toast('楔坛共鸣 · ' + o.name, o.data.desc + '（' + n + ' / 5）', 'ach');
    if (n >= 5) unlockAch('shrine5');
  }

  function readStele(o) {
    const text = DATA.steles[o.data.idx] || '碑面已经被风磨平了。';
    if (!g.s.flags['stele_' + o.data.idx]) {
      g.s.flags['stele_' + o.data.idx] = true;
      g.s.verses++;
      addXp(8);
      if (g.s.verses >= 8) unlockAch('verse8');
      SND.sfx('quest');
    }
    g.dlg = { mode: 'lore' };
    g.talking = true;
    UI.showDialogue('碑 文', '第 ' + (o.data.idx + 1) + ' 碑', text,
      [{ text: '（合上碑文）', next: null }], null);
  }

  function touchAltar() {
    const n = g.s.shrines.filter(Boolean).length;
    if (n < 5) {
      toast('律令之楔', '五座楔坛尚未全部共鸣。已点亮 ' + n + ' / 5。');
      return;
    }
    if (!g.s.endingSeen) {
      g.s.endingSeen = true;
      SND.sfx('shrine');
      UI.showEnding('楔 落', '你把手按在律令之楔上。五座祭坛的光顺着你的手臂爬上来，像五条终于想起自己名字的河。\n\n' +
        '楔子没有断。它只是松了。\n风第一次改变方向，湖面第一次起浪。远处，钟漏的沙漏里落下一粒沙。\n\n' +
        '世界没有结束。它只是重新开始了——而这一次，你还在里面。');
    } else {
      toast('律令之楔', '风从你身边绕开。世界还在缓慢地转动。');
    }
  }

  // ============================================================
  //  对话引擎
  // ============================================================
  function itemCount(key) {
    if (GEAR.mats[key]) return g.s.mats[key] || 0;
    return g.s.items[key] || 0;
  }
  function giveItem(key, n) {
    if (GEAR.mats[key]) g.s.mats[key] = (g.s.mats[key] || 0) + n;
    else g.s.items[key] = (g.s.items[key] || 0) + n;
  }
  function takeItem(key, n) {
    if (GEAR.mats[key]) g.s.mats[key] = Math.max(0, (g.s.mats[key] || 0) - n);
    else g.s.items[key] = Math.max(0, (g.s.items[key] || 0) - n);
  }

  function checkCond(c) {
    if (!c) return true;
    if (c.flag && !g.s.flags[c.flag]) return false;
    if (c.no_flag && g.s.flags[c.no_flag]) return false;
    if (c.item && itemCount(c.item) < (c.min_item || 1)) return false;
    if (c.quest) {
      const st = g.s.quests[c.quest] || 0;
      if (c.min != null && st < c.min) return false;
      if (c.max != null && st > c.max) return false;
    }
    if (c.kills != null && g.s.kills < c.kills) return false;
    if (c.deaths != null && g.s.deaths < c.deaths) return false;
    return true;
  }

  function applyEffects(fx) {
    if (!fx) return;
    if (fx.set_flag) g.s.flags[fx.set_flag] = true;
    if (fx.clear_flag) delete g.s.flags[fx.clear_flag];
    if (fx.give) for (const k in fx.give) giveItem(k, fx.give[k]);
    if (fx.take_item) for (const k in fx.take_item) takeItem(k, fx.take_item[k]);
    if (fx.heal) g.p.hp = Math.min(derived().maxHp, g.p.hp + fx.heal);
    if (fx.stamina) g.p.stamina = derived().maxStamina;
    if (fx.max_hp) addMaxHp(fx.max_hp);
    if (fx.damage_bonus) g.s.damageBonus += fx.damage_bonus;
    if (fx.teach_verse) { g.s.flags.verse_learned = true; addXp(15); }
    if (fx.start_quest) {
      if (g.s.quests[fx.start_quest] == null) {
        g.s.quests[fx.start_quest] = 0;
        if (fx.start_quest === 'q_watch') g.s.watchStartKills = g.s.kills;
        toast('接受委托 · ' + questName(fx.start_quest), '按 M 查看世界地图。', 'quest');
        SND.sfx('quest');
      }
    }
    if (fx.advance_quest) {
      const cur = g.s.quests[fx.advance_quest] || 0;
      g.s.quests[fx.advance_quest] = Math.max(cur, fx.min_stage != null ? fx.min_stage : cur + 1);
      SND.sfx('quest');
    }
    if (fx.complete_quest) {
      g.s.quests[fx.complete_quest] = 99;
      toast('委托完成 · ' + questName(fx.complete_quest), '', 'quest');
      SND.sfx('quest');
    }
    if (fx.open_shop) toast('行 商', '陶把货物摊开：晶尘、楔片、余烬，明码标价。');
    if (fx.end) closeDialogue();
  }

  function questName(key) {
    const q = DATA.quests[key];
    return q ? q.name : key;
  }

  function startDialogue(npcKey) {
    const def = DATA.npcs[npcKey];
    if (!def) return;
    g.talking = true;
    g.dlg = { mode: 'npc', def, npcKey, node: null };
    let branch = null;
    for (const b of def.branches) if (checkCond(b.cond)) { branch = b; break; }
    if (!branch) branch = def.branches[def.branches.length - 1];
    g.dlg.branch = branch;
    applyEffects(branch.on_enter);
    if (def.nodes[branch.id]) {
      showNode(branch.id);
    } else {
      let next = 'start';
      if (branch.id === 'done' && def.nodes.charm_give) next = 'charm_give';
      UI.showDialogue(def.name, def.title, branch.text, [{ text: '……', next }], null);
      g.dlg.pendingNext = next;
    }
  }

  function showNode(id) {
    const d = g.dlg;
    if (!d || !d.def) return;
    if (id == null) { closeDialogue(); return; }
    const node = d.def.nodes[id];
    if (!node) { closeDialogue(); return; }
    d.node = node;
    applyEffects(node.on_enter);
    const choices = [];
    (node.choices || []).forEach(c => {
      if (c.cond && !checkCond(c.cond)) return;
      choices.push({ text: c.text, next: c.next, effect: c.effect });
    });
    if (!choices.length) choices.push({ text: '（离开）', next: null });
    UI.showDialogue(d.def.name, d.def.title, node.text, choices, null);
  }

  function chooseDialogue(i) {
    if (!g.dlg) return;
    if (g.dlg.mode === 'lore') { closeDialogue(); return; }
    const d = g.dlg;
    if (d.pendingNext) {
      const n = d.pendingNext; d.pendingNext = null;
      showNode(n);
      return;
    }
    const choices = (d.node && d.node.choices) || [];
    const valid = choices.filter(c => !c.cond || checkCond(c.cond));
    const c = valid[i];
    if (!c) return;
    applyEffects(c.effect);
    if (c.next == null) { closeDialogue(); return; }
    showNode(c.next);
  }

  function closeDialogue() {
    const wasNpc = g.dlg && g.dlg.mode === 'npc';
    g.talking = false; g.dlg = null;
    UI.closeDialogue();
    if (wasNpc) SND.sfx('ui');
  }

  // ============================================================
  //  面板
  // ============================================================
  function openPanel(kind) {
    g.panelKind = kind;
    UI.openPanel(kind);
  }
  function closePanel() {
    g.panelKind = null;
    UI.closePanel();
  }
  g.closePanel = closePanel;

  function canCraft(r) {
    const m = g.s.mats;
    return (m.crystal || 0) >= (r.crystal || 0) && (m.iron || 0) >= (r.iron || 0) &&
      (m.bone || 0) >= (r.bone || 0) && (m.ember || 0) >= (r.ember || 0) &&
      (m.herb || 0) >= (r.herb || 0);
  }
  g.canCraft = canCraft;

  function craft(r) {
    if (!canCraft(r)) { SND.sfx('error'); toast('材料不足', GEAR.recipeCostText(r)); return false; }
    const m = g.s.mats;
    if (r.crystal) m.crystal -= r.crystal;
    if (r.iron) m.iron -= r.iron;
    if (r.bone) m.bone -= r.bone;
    if (r.ember) m.ember -= r.ember;
    if (r.herb) m.herb -= r.herb;
    if (r.result === 'potion') g.s.potions += r.amount;
    else if (r.result === 'max_hp') addMaxHp(r.amount);
    else if (r.result === 'damage') g.s.damageBonus += r.amount;
    addXp(6);
    SND.sfx('pickup');
    toast('调配完成 · ' + r.name, r.desc, 'item');
    return true;
  }

  function upgradeWeapon() {
    const s = g.s, i = s.weapon, lv = s.weaponLv[i];
    if (lv >= 5) { toast('已达上限', '这柄武器已经强化到 +5。'); return; }
    const nc = GEAR.upgradeCrystal(lv), ni = GEAR.upgradeIron(lv);
    if (s.mats.crystal < nc || s.mats.iron < ni) { SND.sfx('error'); toast('材料不足', '需要 晶尘 ' + nc + ' · 楔铁 ' + ni); return; }
    s.mats.crystal -= nc; s.mats.iron -= ni;
    s.weaponLv[i]++;
    applyGear();
    SND.sfx('level');
    burst(g.p.x, g.p.z, '#e8c98a', 16, 'level');
    toast('强化成功 · ' + GEAR.weapons[i].name, '+' + s.weaponLv[i] + ' · 伤害 +4', 'item');
  }

  g.action = function (kind, i) {
    const s = g.s;
    if (kind === 'weapon') {
      if (!s.owned[i]) { SND.sfx('error'); toast('尚未获得', '升级到 2 级 / 4 级，或从稀有遗物匣中寻找。'); return; }
      s.weapon = i;
      applyGear();
      SND.sfx('ui');
      toast('切换武器 · ' + GEAR.weapons[i].name, GEAR.weapons[i].style + ' · ' + GEAR.weapons[i].elementName);
      UI.renderPanel();
    } else if (kind === 'charm') {
      if (!s.charms[i]) { SND.sfx('error'); toast('尚未获得', '击败精英敌人或开启稀有遗物匣。'); return; }
      s.charm = s.charm === i ? -1 : i;
      applyGear();
      SND.sfx('ui');
      UI.renderPanel();
    } else if (kind === 'travel') {
      const pts = g.travelPoints();
      const t = pts[i];
      if (!t || !t.unlocked) { SND.sfx('error'); return; }
      closePanel();
      UI.fade(true, 0.35);
      setTimeout(() => {
        g.p.x = t.x; g.p.z = t.z; g.p.y = WORLD.heightAt(t.x, t.z);
        g.p.hp = Math.min(derived().maxHp, g.p.hp + 25);
        g.p.stamina = derived().maxStamina;
        g.cam.x = g.p.x; g.cam.z = g.p.z;
        s.time += 10;
        g.enemies.forEach(e => { e.dead = true; e.deathT = 0.3; });
        g.enemies.length = 0;
        UI.fade(false, 0.8);
        toast('抵达 · ' + t.name, '十分钟过去了。');
      }, 380);
    } else if (kind === 'camp') {
      if (i === 0) {
        g.p.hp = derived().maxHp; g.p.stamina = derived().maxStamina;
        s.respawn = { x: g.p.x, z: g.p.z };
        s.time += 60;
        g.s.flags['camp_camp_0'] = true;
        SND.sfx('heal');
        toast('休 息', '生命与耐力已恢复。世界过去了一小时。');
      } else if (i === 1) {
        upgradeWeapon();
      }
      UI.renderPanel();
    } else if (kind === 'craft') {
      const r = GEAR.recipes[i];
      if (!r) return;
      if (craft(r)) UI.renderPanel();
    }
  };

  g.derived = derived;
  g.travelPoints = function () {
    const out = [];
    g.pois.forEach(o => {
      if (o.kind === 'campfire') {
        const unlocked = o.id === 'camp_0' || g.s.flags['camp_' + o.id];
        out.push({ name: o.name, x: o.x, z: o.z, unlocked, kind: '营火', dist: len(o.x - g.p.x, o.z - g.p.z) });
      } else if (o.kind === 'shrine') {
        out.push({ name: o.name, x: o.x, z: o.z, unlocked: g.s.shrines[o.data.idx], kind: '楔坛', dist: len(o.x - g.p.x, o.z - g.p.z) });
      } else if (o.kind === 'altar') {
        out.push({ name: '律令之楔', x: o.x, z: o.z, unlocked: g.s.shrines.filter(Boolean).length >= 5, kind: '峰顶', dist: len(o.x - g.p.x, o.z - g.p.z) });
      }
    });
    return out.filter(p => p.unlocked).sort((a, b) => a.dist - b.dist);
  };

  // ============================================================
  //  队伍 / 世界事件 / 时间
  // ============================================================
  function questInfo() {
    const s = g.s, f = s.flags;
    if (!f.met_zhonglou) {
      return { title: '锈钟的低语', stage: '与钟漏交谈', progress: '', hint: '营火东侧，悬浮的构装体。' };
    }
    if (!f.verse_learned) {
      if (s.mats.crystal < 3) {
        return { title: '锈钟的低语', stage: '收集 3 枚晶尘', progress: s.mats.crystal + ' / 3', hint: '击碎青铜林里发光的晶簇。' };
      }
      return { title: '锈钟的低语', stage: '把晶尘带回给钟漏', progress: '3 / 3', hint: '按 M 可查看地图。' };
    }
    if (!f.met_watch && !f.knows_choice) {
      return { title: '荒原上的人', stage: '在营地附近寻找其他幸存者', progress: '', hint: '守夜人砌、行商陶、回声。' };
    }
    if (f.met_watch && !f.watch_done) {
      const n = Math.min(5, Math.max(0, s.kills - s.watchStartKills));
      return { title: '守夜人的委托', stage: '清理 5 个游荡的构装体', progress: n + ' / 5', hint: n >= 5 ? '回营地向砌复命。' : '夜里它们会更多。' };
    }
    if (f.met_echo && !f.knows_choice) {
      return { title: '回声的三次矛盾', stage: '听完回声的三段叙述', progress: '', hint: '东边的回声在等你。' };
    }
    const n = s.shrines.filter(Boolean).length;
    if (n < 5) return { title: '五楔之谜', stage: '点亮五座楔之祭坛', progress: n + ' / 5', hint: '按 M 查看已点亮的祭坛。' };
    if (!s.endingSeen) return { title: '楔之峰', stage: '登上峰顶，触碰律令之楔', progress: '5 / 5', hint: '世界中心，海拔 128 米。' };
    return { title: '世界在缓慢转动', stage: '继续漫游荒原', progress: '已通关', hint: '按 J 查看手记。' };
  }
  g.questInfo = questInfo;

  function weatherName() {
    const map = { clear: '晴朗', cloudy: '多云', mist: '薄雾', rain: '细雨', dust: '沙尘', crystal: '晶尘雨' };
    return map[g.s.weather] || '薄雾';
  }
  g.weatherName = weatherName;
  g.clockText = function () {
    const t = ((g.s.time % 1440) + 1440) % 1440;
    const h = Math.floor(t / 60), m = Math.floor(t % 60);
    return (h < 10 ? '0' : '') + h + ':' + (m < 10 ? '0' : '') + m;
  };
  g.regionName = function () { return WORLD.label(g.p.x, g.p.z); };
  g.entCount = function () { return g.enemies.length + g.pois.length; };

  function pickWeather() {
    const h = (g.s.time / 60) % 24;
    const r = Math.random();
    if (h >= 20 || h < 5) {
      if (r < 0.30) return 'clear';
      if (r < 0.55) return 'cloudy';
      if (r < 0.78) return 'mist';
      return 'crystal';
    }
    if (r < 0.36) return 'clear';
    if (r < 0.62) return 'cloudy';
    if (r < 0.76) return 'mist';
    if (r < 0.88) return 'rain';
    if (r < 0.95) return 'dust';
    return 'crystal';
  }

  function updateTime(dt) {
    const s = g.s;
    s.time += dt;
    if (s.time >= 1440) { s.time -= 1440; s.day++; toast('第 ' + s.day + ' 日', '世界又转了一圈。', 'ach'); }
    s.weatherTimer -= dt;
    if (s.weatherTimer <= 0) {
      s.weatherTimer = 140 + Math.random() * 140;
      s.weather = pickWeather();
      g.weatherBlend = 0;
      toast('天 气 · ' + weatherName(), '', '');
    }
    g.weatherBlend = Math.min(1, g.weatherBlend + dt * 0.7);
  }

  function nightAmount() {
    const h = (g.s.time / 60) % 24;
    if (h >= 21 || h < 4.5) return 1;
    if (h >= 18.5) return clamp((h - 18.5) / 2.5, 0, 1);
    if (h < 6.5) return clamp((6.5 - h) / 2, 0, 1);
    return 0;
  }

  function updateWorldEvents(dt) {
    g.eventT -= dt;
    if (g.eventT > 0) return;
    g.eventT = 95 + Math.random() * 55;
    const roll = Math.random();
    if (roll < 0.38) {
      const [x, z] = spawnPointNear(g.p.x, g.p.z, 11, 16);
      for (let i = 0; i < 3; i++) {
        const a = i / 3 * TAU;
        spawnEnemy(i === 2 ? 'winged' : 'construct', x + Math.cos(a) * 2.2, z + Math.sin(a) * 2.2,
          Math.min(8, 1 + Math.floor(g.s.kills / 8)), pickAffix(2));
      }
      toast('荒原的低语', '三名构造体从尘雾里围了上来。', 'bad');
      SND.sfx('swing');
      g.s.lastEvent = '三名构造体伏击了你。';
    } else if (roll < 0.70) {
      g.s.mats.crystal += 3;
      toast('晶尘雨', '风里落下细碎的晶尘。晶尘 +3', 'item');
      g.s.lastEvent = '一场晶尘雨落过荒原。';
      burst(g.p.x + 6, g.p.z, '#a9dfe8', 26, 'crystal');
    } else {
      const xp = 24 + g.s.level * 8;
      addXp(xp);
      toast('遗迹回响', '你听见一段不属于此刻的对话。经验 +' + Math.round(xp * derived().xpGain), '');
      g.s.lastEvent = '遗迹的回响在你耳边停留了一会儿。';
      SND.sfx('quest');
    }
  }

  // ============================================================
  //  存档
  // ============================================================
  function save() {
    if (!g.started) return;
    try {
      const s = g.s;
      s.hp = g.p.hp; s.stamina = g.p.stamina; s.law = g.p.law;
      const pois = {};
      g.pois.forEach(o => { pois[o.id] = { u: o.used, h: o.hidden, t: o.respawnT }; });
      const data = { v: 5, s, p: { x: g.p.x, z: g.p.z }, pois, cam: { x: g.cam.x, z: g.cam.z } };
      localStorage.setItem(SAVE_KEY, JSON.stringify(data));
      g.hasSave = true;
    } catch (e) { /* 本地文件环境可能限制存储 */ }
  }

  g.save = save;

  function loadGame() {
    try {
      const raw = localStorage.getItem(SAVE_KEY);
      if (!raw) return false;
      const data = JSON.parse(raw);
      if (!data || !data.s) return false;
      const base = defaultState();
      g.s = Object.assign(base, data.s);
      g.s.mats = Object.assign(base.mats, data.s.mats || {});
      g.s.items = Object.assign(base.items, data.s.items || {});
      g.s.flags = data.s.flags || {};
      g.s.quests = data.s.quests || {};
      g.s.bestiary = Object.assign(base.bestiary, data.s.bestiary || {});
      g.s.shrines = (data.s.shrines || base.shrines).slice(0, 5);
      g.s.weaponLv = (data.s.weaponLv || [0, 0, 0]).slice(0, 3);
      g.s.owned = (data.s.owned || [true, false, false]).slice(0, 3);
      g.s.charms = (data.s.charms || base.charms).slice(0, 5);
      g.s.achievements = data.s.achievements || [];
      resetPois();
      if (data.pois) {
        g.pois.forEach(o => {
          const st = data.pois[o.id];
          if (st) { o.used = !!st.u; o.hidden = !!st.h; o.respawnT = st.t || 0; }
        });
      }
      makePlayer();
      if (data.p) { g.p.x = data.p.x; g.p.z = data.p.z; g.p.y = WORLD.heightAt(g.p.x, g.p.z); }
      g.p.hp = clamp(data.s.hp != null ? data.s.hp : g.s.hpBase, 1, derived().maxHp);
      g.p.stamina = clamp(data.s.stamina != null ? data.s.stamina : g.s.staminaBase, 1, derived().maxStamina);
      g.p.law = data.s.law || 0;
      g.s.respawn = data.s.respawn || { x: DATA.spawn[0], z: DATA.spawn[1] };
      g.cam.x = g.p.x; g.cam.z = g.p.z;
      applyGear();
      g.enemies.length = 0;
      spawnInitialEnemies();
      g.started = true;
      UI.hideTitle();
      UI.fade(false, 0.2);
      toast('欢迎回来', '第 ' + g.s.day + ' 日 · ' + g.clockText() + ' · 位于' + g.regionName());
      return true;
    } catch (e) {
      console.warn('读档失败', e);
      return false;
    }
  }

  g.startNew = function () {
    g.s = defaultState();
    if (/[?&]clean=1/.test(location.search)) { g.s.time = 12 * 60; g.s.weather = 'clear'; }
    resetPois();
    makePlayer();
    applyGear();
    spawnInitialEnemies();
    g.started = true;
    g.dead = false;
    if (/[?&]forcehud=1/.test(location.search)) {
      const h = document.getElementById('hud');
      h.style.opacity = '1';
      h.style.transition = 'none';
    }
    UI.hideTitle();
    UI.fade(true, 0.2);
    setTimeout(() => UI.fade(false, 1.0), 220);
    toast('青铜林', '风从北方的泪湖吹来，带着铁锈和潮气。', '');
    setTimeout(() => toast('醒 来', '你记不清自己是谁。只记得剑在右手。', ''), 1800);
    setTimeout(() => toast('提示', 'WASD 移动 · 左键攻击 · 右键格挡 · 空格翻滚 · E 交互', ''), 3600);
    SND.ensure();
    g.s.intro = true;
  };

  g.continueGame = function () {
    if (!loadGame()) g.startNew();
  };

  g.chooseDialogue = chooseDialogue;
  g.closeDialogue = closeDialogue;

  // ============================================================
  //  绘 制
  // ============================================================
  const cv = document.getElementById('game');
  const ctx = cv.getContext('2d');
  let DPR = Math.min(2, window.devicePixelRatio || 1);

  function resize() {
    g.W = window.innerWidth; g.H = window.innerHeight;
    DPR = Math.min(2, window.devicePixelRatio || 1);
    cv.width = Math.floor(g.W * DPR);
    cv.height = Math.floor(g.H * DPR);
    cv.style.width = g.W + 'px';
    cv.style.height = g.H + 'px';
    ctx.setTransform(DPR, 0, 0, DPR, 0, 0);
    g.atlas.light = document.createElement('canvas');
    g.atlas.light.width = Math.floor(g.W * 0.6);
    g.atlas.light.height = Math.floor(g.H * 0.6);
  }
  window.addEventListener('resize', resize);

  function proj(x, z) { return WORLD.project(x, z, g.cam, g.W, g.H); }

  function shadowEllipse(sx, sy, w, h, a) {
    ctx.fillStyle = 'rgba(0,0,0,' + (a || 0.3) + ')';
    ctx.beginPath(); ctx.ellipse(sx, sy, w, h, 0, 0, TAU); ctx.fill();
  }

  function groundGlow(x, z, r, color, alpha) {
    const [sx, sy] = proj(x, z);
    const gd = ctx.createRadialGradient(sx, sy, 0, sx, sy, r * g.cam.zoom);
    gd.addColorStop(0, color.replace('ALPHA', alpha));
    gd.addColorStop(1, color.replace('ALPHA', '0'));
    ctx.fillStyle = gd;
    ctx.beginPath(); ctx.ellipse(sx, sy, r * g.cam.zoom, r * g.cam.zoom * WORLD.YS, 0, 0, TAU); ctx.fill();
  }

  function drawTerrain() {
    const cam = g.cam;
    ctx.fillStyle = '#0b0f0d';
    ctx.fillRect(0, 0, g.W, g.H);
    WORLD.update(cam, g.W, g.H);
    WORLD.drawTerrain(ctx, cam, g.W, g.H);
    WORLD.drawWater(ctx, cam, g.W, g.H, performance.now() / 1000);
  }

  function drawGroundDecals() {
    const p = g.p;
    // 篝火光圈
    for (const o of g.pois) {
      if (o.kind === 'campfire' && !o.hidden) {
        const d = len(o.x - p.x, o.z - p.z);
        if (d < 90) groundGlow(o.x, o.z, 4.8, 'rgba(224,138,80,ALPHA)', 0.16);
      } else if (o.kind === 'shrine' && !o.used) {
        const d = len(o.x - p.x, o.z - p.z);
        if (d < 80) groundGlow(o.x, o.z, 6, 'rgba(226,194,122,ALPHA)', 0.13);
      }
    }
    // 敌人预警
    for (const e of g.enemies) {
      if (e.dead) continue;
      const [sx, sy] = proj(e.x, e.z);
      if (e.windup > 0) {
        const t = 1 - e.windup / e.windMax;
        ctx.strokeStyle = 'rgba(224,110,74,' + (0.35 + t * 0.5) + ')';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.ellipse(sx, sy, (e.range + 1.2) * g.cam.zoom * (1.1 - t * 0.35),
          (e.range + 1.2) * g.cam.zoom * WORLD.YS * (1.1 - t * 0.35), 0, 0, TAU);
        ctx.stroke();
        // 扇形
        ctx.fillStyle = 'rgba(224,90,60,.09)';
        ctx.beginPath();
        ctx.moveTo(sx, sy);
        const a0 = e.yaw - 0.8, a1 = e.yaw + 0.8;
        ctx.ellipse(sx, sy, (e.range + 1.2) * g.cam.zoom, (e.range + 1.2) * g.cam.zoom * WORLD.YS, 0, a0, a1);
        ctx.closePath(); ctx.fill();
      }
      if (g.target === e) {
        ctx.strokeStyle = 'rgba(226,194,122,.55)';
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.ellipse(sx, sy, (e.r + 0.7) * g.cam.zoom, (e.r + 0.7) * g.cam.zoom * WORLD.YS, 0, 0, TAU);
        ctx.stroke();
      }
    }
    // 玩家脚下律令
    if (p.lawT > 0) {
      const [sx, sy] = proj(p.x, p.z);
      const t = performance.now() / 400;
      ctx.strokeStyle = 'rgba(226,194,122,' + (0.25 + Math.sin(t) * 0.1) + ')';
      ctx.lineWidth = 1.5;
      for (let i = 0; i < 3; i++) {
        ctx.beginPath();
        ctx.ellipse(sx, sy, (1.4 + i * 0.55) * g.cam.zoom, (1.4 + i * 0.55) * g.cam.zoom * WORLD.YS, 0, t + i, t + i + 1.6);
        ctx.stroke();
      }
    }
  }

  // ---------- 角色 ----------
  function drawPlayer() {
    const p = g.p;
    const [sx, sy] = proj(p.x, p.z);
    const u = clamp(g.cam.zoom / 26, 0.7, 1.7);
    const bob = p.bob || 0;
    shadowEllipse(sx, sy, 13 * u, 4.6 * u, 0.34);
    if (p.dead) {
      ctx.globalAlpha = 0.6;
      ctx.fillStyle = '#5a5244';
      ctx.beginPath(); ctx.ellipse(sx, sy - 8 * u, 10 * u, 26 * u, 1.2, 0, TAU); ctx.fill();
      ctx.globalAlpha = 1;
      return;
    }
    const y0 = sy - bob;
    const face = Math.sin(p.yaw), faceZ = Math.cos(p.yaw);
    const flip = face < -0.1 ? -1 : 1;
    // 律令光环
    if (p.lawT > 0) {
      const gd = ctx.createRadialGradient(sx, y0 - 22 * u, 4, sx, y0 - 22 * u, 48 * u);
      gd.addColorStop(0, 'rgba(255,224,160,.20)');
      gd.addColorStop(1, 'rgba(255,224,160,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, y0 - 22 * u, 48 * u, 0, TAU); ctx.fill();
    }
    // 披风
    const sway = Math.sin(p.walk * 1.4) * 3 * u;
    ctx.fillStyle = '#3f3327';
    ctx.beginPath();
    ctx.moveTo(sx - 12 * u, y0 - 36 * u);
    ctx.quadraticCurveTo(sx - 20 * u + sway, y0 - 14 * u, sx - 10 * u, y0 - 1 * u);
    ctx.lineTo(sx + 10 * u, y0 - 1 * u);
    ctx.quadraticCurveTo(sx + 17 * u + sway, y0 - 16 * u, sx + 12 * u, y0 - 36 * u);
    ctx.closePath(); ctx.fill();
    // 腿
    const step = Math.sin(p.walk * 2) * 3 * u;
    ctx.strokeStyle = '#2e2820'; ctx.lineWidth = 4.6 * u; ctx.lineCap = 'round';
    ctx.beginPath(); ctx.moveTo(sx - 4 * u, y0 - 14 * u); ctx.lineTo(sx - 5 * u + step, y0 - 1 * u); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(sx + 4 * u, y0 - 14 * u); ctx.lineTo(sx + 5 * u - step, y0 - 1 * u); ctx.stroke();
    // 躯干
    ctx.fillStyle = '#c8a35c';
    ctx.beginPath();
    ctx.moveTo(sx - 9 * u, y0 - 38 * u);
    ctx.lineTo(sx + 9 * u, y0 - 38 * u);
    ctx.lineTo(sx + 7 * u, y0 - 15 * u);
    ctx.lineTo(sx - 7 * u, y0 - 15 * u);
    ctx.closePath(); ctx.fill();
    ctx.fillStyle = 'rgba(90,60,35,.55)';
    ctx.fillRect(sx - 8 * u, y0 - 26 * u, 16 * u, 3 * u);
    // 肩甲
    ctx.fillStyle = '#8a6f42';
    ctx.beginPath(); ctx.ellipse(sx - 11 * u, y0 - 36 * u, 5 * u, 4 * u, 0.2, 0, TAU); ctx.fill();
    ctx.beginPath(); ctx.ellipse(sx + 11 * u, y0 - 36 * u, 5 * u, 4 * u, -0.2, 0, TAU); ctx.fill();
    // 头
    ctx.fillStyle = '#d8c39a';
    ctx.beginPath(); ctx.arc(sx + flip * 2 * u, y0 - 46 * u, 7.4 * u, 0, TAU); ctx.fill();
    ctx.fillStyle = '#2c241b';
    ctx.beginPath(); ctx.arc(sx + flip * 2 * u, y0 - 49 * u, 6.6 * u, Math.PI, TAU); ctx.fill();
    // 眼
    if (faceZ > -0.3) {
      ctx.fillStyle = '#2b2118';
      ctx.fillRect(sx + flip * 3.2 * u - 2.4 * u, y0 - 46 * u, 1.6 * u, 1.6 * u);
      ctx.fillRect(sx + flip * 3.2 * u + 1.2 * u, y0 - 46 * u, 1.6 * u, 1.6 * u);
    }
    // 格挡盾
    if (p.blocking) {
      const bx = sx + Math.sin(p.yaw + Math.PI) * 18 * u;
      const by = y0 - 26 * u;
      ctx.save();
      ctx.translate(bx, by);
      ctx.rotate(Math.atan2(Math.cos(p.yaw) * 0.82, Math.sin(p.yaw)) - Math.PI / 2);
      ctx.fillStyle = 'rgba(200,170,110,.35)';
      ctx.strokeStyle = '#e2c27a'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.arc(0, 0, 17 * u, -0.9, 0.9); ctx.stroke();
      ctx.restore();
    }
    // 剑
    drawWeapon(sx, y0, u, p);
    // 蓄力
    if (p.heavyT >= 0) {
      const t = clamp(p.heavyT, 0, 1);
      ctx.strokeStyle = 'rgba(255,200,120,' + (0.2 + t * 0.6) + ')';
      ctx.lineWidth = 3 * u;
      ctx.beginPath(); ctx.arc(sx, y0 - 24 * u, (22 + t * 16) * u, -2.6, -0.5); ctx.stroke();
    }
  }

  function drawWeapon(sx, y0, u, p) {
    const w = GEAR.weapons[g.s.weapon];
    let ang = p.yaw;
    if (p.swingT >= 0) {
      const t = clamp(p.swingT, 0, 1);
      const ease = t < 0.35 ? (t / 0.35) : 1 - (t - 0.35) / 0.65 * 0.55;
      ang = p.swingYaw + (ease - 0.5) * w.arc * 2;
    }
    const hx = sx + Math.cos(ang) * 12 * u;
    const hy = y0 - 28 * u + Math.sin(ang) * 4 * u;
    const L = (w.key === 'ember' ? 40 : w.key === 'frost' ? 30 : 35) * u;
    const tipX = hx + Math.sin(ang) * L;
    const tipY = hy - Math.cos(ang) * L * 0.75;
    // 剑身
    ctx.strokeStyle = w.key === 'bronze' ? '#e8d5a8' : w.key === 'frost' ? '#cdeef2' : '#e8b08a';
    ctx.lineWidth = (w.key === 'ember' ? 5 : 3) * u;
    ctx.lineCap = 'round';
    ctx.beginPath(); ctx.moveTo(hx, hy); ctx.lineTo(tipX, tipY); ctx.stroke();
    ctx.strokeStyle = 'rgba(255,255,255,.7)'; ctx.lineWidth = 1 * u;
    ctx.beginPath(); ctx.moveTo(hx, hy); ctx.lineTo(tipX, tipY); ctx.stroke();
    // 护手
    ctx.strokeStyle = '#7a5e38'; ctx.lineWidth = 2.4 * u;
    ctx.beginPath(); ctx.moveTo(hx - 5 * u, hy); ctx.lineTo(hx + 5 * u, hy); ctx.stroke();
    // 挥砍轨迹
    if (p.swingT >= 0 && p.swingT < 0.85) {
      const t = clamp(p.swingT / 0.85, 0, 1);
      ctx.save();
      ctx.globalAlpha = (1 - t) * 0.5;
      ctx.strokeStyle = w.glow;
      ctx.lineWidth = 10 * u;
      ctx.lineCap = 'round';
      ctx.beginPath();
      const a0 = p.swingYaw - w.arc + t * w.arc * 2 - 0.2;
      ctx.arc(sx, y0 - 20 * u, (w.range * 0.55 + 8) * u, a0 - 0.5, a0 + 0.6);
      ctx.stroke();
      ctx.restore();
    }
  }

  function drawEnemy(e) {
    const [sx, sy] = proj(e.x, e.z);
    const u = clamp(g.cam.zoom / 26, 0.7, 1.7) * (e.size || 1);
    const bob = Math.sin(e.anim * 2.2) * 2 * u;
    if (e.dead) {
      ctx.globalAlpha = clamp(e.deathT / 1.15, 0, 1);
      shadowEllipse(sx, sy, 13 * u, 4 * u, 0.3);
      ctx.fillStyle = '#443c30';
      ctx.beginPath(); ctx.ellipse(sx, sy - 6 * u, 16 * u, 9 * u, 0.4, 0, TAU); ctx.fill();
      ctx.globalAlpha = 1;
      return;
    }
    shadowEllipse(sx, sy, 12 * u * (e.kind === 'lava' ? 1.35 : 1), 4 * u, 0.32);
    if (e.affix) {
      const col = e.affix === 1 ? '#a9dfe8' : e.affix === 2 ? '#d8c39a' : '#e08a6a';
      ctx.strokeStyle = col; ctx.globalAlpha = 0.4;
      ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.ellipse(sx, sy, 15 * u, 5.5 * u, 0, 0, TAU); ctx.stroke();
      ctx.globalAlpha = 1;
    }
    if (e.kind === 'winged') {
      const flap = Math.sin(e.anim * 7) * 0.5;
      ctx.fillStyle = 'rgba(120,200,180,.20)';
      ctx.beginPath(); ctx.ellipse(sx - 16 * u, sy - 40 * u + bob, 18 * u, 7 * u, -0.5 + flap, 0, TAU); ctx.fill();
      ctx.beginPath(); ctx.ellipse(sx + 16 * u, sy - 40 * u + bob, 18 * u, 7 * u, 0.5 - flap, 0, TAU); ctx.fill();
      ctx.fillStyle = 'rgba(127,193,176,.75)';
      ctx.beginPath();
      ctx.moveTo(sx - 9 * u, sy - 4 * u + bob);
      ctx.quadraticCurveTo(sx - 13 * u, sy - 44 * u + bob, sx, sy - 52 * u + bob);
      ctx.quadraticCurveTo(sx + 13 * u, sy - 44 * u + bob, sx + 9 * u, sy - 4 * u + bob);
      ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#dff3ec';
      ctx.beginPath(); ctx.arc(sx, sy - 46 * u + bob, 4.5 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = '#1d2b28';
      ctx.fillRect(sx - 2.8 * u, sy - 47 * u + bob, 1.5 * u, 2 * u);
      ctx.fillRect(sx + 1.3 * u, sy - 47 * u + bob, 1.5 * u, 2 * u);
    } else if (e.kind === 'lava') {
      ctx.fillStyle = '#6e3a2a';
      ctx.beginPath(); ctx.ellipse(sx, sy - 18 * u + bob, 20 * u, 16 * u, 0, 0, TAU); ctx.fill();
      ctx.fillStyle = '#8e4b33';
      ctx.beginPath(); ctx.ellipse(sx, sy - 28 * u + bob, 14 * u, 12 * u, 0, 0, TAU); ctx.fill();
      ctx.strokeStyle = '#f0a06a'; ctx.lineWidth = 2 * u;
      for (let i = 0; i < 4; i++) {
        ctx.beginPath();
        ctx.moveTo(sx - 12 * u + i * 7 * u, sy - 10 * u + bob);
        ctx.lineTo(sx - 8 * u + i * 6 * u, sy - 24 * u + bob);
        ctx.stroke();
      }
      ctx.fillStyle = '#ffd08a';
      ctx.beginPath(); ctx.arc(sx - 6 * u, sy - 32 * u + bob, 2.2 * u, 0, TAU); ctx.fill();
      ctx.beginPath(); ctx.arc(sx + 6 * u, sy - 32 * u + bob, 2.2 * u, 0, TAU); ctx.fill();
    } else {
      // 构造机偶
      ctx.fillStyle = '#7d6d52';
      ctx.fillRect(sx - 13 * u, sy - 42 * u + bob, 26 * u, 34 * u);
      ctx.fillStyle = '#9a8a6a';
      ctx.fillRect(sx - 15 * u, sy - 44 * u + bob, 30 * u, 8 * u);
      ctx.fillStyle = '#5f523c';
      ctx.fillRect(sx - 9 * u, sy - 8 * u + bob, 7 * u, 8 * u);
      ctx.fillRect(sx + 2 * u, sy - 8 * u + bob, 7 * u, 8 * u);
      ctx.fillStyle = '#2c2620';
      ctx.fillRect(sx - 8 * u, sy - 36 * u + bob, 16 * u, 9 * u);
      ctx.fillStyle = '#e06a4a';
      ctx.beginPath(); ctx.arc(sx - 3.5 * u, sy - 31.5 * u + bob, 2.3 * u, 0, TAU); ctx.fill();
      ctx.beginPath(); ctx.arc(sx + 3.5 * u, sy - 31.5 * u + bob, 2.3 * u, 0, TAU); ctx.fill();
      ctx.strokeStyle = '#4a4033'; ctx.lineWidth = 3 * u;
      ctx.beginPath(); ctx.moveTo(sx - 14 * u, sy - 36 * u + bob); ctx.lineTo(sx - 22 * u, sy - 18 * u + bob); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(sx + 14 * u, sy - 36 * u + bob); ctx.lineTo(sx + 22 * u, sy - 18 * u + bob); ctx.stroke();
      // 武器前摇
      if (e.windup > 0) {
        ctx.strokeStyle = 'rgba(240,160,106,.8)'; ctx.lineWidth = 3 * u;
        const a = e.yaw;
        ctx.beginPath();
        ctx.moveTo(sx + Math.sin(a) * 14 * u, sy - 30 * u + bob);
        ctx.lineTo(sx + Math.sin(a) * 34 * u, sy - 30 * u + bob - Math.cos(a) * 10 * u);
        ctx.stroke();
      }
    }
    if (e.hitFlash > 0) {
      ctx.globalAlpha = clamp(e.hitFlash * 4, 0, 0.7);
      ctx.fillStyle = '#fff2d8';
      ctx.beginPath(); ctx.ellipse(sx, sy - 24 * u, 20 * u, 26 * u, 0, 0, TAU); ctx.fill();
      ctx.globalAlpha = 1;
    }
    if (e.chillT > 0) {
      ctx.fillStyle = 'rgba(169,223,232,.22)';
      ctx.beginPath(); ctx.ellipse(sx, sy - 24 * u, 19 * u, 25 * u, 0, 0, TAU); ctx.fill();
    }
    // 血条
    if ((g.target === e || e.elite) && !e.dead) {
      const w = 44 * u;
      ctx.fillStyle = 'rgba(0,0,0,.55)'; ctx.fillRect(sx - w / 2, sy - 60 * u, w, 3.5);
      ctx.fillStyle = e.elite ? '#e08a6a' : '#b45a35';
      ctx.fillRect(sx - w / 2, sy - 60 * u, w * clamp(e.hp / e.maxHp, 0, 1), 3.5);
      if (e.elite) {
        ctx.fillStyle = '#e2c27a'; ctx.font = '10px ' + FONT; ctx.textAlign = 'center';
        ctx.fillText(e.title + ' · Lv.' + e.level, sx, sy - 65 * u);
      }
    }
    if (e.stunT > 0) {
      ctx.fillStyle = '#e2c27a'; ctx.font = '13px ' + FONT; ctx.textAlign = 'center';
      ctx.fillText('✦', sx, sy - 66 * u);
    }
  }

  function drawNPC(o) {
    const [sx, sy] = proj(o.x, o.z);
    const u = clamp(g.cam.zoom / 26, 0.7, 1.7);
    const key = o.npcKey;
    const t = performance.now() / 1000;
    shadowEllipse(sx, sy, 12 * u, 4 * u, 0.3);
    if (key === 'zhonglou') {
      const bob = Math.sin(t * 1.3) * 2 * u;
      ctx.strokeStyle = 'rgba(226,194,122,.4)'; ctx.lineWidth = 1.4;
      ctx.beginPath(); ctx.ellipse(sx, sy - 34 * u + bob, 19 * u, 7 * u, 0, 0, TAU); ctx.stroke();
      ctx.fillStyle = '#6d5a3c';
      ctx.fillRect(sx - 11 * u, sy - 42 * u + bob, 22 * u, 28 * u);
      ctx.fillStyle = '#8a7148';
      ctx.fillRect(sx - 13 * u, sy - 46 * u + bob, 26 * u, 6 * u);
      ctx.fillStyle = '#e2c27a';
      ctx.beginPath(); ctx.arc(sx, sy - 28 * u + bob, 7 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = '#3a2f20';
      ctx.beginPath(); ctx.arc(sx, sy - 28 * u + bob, 4.5 * u, 0, TAU); ctx.fill();
      ctx.strokeStyle = '#e2c27a'; ctx.lineWidth = 1.4;
      ctx.beginPath(); ctx.moveTo(sx, sy - 28 * u + bob); ctx.lineTo(sx + 4 * u, sy - 31 * u + bob); ctx.stroke();
      ctx.fillStyle = '#5a4a32';
      ctx.fillRect(sx - 6 * u, sy - 14 * u + bob, 4 * u, 14 * u);
      ctx.fillRect(sx + 2 * u, sy - 14 * u + bob, 4 * u, 14 * u);
    } else if (key === 'shouye') {
      const bob = Math.sin(t * 1.1) * 1.5 * u;
      ctx.fillStyle = '#3d4650';
      ctx.beginPath();
      ctx.moveTo(sx - 12 * u, sy - 2 * u);
      ctx.lineTo(sx - 7 * u, sy - 52 * u + bob);
      ctx.lineTo(sx + 7 * u, sy - 52 * u + bob);
      ctx.lineTo(sx + 12 * u, sy - 2 * u);
      ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#2c333a';
      ctx.beginPath(); ctx.arc(sx, sy - 55 * u + bob, 7 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = '#e8c98a';
      ctx.beginPath(); ctx.arc(sx + 12 * u, sy - 26 * u + bob, 4.5 * u, 0, TAU); ctx.fill();
      const gd = ctx.createRadialGradient(sx + 12 * u, sy - 26 * u + bob, 0, sx + 12 * u, sy - 26 * u + bob, 34 * u);
      gd.addColorStop(0, 'rgba(240,200,120,.22)'); gd.addColorStop(1, 'rgba(240,200,120,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx + 12 * u, sy - 26 * u + bob, 34 * u, 0, TAU); ctx.fill();
      ctx.strokeStyle = '#2c333a'; ctx.lineWidth = 3 * u;
      ctx.beginPath(); ctx.moveTo(sx + 8 * u, sy - 36 * u + bob); ctx.lineTo(sx + 12 * u, sy - 26 * u + bob); ctx.stroke();
    } else if (key === 'tao') {
      const bob = Math.sin(t * 1.6) * 1.5 * u;
      ctx.fillStyle = '#6a4f38';
      ctx.fillRect(sx - 10 * u, sy - 40 * u + bob, 20 * u, 30 * u);
      ctx.fillStyle = '#8a6a48';
      ctx.beginPath(); ctx.arc(sx, sy - 45 * u + bob, 7 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = '#3c2f22';
      ctx.beginPath(); ctx.arc(sx, sy - 47 * u + bob, 7.4 * u, Math.PI, TAU); ctx.fill();
      ctx.fillStyle = '#4a3b28';
      ctx.fillRect(sx + 8 * u, sy - 38 * u + bob, 8 * u, 20 * u);
      ctx.fillStyle = '#c8a35c';
      ctx.fillRect(sx - 6 * u, sy - 34 * u + bob, 12 * u, 2 * u);
      ctx.fillStyle = 'rgba(200,163,92,.45)';
      ctx.beginPath(); ctx.arc(sx - 8 * u, sy - 30 * u + bob, 2 * u, 0, TAU); ctx.fill();
    } else {
      const bob = Math.sin(t * 1.9) * 3 * u;
      const gd = ctx.createRadialGradient(sx, sy - 30 * u + bob, 0, sx, sy - 30 * u + bob, 26 * u);
      gd.addColorStop(0, 'rgba(169,223,232,.35)'); gd.addColorStop(1, 'rgba(169,223,232,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, sy - 30 * u + bob, 26 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = 'rgba(200,240,236,.75)';
      ctx.beginPath(); ctx.arc(sx, sy - 30 * u + bob, 6 * u, 0, TAU); ctx.fill();
      ctx.strokeStyle = 'rgba(200,240,236,.4)'; ctx.lineWidth = 1.2;
      for (let i = 0; i < 3; i++) {
        const a = t * 0.8 + i * 2.1;
        ctx.beginPath();
        ctx.arc(sx + Math.cos(a) * 10 * u, sy - 30 * u + bob + Math.sin(a) * 4 * u, 3 * u, 0, TAU);
        ctx.stroke();
      }
    }
    // 名字
    const d = len(o.x - g.p.x, o.z - g.p.z);
    if (d < 12) {
      ctx.fillStyle = 'rgba(230,220,196,.82)';
      ctx.font = '12px ' + FONT; ctx.textAlign = 'center';
      ctx.fillText(o.name, sx, sy - 66 * u);
      ctx.fillStyle = 'rgba(200,163,92,.6)';
      ctx.font = '9px ' + FONT;
      ctx.fillText(DATA.npcs[key].title, sx, sy - 80 * u);
    }
  }

  // ---------- POI ----------
  function drawPoi(o) {
    if (o.hidden) return;
    if (o.kind === 'npc') return;
    const [sx, sy] = proj(o.x, o.z);
    const u = clamp(g.cam.zoom / 26, 0.7, 1.7);
    const t = performance.now() / 1000;
    const dim = o.used && (o.kind === 'crystal' || o.kind === 'herb' || o.kind === 'chest');
    if (dim) return;
    if (o.kind === 'campfire') {
      shadowEllipse(sx, sy, 15 * u, 5 * u, 0.34);
      ctx.fillStyle = '#4a3a28';
      ctx.save(); ctx.translate(sx, sy); ctx.rotate(0.3);
      ctx.fillRect(-11 * u, -3 * u, 22 * u, 6 * u);
      ctx.rotate(-0.6); ctx.fillRect(-11 * u, -3 * u, 22 * u, 6 * u);
      ctx.restore();
      const fl = 1 + Math.sin(t * 9 + o.x) * 0.12;
      const gd = ctx.createRadialGradient(sx, sy - 14 * u, 0, sx, sy - 14 * u, 26 * u);
      gd.addColorStop(0, 'rgba(255,210,130,.55)'); gd.addColorStop(1, 'rgba(224,120,60,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, sy - 14 * u, 26 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = '#f0a050';
      ctx.beginPath();
      ctx.moveTo(sx - 6 * u, sy - 6 * u);
      ctx.quadraticCurveTo(sx - 8 * u, sy - 24 * u * fl, sx, sy - 30 * u * fl);
      ctx.quadraticCurveTo(sx + 8 * u, sy - 22 * u * fl, sx + 6 * u, sy - 6 * u);
      ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#ffe2a0';
      ctx.beginPath();
      ctx.moveTo(sx - 3 * u, sy - 6 * u);
      ctx.quadraticCurveTo(sx - 3 * u, sy - 18 * u, sx, sy - 21 * u);
      ctx.quadraticCurveTo(sx + 3 * u, sy - 16 * u, sx + 3 * u, sy - 6 * u);
      ctx.closePath(); ctx.fill();
      if (Math.random() < 0.12) {
        g.particles.push({ x: o.x + (Math.random() - 0.5) * 0.6, z: o.z, y: 14, vx: 0, vz: 0, vy: 18 + Math.random() * 18,
          life: 0.8, max: 0.8, color: 'rgba(255,200,120,.8)', size: 1.2, type: 'ember', grav: -8 });
      }
    } else if (o.kind === 'shrine') {
      shadowEllipse(sx, sy, 20 * u, 7 * u, 0.35);
      const used = g.s.shrines[o.data.idx];
      ctx.strokeStyle = used ? 'rgba(226,194,122,.5)' : 'rgba(226,194,122,.18)';
      ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.ellipse(sx, sy, 24 * u, 9 * u, 0, 0, TAU); ctx.stroke();
      ctx.fillStyle = '#6f6555';
      ctx.beginPath();
      ctx.moveTo(sx - 8 * u, sy);
      ctx.lineTo(sx - 5 * u, sy - 46 * u);
      ctx.lineTo(sx, sy - 56 * u);
      ctx.lineTo(sx + 5 * u, sy - 46 * u);
      ctx.lineTo(sx + 8 * u, sy);
      ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#8a7c64';
      ctx.beginPath();
      ctx.moveTo(sx - 5 * u, sy - 46 * u); ctx.lineTo(sx, sy - 56 * u); ctx.lineTo(sx + 5 * u, sy - 46 * u);
      ctx.closePath(); ctx.fill();
      const glow = used ? 0.85 : 0.35 + Math.sin(t * 2 + o.x) * 0.12;
      ctx.fillStyle = 'rgba(226,194,122,' + glow + ')';
      ctx.beginPath(); ctx.arc(sx, sy - 34 * u, 3.4 * u, 0, TAU); ctx.fill();
      if (used) {
        const gd = ctx.createRadialGradient(sx, sy - 34 * u, 0, sx, sy - 34 * u, 28 * u);
        gd.addColorStop(0, 'rgba(226,194,122,.20)'); gd.addColorStop(1, 'rgba(226,194,122,0)');
        ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, sy - 34 * u, 28 * u, 0, TAU); ctx.fill();
      }
    } else if (o.kind === 'stele') {
      shadowEllipse(sx, sy, 12 * u, 4 * u, 0.32);
      ctx.fillStyle = '#6b6355';
      ctx.beginPath();
      ctx.moveTo(sx - 8 * u, sy); ctx.lineTo(sx - 7 * u, sy - 34 * u);
      ctx.quadraticCurveTo(sx, sy - 44 * u, sx + 7 * u, sy - 34 * u);
      ctx.lineTo(sx + 8 * u, sy); ctx.closePath(); ctx.fill();
      ctx.strokeStyle = 'rgba(226,194,122,.35)'; ctx.lineWidth = 1;
      for (let i = 0; i < 3; i++) {
        ctx.beginPath(); ctx.moveTo(sx - 4 * u, sy - 26 * u + i * 6 * u); ctx.lineTo(sx + 4 * u, sy - 26 * u + i * 6 * u); ctx.stroke();
      }
      if (o.used) {
        ctx.fillStyle = 'rgba(226,194,122,.25)'; ctx.font = '10px ' + FONT; ctx.textAlign = 'center';
        ctx.fillText('已读', sx, sy - 48 * u);
      }
    } else if (o.kind === 'crystal') {
      const s = (o.data.size || 1) * u;
      shadowEllipse(sx, sy, 10 * s, 3.5 * s, 0.28);
      const pulse = 0.6 + Math.sin(t * 2.4 + o.x) * 0.2;
      const gd = ctx.createRadialGradient(sx, sy - 12 * s, 0, sx, sy - 12 * s, 24 * s);
      gd.addColorStop(0, 'rgba(169,223,232,' + (0.30 * pulse) + ')');
      gd.addColorStop(1, 'rgba(169,223,232,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, sy - 12 * s, 24 * s, 0, TAU); ctx.fill();
      for (let i = 0; i < 3; i++) {
        const ox = (i - 1) * 6 * s, hh = (13 + i * 5) * s;
        ctx.fillStyle = i === 1 ? '#a9dfe8' : '#7fbfc8';
        ctx.beginPath();
        ctx.moveTo(sx + ox, sy);
        ctx.lineTo(sx + ox - 4 * s, sy - hh * 0.5);
        ctx.lineTo(sx + ox, sy - hh);
        ctx.lineTo(sx + ox + 4 * s, sy - hh * 0.5);
        ctx.closePath(); ctx.fill();
      }
    } else if (o.kind === 'chest') {
      shadowEllipse(sx, sy, 12 * u, 4 * u, 0.3);
      const rare = o.data.tier;
      ctx.fillStyle = rare ? '#7a5a32' : '#5b4a34';
      ctx.fillRect(sx - 11 * u, sy - 16 * u, 22 * u, 14 * u);
      ctx.fillStyle = rare ? '#c8a35c' : '#8a7450';
      ctx.beginPath(); ctx.moveTo(sx - 11 * u, sy - 16 * u); ctx.lineTo(sx - 9 * u, sy - 23 * u);
      ctx.lineTo(sx + 9 * u, sy - 23 * u); ctx.lineTo(sx + 11 * u, sy - 16 * u); ctx.closePath(); ctx.fill();
      ctx.fillStyle = rare ? '#ffd47a' : '#d9ceb6';
      ctx.fillRect(sx - 2 * u, sy - 17 * u, 4 * u, 5 * u);
      if (rare) {
        ctx.strokeStyle = 'rgba(255,212,122,.5)'; ctx.lineWidth = 1.2;
        ctx.beginPath(); ctx.ellipse(sx, sy, 17 * u, 6 * u, 0, 0, TAU); ctx.stroke();
      }
    } else if (o.kind === 'vent') {
      ctx.strokeStyle = 'rgba(143,216,200,.4)'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.ellipse(sx, sy, 13 * u, 5 * u, 0, 0, TAU); ctx.stroke();
      const gd = ctx.createRadialGradient(sx, sy - 6 * u, 0, sx, sy - 6 * u, 20 * u);
      gd.addColorStop(0, 'rgba(143,216,200,.28)'); gd.addColorStop(1, 'rgba(143,216,200,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, sy - 6 * u, 20 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = 'rgba(200,240,236,.55)';
      for (let i = 0; i < 3; i++) {
        const a = t * 1.2 + i * 2;
        ctx.beginPath(); ctx.arc(sx + Math.cos(a) * 8 * u, sy - 10 * u, 2 * u, 0, TAU); ctx.fill();
      }
    } else if (o.kind === 'ore') {
      shadowEllipse(sx, sy, 12 * u, 4 * u, 0.3);
      ctx.fillStyle = '#4d4a44';
      ctx.beginPath();
      ctx.moveTo(sx - 12 * u, sy); ctx.lineTo(sx - 8 * u, sy - 14 * u);
      ctx.lineTo(sx - 1 * u, sy - 20 * u); ctx.lineTo(sx + 9 * u, sy - 12 * u);
      ctx.lineTo(sx + 12 * u, sy); ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#6b675e';
      ctx.beginPath();
      ctx.moveTo(sx - 6 * u, sy - 8 * u); ctx.lineTo(sx - 2 * u, sy - 18 * u);
      ctx.lineTo(sx + 5 * u, sy - 11 * u); ctx.closePath(); ctx.fill();
      ctx.strokeStyle = o.used ? 'rgba(200,190,160,.25)' : 'rgba(200,190,160,.7)';
      ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.moveTo(sx - 7 * u, sy - 4 * u); ctx.lineTo(sx - 2 * u, sy - 15 * u); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(sx + 2 * u, sy - 5 * u); ctx.lineTo(sx + 7 * u, sy - 12 * u); ctx.stroke();
    } else if (o.kind === 'herb') {
      const sway = Math.sin(t * 1.6 + o.x) * 2 * u;
      ctx.strokeStyle = '#5f8a5a'; ctx.lineWidth = 1.6 * u;
      for (let i = 0; i < 5; i++) {
        ctx.beginPath();
        ctx.moveTo(sx + (i - 2) * 2.4 * u, sy);
        ctx.quadraticCurveTo(sx + (i - 2) * 3 * u + sway, sy - 8 * u, sx + (i - 2) * 3.4 * u + sway, sy - 13 * u);
        ctx.stroke();
      }
      ctx.fillStyle = 'rgba(143,191,136,.9)';
      for (let i = 0; i < 4; i++) {
        ctx.beginPath();
        ctx.ellipse(sx + (i - 1.5) * 3 * u + sway, sy - (6 + i % 2 * 4) * u, 2.6 * u, 1.2 * u, -0.6, 0, TAU);
        ctx.fill();
      }
      ctx.fillStyle = '#d9e8b8';
      ctx.beginPath(); ctx.arc(sx + sway, sy - 14 * u, 1.8 * u, 0, TAU); ctx.fill();
    } else if (o.kind === 'altar') {
      shadowEllipse(sx, sy, 24 * u, 8 * u, 0.4);
      ctx.strokeStyle = 'rgba(226,194,122,.45)'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.ellipse(sx, sy, 30 * u, 11 * u, 0, 0, TAU); ctx.stroke();
      ctx.fillStyle = '#5a5144';
      ctx.fillRect(sx - 14 * u, sy - 12 * u, 28 * u, 12 * u);
      ctx.fillStyle = '#8a7c64';
      ctx.beginPath();
      ctx.moveTo(sx - 4 * u, sy - 12 * u); ctx.lineTo(sx - 2 * u, sy - 74 * u);
      ctx.lineTo(sx + 2 * u, sy - 74 * u); ctx.lineTo(sx + 4 * u, sy - 12 * u);
      ctx.closePath(); ctx.fill();
      const pulse = 0.5 + Math.sin(t * 1.4) * 0.2;
      const gd = ctx.createRadialGradient(sx, sy - 74 * u, 0, sx, sy - 74 * u, 32 * u);
      gd.addColorStop(0, 'rgba(226,194,122,' + (0.35 + pulse * 0.25) + ')');
      gd.addColorStop(1, 'rgba(226,194,122,0)');
      ctx.fillStyle = gd; ctx.beginPath(); ctx.arc(sx, sy - 74 * u, 32 * u, 0, TAU); ctx.fill();
      ctx.fillStyle = '#e2c27a';
      ctx.fillRect(sx - 1.4 * u, sy - 73 * u, 2.8 * u, 30 * u);
    }
    // 名字
    if (o.kind !== 'crystal' && o.kind !== 'herb' && o.kind !== 'ore' && o.kind !== 'chest') {
      const d = len(o.x - g.p.x, o.z - g.p.z);
      if (d < 9 && o.kind !== 'campfire') {
        ctx.fillStyle = 'rgba(230,220,196,.55)';
        ctx.font = '11px ' + FONT; ctx.textAlign = 'center';
        ctx.fillText(o.name, sx, sy - (o.kind === 'altar' ? 92 : o.kind === 'shrine' ? 66 : 50) * u);
      }
    }
  }

  // ---------- 特效 / 天气 ----------
  function drawParticles() {
    for (const q of g.particles) {
      const [sx, sy] = proj(q.x, q.z);
      ctx.globalAlpha = clamp(q.life / q.max, 0, 1);
      ctx.fillStyle = q.color;
      if (q.type === 'dust') {
        ctx.beginPath(); ctx.arc(sx, sy - q.y * 0.4, q.size * 1.6, 0, TAU); ctx.fill();
      } else {
        ctx.fillRect(sx - q.size / 2, sy - q.y, q.size, q.size);
      }
      ctx.globalAlpha = 1;
    }
    // 伤害数字
    for (const d of g.dmgNums) {
      const [sx, sy] = proj(d.x, d.z);
      const a = clamp(d.life / d.max, 0, 1);
      ctx.globalAlpha = a;
      ctx.font = '600 ' + d.size + 'px ' + FONT;
      ctx.textAlign = 'center';
      ctx.fillStyle = 'rgba(0,0,0,.5)';
      ctx.fillText(d.text, sx + 1, sy - 44 - d.y);
      ctx.fillStyle = d.color;
      ctx.fillText(d.text, sx, sy - 45 - d.y);
      ctx.globalAlpha = 1;
    }
  }

  function drawLighting() {
    const night = nightAmount();
    if (night <= 0.02) return;
    const lc = g.atlas.light, lx = lc.getContext('2d');
    const s = 0.6;
    lx.globalCompositeOperation = 'source-over';
    lx.clearRect(0, 0, lc.width, lc.height);
    lx.fillStyle = 'rgba(8,14,28,' + (night * 0.62) + ')';
    lx.fillRect(0, 0, lc.width, lc.height);
    lx.globalCompositeOperation = 'destination-out';
    const lights = [];
    const [px, py] = proj(g.p.x, g.p.z);
    lights.push([px * s, (py - 22) * s, (g.p.lawT > 0 ? 260 : 180) * s, 1]);
    for (const o of g.pois) {
      if (o.kind === 'campfire' && len(o.x - g.p.x, o.z - g.p.z) < 80) {
        const [x, y] = proj(o.x, o.z);
        lights.push([x * s, (y - 12) * s, 150 * s, 0.9]);
      } else if (o.kind === 'shrine' && g.s.shrines[o.data.idx]) {
        const [x, y] = proj(o.x, o.z);
        lights.push([x * s, (y - 30) * s, 110 * s, 0.6]);
      }
    }
    for (const l of lights) {
      const gr = lx.createRadialGradient(l[0], l[1], 0, l[0], l[1], l[2]);
      gr.addColorStop(0, 'rgba(0,0,0,' + l[3] + ')');
      gr.addColorStop(0.55, 'rgba(0,0,0,' + l[3] * 0.55 + ')');
      gr.addColorStop(1, 'rgba(0,0,0,0)');
      lx.fillStyle = gr;
      lx.beginPath(); lx.arc(l[0], l[1], l[2], 0, TAU); lx.fill();
    }
    lx.globalCompositeOperation = 'source-over';
    ctx.drawImage(lc, 0, 0, g.W, g.H);
    // 暖色光晕
    ctx.save();
    ctx.globalCompositeOperation = 'lighter';
    const [cx, cy] = proj(g.p.x, g.p.z);
    const warm = ctx.createRadialGradient(cx, cy - 20, 0, cx, cy - 20, 140);
    warm.addColorStop(0, 'rgba(255,190,110,' + (night * 0.05) + ')');
    warm.addColorStop(1, 'rgba(255,190,110,0)');
    ctx.fillStyle = warm; ctx.beginPath(); ctx.arc(cx, cy - 20, 140, 0, TAU); ctx.fill();
    ctx.restore();
  }

  const rainDrops = [];
  for (let i = 0; i < 180; i++) rainDrops.push({ x: Math.random(), y: Math.random(), s: 0.5 + Math.random() });
  let dustMotes = [];
  for (let i = 0; i < 60; i++) dustMotes.push({ x: Math.random(), y: Math.random(), p: Math.random() * TAU });

  function drawWeather(dt) {
    const w = g.s.weather;
    const night = nightAmount();
    if (w === 'rain') {
      ctx.strokeStyle = 'rgba(170,200,210,.20)';
      ctx.lineWidth = 1;
      for (const d of rainDrops) {
        const x = d.x * g.W, y = ((d.y + performance.now() / 1000 * 0.55 * d.s) % 1) * g.H;
        ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x - 5, y + 18 * d.s); ctx.stroke();
      }
      ctx.fillStyle = 'rgba(40,60,70,.10)'; ctx.fillRect(0, 0, g.W, g.H);
    } else if (w === 'mist' || w === 'cloudy') {
      const a = w === 'mist' ? 0.13 : 0.06;
      ctx.fillStyle = 'rgba(190,200,190,' + a + ')';
      ctx.fillRect(0, 0, g.W, g.H);
    } else if (w === 'dust') {
      ctx.fillStyle = 'rgba(180,150,105,.10)'; ctx.fillRect(0, 0, g.W, g.H);
      ctx.fillStyle = 'rgba(210,180,130,.35)';
      for (const m of dustMotes) {
        const x = (m.x * g.W + performance.now() * 0.03) % g.W;
        const y = m.y * g.H + Math.sin(performance.now() * 0.001 + m.p) * 20;
        ctx.fillRect(x, y, 1.6, 1.6);
      }
    } else if (w === 'crystal') {
      ctx.fillStyle = 'rgba(90,130,130,.08)'; ctx.fillRect(0, 0, g.W, g.H);
      for (const m of dustMotes) {
        const x = (m.x * g.W + Math.sin(performance.now() * 0.0004 + m.p) * 60) % g.W;
        const y = (m.y * g.H + performance.now() * 0.012 * (0.5 + m.p % 1)) % g.H;
        ctx.fillStyle = 'rgba(169,223,232,.45)';
        ctx.fillRect(x, y, 2, 2);
      }
    }
    if (night > 0.3) {
      ctx.fillStyle = 'rgba(6,10,20,' + (night * 0.06) + ')';
      ctx.fillRect(0, 0, g.W, g.H);
    }
  }

  function drawCursor() {
    const x = g.mouse.x, y = g.mouse.y;
    ctx.save();
    ctx.strokeStyle = g.p.blocking ? 'rgba(226,194,122,.85)' : 'rgba(230,220,196,.55)';
    ctx.lineWidth = 1.2;
    ctx.beginPath(); ctx.arc(x, y, 7, 0, TAU); ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(x - 11, y); ctx.lineTo(x - 4, y);
    ctx.moveTo(x + 4, y); ctx.lineTo(x + 11, y);
    ctx.moveTo(x, y - 11); ctx.lineTo(x, y - 4);
    ctx.moveTo(x, y + 4); ctx.lineTo(x, y + 11);
    ctx.stroke();
    ctx.fillStyle = 'rgba(230,220,196,.8)';
    ctx.fillRect(x - 1, y - 1, 2, 2);
    ctx.restore();
  }

  function render(dt) {
    const cam = g.cam;
    // 相机抖动
    if (g.shake > 0.1) {
      cam.shakeX = (Math.random() - 0.5) * g.shake;
      cam.shakeY = (Math.random() - 0.5) * g.shake;
      g.shake *= 0.86;
    } else { cam.shakeX = 0; cam.shakeY = 0; g.shake = 0; }

    ctx.clearRect(0, 0, g.W, g.H);
    drawTerrain();
    drawGroundDecals();

    const list = [];
    const props = WORLD.visibleProps(cam, g.W, g.H);
    for (const p of props) list.push({ z: p.z, kind: 0, o: p });
    for (const o of g.pois) {
      if (o.kind === 'npc') continue;
      list.push({ z: o.z, kind: 1, o });
    }
    for (const n of g.npcs) list.push({ z: n.z, kind: 2, o: n });
    for (const e of g.enemies) list.push({ z: e.z, kind: 3, o: e });
    list.push({ z: g.p.z, kind: 4, o: g.p });
    list.sort((a, b) => a.z - b.z);

    for (const it of list) {
      if (it.kind === 0) WORLD.drawProp(ctx, it.o, cam, g.W, g.H, performance.now() / 1000, 1 + nightAmount() * 0.4);
      else if (it.kind === 1) drawPoi(it.o);
      else if (it.kind === 2) {
        const po = g.pois.find(q => q.npcKey === it.o.key);
        if (po) drawNPC(po);
      } else if (it.kind === 3) drawEnemy(it.o);
      else drawPlayer();
    }

    drawParticles();
    drawLighting();
    drawWeather(dt);
    drawCursor();
  }

  // ============================================================
  //  输 入
  // ============================================================
  window.addEventListener('keydown', e => {
    if (['Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].indexOf(e.code) >= 0) e.preventDefault();
    if (g.keys[e.code]) return;
    g.keys[e.code] = true;
    const code = e.code;

    if (UI.dialogueOn) {
      if (code === 'Escape') { closeDialogue(); return; }
      if (/^Digit[1-9]$/.test(code)) {
        const n = parseInt(code.slice(5), 10) - 1;
        chooseDialogue(n);
      }
      return;
    }
    if (UI.panelOn) {
      if (code === 'Escape') { closePanel(); return; }
      if (/^Digit[1-9]$/.test(code)) { UI.panelKey(parseInt(code.slice(5), 10)); return; }
      if (code === 'KeyI') { openPanel('gear'); return; }
      if (code === 'KeyM') { openPanel('map'); return; }
      if (code === 'KeyJ') { openPanel('journal'); return; }
      return;
    }
    if (!g.started || g.dead) return;
    if (code === 'KeyI') { openPanel('gear'); return; }
    if (code === 'KeyM') { openPanel('map'); return; }
    if (code === 'KeyJ') { openPanel('journal'); return; }
    if (code === 'KeyE') { interact(); return; }
    if (code === 'Space') { startRoll(); return; }
    if (code === 'KeyV') { startHeavy(); return; }
    if (code === 'KeyR') { tryLaw(); return; }
    if (code === 'KeyZ') { cycleWeapon(); return; }
    if (code === 'KeyQ') { usePotion(); return; }
    if (code === 'Tab') { e.preventDefault(); cycleTarget(); return; }
  });
  window.addEventListener('keyup', e => { g.keys[e.code] = false; });

  cv.addEventListener('mousemove', e => { g.mouse.x = e.clientX; g.mouse.y = e.clientY; });
  cv.addEventListener('mousedown', e => {
    if (e.button === 0) {
      g.mouse.down = true;
      if (g.started && !UI.panelOn && !UI.dialogueOn && !g.dead) startLightAttack();
    } else if (e.button === 2) {
      if (g.started && !UI.panelOn && !UI.dialogueOn && !g.dead) {
        g.blockKey = true; g.p.parryT = 0.22; SND.sfx('block', 0.4);
      }
    }
  });
  window.addEventListener('mouseup', e => {
    if (e.button === 0) g.mouse.down = false;
    if (e.button === 2) g.blockKey = false;
  });
  cv.addEventListener('contextmenu', e => e.preventDefault());
  cv.addEventListener('wheel', e => {
    if (!g.started) return;
    g.cam.zoom = clamp(g.cam.zoom * (e.deltaY > 0 ? 0.92 : 1.08), 16, 44);
    e.preventDefault();
  }, { passive: false });

  function tryLaw() {
    const p = g.p;
    if (p.law < 100) { SND.sfx('error'); toast('律令未满', '攻击、弹反与楔坛会积累律令。'); return; }
    if (p.lawT > 0) return;
    p.law = 0; p.lawT = 10;
    SND.sfx('level');
    g.shake = 6;
    toast('律 令', '十秒之内，伤害 ×1.5。世界对你的动作慢了一拍。', 'ach');
    burst(p.x, p.z, '#e2c27a', 30, 'level');
  }

  function cycleWeapon() {
    const s = g.s;
    for (let i = 1; i <= 3; i++) {
      const k = (s.weapon + i) % 3;
      if (s.owned[k]) {
        s.weapon = k; applyGear();
        SND.sfx('ui');
        toast('切换武器 · ' + GEAR.weapons[k].name, GEAR.weapons[k].elementName);
        return;
      }
    }
  }

  function usePotion() {
    if (g.s.potions <= 0) { SND.sfx('error'); toast('没有药剂', '在营火旁用苔草与晶尘调配。'); return; }
    if (g.p.hp >= derived().maxHp) return;
    g.s.potions--;
    g.p.hp = Math.min(derived().maxHp, g.p.hp + 45);
    SND.sfx('heal');
    floating(g.p.x, g.p.z, '+45', '#8fbf88', 17);
    burst(g.p.x, g.p.z, '#8fbf88', 14, 'heal');
  }

  function cycleTarget() {
    const list = g.enemies.filter(e => !e.dead && len(e.x - g.p.x, e.z - g.p.z) < 18);
    if (!list.length) { g.target = null; return; }
    const idx = list.indexOf(g.target);
    g.target = list[(idx + 1) % list.length];
  }

  // ============================================================
  //  更 新
  // ============================================================
  function updatePlayer(dt) {
    const p = g.p;
    if (p.dead) {
      g.deadT -= dt;
      if (g.deadT <= 0) respawn();
      return;
    }
    movePlayer(dt);
    updatePlayerCombat(dt);
    // 提示
    const o = nearestPoi();
    g.promptPoi = o;
    if (o) {
      g.prompt = o.prompt;
      UI.setPrompt(o.prompt);
    } else {
      g.prompt = null;
      UI.setPrompt(null);
    }
    // 律令值自然衰减（不发动时）
    if (p.lawT <= 0 && p.law > 0) p.law = Math.min(100, p.law);
    // 选目标
    if (!g.target || g.target.dead || len(g.target.x - p.x, g.target.z - p.z) > 16) {
      let best = null, bd = 12;
      for (const e of g.enemies) {
        if (e.dead) continue;
        const d = len(e.x - p.x, e.z - p.z);
        if (d < bd) { bd = d; best = e; }
      }
      g.target = best;
    }
    // 连击衰减
    if (g.comboT > 0) { g.comboT -= dt; if (g.comboT <= 0) g.comboCount = 0; }
    // 音效环境
    const nearCamp = g.pois.some(o2 => o2.kind === 'campfire' && len(o2.x - p.x, o2.z - p.z) < 14);
    if (nearCamp !== g.lastCamp) { SND.campfireNear(nearCamp); g.lastCamp = nearCamp; }
  }

  function updatePois(dt) {
    for (const o of g.pois) {
      if (o.used && o.respawnT > 0) {
        o.respawnT -= dt;
        if (o.respawnT <= 0) { o.used = false; o.respawnT = 0; }
      }
    }
  }

  function updateParticles(dt) {
    for (let i = g.particles.length - 1; i >= 0; i--) {
      const q = g.particles[i];
      q.life -= dt;
      if (q.life <= 0) { g.particles.splice(i, 1); continue; }
      q.x += q.vx * dt; q.z += q.vz * dt;
      q.vy -= (q.grav || 0) * dt;
      q.y += q.vy * dt;
      if (q.y < 0) { q.y = 0; q.vy *= -0.3; }
    }
    for (let i = g.dmgNums.length - 1; i >= 0; i--) {
      const d = g.dmgNums[i];
      d.life -= dt;
      d.y += d.vy * dt;
      d.vy *= 0.92;
      if (d.life <= 0) g.dmgNums.splice(i, 1);
    }
  }

  function updateCamera(dt) {
    const p = g.p;
    const tx = p.x + p.vx * 0.18;
    const tz = p.z + p.vz * 0.18;
    const k = clamp(dt * 4.2, 0, 1);
    g.cam.x = lerp(g.cam.x, tx, k);
    g.cam.z = lerp(g.cam.z, tz, k);
    g.cam.x = clamp(g.cam.x, -3900, 3900);
    g.cam.z = clamp(g.cam.z, -3900, 3900);
    if (g.slowT > 0) g.slowT -= dt;
  }

  function update(dt) {
    const s = g.s;
    s.playtime += dt;
    updateTime(dt);
    updatePlayer(dt);
    updatePlayerCombatLog(dt);
    if (!g.talking && !UI.panelOn) updateEnemies(dt);
    updatePois(dt);
    updateParticles(dt);
    updateWorldEvents(dt);
    updateCamera(dt);
    applyGear();
    // 天气音效
    const wk = s.weather + (nightAmount() > 0.5 ? '_n' : '');
    if (wk !== g.lastWeather) { g.lastWeather = wk; SND.setWeather(s.weather, nightAmount() > 0.5); }
    if (UI.panelOn && Math.floor(g.fpsAcc) === 0) UI.frame(g);
  }

  function updatePlayerCombatLog(dt) {
    // 律令值上限与成就
    if (g.s.nightMeters >= 500) unlockAch('night');
  }

  // ============================================================
  //  主循环
  // ============================================================
  let last = performance.now();
  function loop(now) {
    const raw = Math.min(0.05, Math.max(0.0001, (now - last) / 1000));
    last = now;
    g.fpsAcc += raw; g.fpsN++;
    if (g.fpsAcc >= 0.5) { g.fps = Math.round(g.fpsN / g.fpsAcc); g.fpsAcc = 0; g.fpsN = 0; }

    if (g.started && !g.dead) {
      const dt = g.slowT > 0 ? raw * 0.35 : raw;
      update(dt);
    } else if (g.started && g.dead) {
      g.deadT -= raw;
      if (g.deadT <= 0) respawn();
      updateParticles(raw);
      updateCamera(raw);
    } else {
      // 标题界面：让世界缓慢呼吸
      g.cam.x += Math.sin(now / 9000) * 0.03;
      updateParticles(raw);
    }
    render(raw);
    UI.frame(g);
    requestAnimationFrame(loop);
  }

  // ============================================================
  //  自 检（?selftest=1）
  // ============================================================
  function runSelfTest() {
    const box = document.createElement('pre');
    box.id = 'selftest';
    box.style.cssText = 'position:fixed;left:12px;top:12px;z-index:20000;background:#000;color:#7f7;padding:12px;font:12px monospace;white-space:pre;max-width:70vw';
    document.body.appendChild(box);
    const lines = [];
    const ok = (name, cond, extra) => {
      lines.push((cond ? 'PASS ' : 'FAIL ') + name + (extra ? ' :: ' + extra : ''));
      box.textContent = lines.join('\n');
    };
    const sleep = ms => new Promise(r => setTimeout(r, ms));
    (async () => {
      try {
        if (!g.started) g.startNew();
        await sleep(300);
        ok('start', g.started && !!g.p, 'pos=' + g.p.x.toFixed(0) + ',' + g.p.z.toFixed(0));

        const x0 = g.p.x, z0 = g.p.z;
        g.keys['KeyW'] = true;
        for (let i = 0; i < 60; i++) movePlayer(1 / 60);
        g.keys['KeyW'] = false;
        const movedD = len(g.p.x - x0, g.p.z - z0);
        ok('move', movedD > 0.8, 'moved=' + movedD.toFixed(2) + ' v=' + g.p.vx.toFixed(2) + ',' + g.p.vz.toFixed(2));

        const p = g.p;
        p.stamina = derived().maxStamina; p.attackCd = 0; p.swingT = -1; p.heavyT = -1;
        p.rollT = 0; p.blocking = false; p.dead = false;
        const ang = p.yaw;
        const ex = p.x + Math.sin(ang) * 2.2, ez = p.z + Math.cos(ang) * 2.2;
        const e = spawnEnemy('construct', ex, ez, 1, 0);
        e.hp = e.maxHp;
        p.swingYaw = ang;
        startLightAttack();
        p.swingYaw = ang;
        for (let i = 0; i < 40; i++) updatePlayerCombat(1 / 60);
        ok('attack', e.hp < e.maxHp, 'hp=' + e.hp + '/' + e.maxHp);

        e.hp = 1;
        damageEnemy(e, 50, {});
        await sleep(80);
        ok('kill', e.dead && g.s.kills > 0, 'kills=' + g.s.kills);

        openPanel('gear');
        await sleep(60);
        const panel = document.getElementById('panel');
        ok('panel', !panel.classList.contains('hidden'), 'visible=' + !panel.classList.contains('hidden'));
        closePanel();
        await sleep(40);
        ok('panel-close', panel.classList.contains('hidden'));

        startDialogue('zhonglou');
        await sleep(80);
        const dlg = document.getElementById('dialogue');
        const dtext = (document.querySelector('#dialogue .dlg-text') || {}).textContent || '';
        ok('dialogue', !dlg.classList.contains('hidden') && dtext.length > 4, 'text=' + dtext.slice(0, 18));
        closeDialogue();

        g.s.kills = 7;
        save();
        const savedLen = (localStorage.getItem('zhaoyi_web_save_v1') || '').length;
        g.s.kills = 999;
        const loaded = loadGame();
        ok('save/load', loaded && g.s.kills === 7, 'len=' + savedLen + ' kills=' + g.s.kills);

        const done = lines.filter(l => l.indexOf('FAIL') === 0).length === 0;
        document.title = (done ? 'SELFTEST-PASS' : 'SELFTEST-FAIL') + ' ' + lines.length;
        lines.push(done ? 'ALL PASS' : 'HAS FAIL');
        box.textContent = lines.join('\n');
      } catch (err) {
        lines.push('FAIL exception :: ' + (err && err.stack || err));
        box.textContent = lines.join('\n');
        document.title = 'SELFTEST-FAIL exception';
      }
    })();
  }

  // ============================================================
  //  启 动
  // ============================================================
  function boot() {
    resize();
    buildPOIs();
    makePlayer();
    applyGear();
    UI.init(g);
    g.hasSave = hasSave();
    UI.showTitle(g.hasSave);
    UI.loading(0.02, '正在铺开荒原……');
    // 预先给 POI 加排除区后生成出生点周围
    WORLD.warmup(DATA.spawn[0], DATA.spawn[1], p => {
      UI.loading(0.05 + p * 0.9, p > 0.7 ? '正在点亮楔坛……' : '正在生长青铜林与泪湖……');
      if (p >= 1) {
        UI.hideLoading();
        setTimeout(() => { if (!g.started) UI.showTitle(g.hasSave); }, 450);
      }
    });
    // 若预生成过快，兜底
    setTimeout(() => { UI.loading(0.6, '正在刻写碑文……'); }, 1200);
    setTimeout(() => { UI.hideLoading(); }, 4500);

    window.addEventListener('beforeunload', save);
    document.addEventListener('visibilitychange', () => { if (document.hidden) save(); });
    setInterval(() => { if (g.started && !UI.panelOn && !g.talking) save(); }, 25000);

    // 调试/演示：?play=1 直接进入游戏
    if (/[?&]play=1/.test(location.search)) {
      setTimeout(() => { if (!g.started) g.startNew(); }, 1400);
    }
    if (/[?&]selftest=1/.test(location.search)) {
      setTimeout(runSelfTest, 2200);
    }

    // 首帧渲染
    render(0.016);
    UI.frame(g);
    requestAnimationFrame(loop);
  }

  // 错误兜底：把异常显示在页面上，便于诊断
  window.addEventListener('error', ev => {
    console.error(ev.error || ev.message);
    window.__err = String(ev.message || ev.error);
    const d = document.createElement('div');
    d.style.cssText = 'position:fixed;left:12px;bottom:12px;z-index:9999;color:#ffb4a0;background:#1a0c08;border:1px solid #b45a35;padding:10px 14px;font:12px monospace;max-width:80vw;white-space:pre-wrap';
    d.textContent = '脚本错误：' + (ev.message || ev.error);
    document.body.appendChild(d);
  });

  if (/[?&]debug=1/.test(location.search)) {
    const dbg = document.createElement('div');
    dbg.style.cssText = 'position:fixed;left:8px;top:8px;z-index:10000;color:#9fe;background:rgba(0,0,0,.72);padding:8px;font:11px monospace;white-space:pre;max-width:70vw';
    document.body.appendChild(dbg);
    setInterval(() => {
      dbg.textContent = [
        'body=' + document.body.className,
        'started=' + g.started + ' dead=' + g.dead + ' panel=' + UI.panelOn + ' dlg=' + UI.dialogueOn,
        'p=' + (g.p ? g.p.x.toFixed(0) + ',' + g.p.z.toFixed(0) + ' hp' + g.p.hp.toFixed(0) : 'none') + ' region=' + g.regionName(),
        'cam=' + g.cam.x.toFixed(0) + ',' + g.cam.z.toFixed(0) + ' z' + g.cam.zoom.toFixed(0),
        'chunks=' + WORLD.chunkCount() + ' pois=' + g.pois.length + ' enemies=' + g.enemies.length,
        'hudOpacity=' + getComputedStyle(document.getElementById('hud')).opacity,
        'titleHidden=' + document.getElementById('title').className,
        'canvasRGB=' + (function () {
          try {
            const d = ctx.getImageData(Math.floor(g.W * 0.5 * DPR), Math.floor(g.H * 0.5 * DPR), 1, 1).data;
            return d[0] + ',' + d[1] + ',' + d[2] + ',' + d[3];
          } catch (e) { return 'n/a'; }
        })(),
        'chunkRGB=' + (function () {
          try { return WORLD.chunkPixel(g.cam.x, g.cam.z, 1).join(','); } catch (e) { return 'n/a'; }
        })(),
        'colorAt=' + (function () {
          try { return WORLD.colorAt(g.cam.x, g.cam.z, WORLD.heightAt(g.cam.x, g.cam.z), WORLD.regionAt(g.cam.x, g.cam.z), 1).map(Math.round).join(','); } catch (e) { return 'n/a'; }
        })(),
        'err=' + (window.__err || 'none')
      ].join('\n');
    }, 400);
  }

  if (/[?&]clean=1/.test(location.search)) {
    ['vignette', 'grain', 'hud', 'title', 'loading', 'fade', 'dialogue', 'panel'].forEach(id => {
      const el = document.getElementById(id);
      if (el) el.style.display = 'none';
    });
    g.s.time = 12 * 60;
    g.s.weather = 'clear';
  }

  boot();
})();
