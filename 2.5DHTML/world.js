/* 招亦废丽罗 · 程序化荒原 —— 地形、水文与植被 */
(function () {
  'use strict';

  // ---------- 确定性噪声 ----------
  const PERM = new Uint8Array(512);
  (function () {
    let seed = 20261121;
    const p = new Uint8Array(256);
    for (let i = 0; i < 256; i++) p[i] = i;
    for (let i = 255; i > 0; i--) {
      seed = (seed * 1664525 + 1013904223) >>> 0;
      const j = seed % (i + 1);
      const t = p[i]; p[i] = p[j]; p[j] = t;
    }
    for (let i = 0; i < 512; i++) PERM[i] = p[i & 255];
  })();

  function hash2(ix, iz) {
    return PERM[(PERM[ix & 255] + iz) & 255] / 255;
  }
  function noise2(x, z) {
    const ix = Math.floor(x), iz = Math.floor(z);
    const fx = x - ix, fz = z - iz;
    const ux = fx * fx * (3 - 2 * fx);
    const uz = fz * fz * (3 - 2 * fz);
    const a = hash2(ix, iz), b = hash2(ix + 1, iz);
    const c = hash2(ix, iz + 1), d = hash2(ix + 1, iz + 1);
    const l1 = a + (b - a) * ux;
    const l2 = c + (d - c) * ux;
    return (l1 + (l2 - l1) * uz) * 2 - 1;
  }
  function fbm(x, z, oct, lac, gain) {
    oct = oct || 3; lac = lac || 2; gain = gain || 0.5;
    let amp = 1, freq = 1, sum = 0, norm = 0;
    for (let k = 0; k < oct; k++) {
      sum += amp * noise2(x * freq, z * freq);
      norm += amp; amp *= gain; freq *= lac;
    }
    return sum / Math.max(1e-6, norm);
  }
  const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
  const clamp01 = v => v < 0 ? 0 : v > 1 ? 1 : v;
  function smoother(t) {
    t = clamp01(t);
    return t * t * t * (t * (t * 6 - 15) + 10);
  }
  function lakeEllipse(x, z) {
    const zz = (z + 540) / 1.35;
    return Math.sqrt(x * x + zz * zz);
  }
  function dirWin(aRad, centerDeg, halfDeg) {
    let d = (aRad * 57.2957795 - centerDeg) % 360;
    if (d < 0) d += 360;
    if (d > 180) d = 360 - d;
    return smoother((halfDeg - d) / 12);
  }

  // ---------- 高度场 ----------
  function heightAt(x, z) {
    const r = Math.sqrt(x * x + z * z);
    const edge = smoother((3900 - r) / 220);
    let h = 4 * edge - 16 * (1 - edge);

    const le = lakeEllipse(x, z);
    const lake = smoother((200 - le) / 130);
    h = h * (1 - lake) + (-14) * lake;
    h += 10 * Math.exp(-((le - 330) * (le - 330)) / (2 * 95 * 95)) * edge;

    const pk = Math.exp(-(r * r) / (2 * 265 * 265)) * (1 - lake);
    h += 126 * pk * edge;

    const flat = smoother((r - 62) / 55);
    h = 128.5 * (1 - flat) + h * flat;

    const er = Math.sqrt(x * x + (z - 640) * (z - 640));
    h += 62 * Math.exp(-((er - 235) * (er - 235)) / (2 * 105 * 105)) * edge;

    const fr = Math.sqrt((x + 620) * (x + 620) + z * z);
    h += 36 * Math.exp(-((fr - 175) * (fr - 175)) / (2 * 95 * 95)) * edge;
    h -= 16 * Math.exp(-(fr * fr) / (2 * 85 * 85)) * edge;
    h += 27 * Math.exp(-((x - 620) * (x - 620) + z * z) / (2 * 290 * 290)) * edge;

    h += 15 * fbm(x * 0.0042 + 37, z * 0.0042, 3) * edge;
    h += 17 * fbm(x * 0.008, z * 0.008, 4) * edge;

    const a = Math.atan2(z, x);
    const band = smoother((r - 900) / 260) * smoother((3700 - r) / 400);
    const des = band * dirWin(a, 0, 46);
    h += 10 * des + 6 * des * fbm(x * 0.005 + 31, z * 0.005, 3);
    const tun = band * dirWin(a, 270, 46);
    h += 9 * tun + 5 * tun * fbm(x * 0.004 + 77, z * 0.004, 3);
    const tr = Math.sqrt(x * x + (z + 1350) * (z + 1350));
    h += 20 * Math.exp(-(tr * tr) / (2 * 430 * 430)) * tun;
    const tr2 = Math.sqrt(x * x + (z + 2700) * (z + 2700));
    h += 26 * Math.exp(-(tr2 * tr2) / (2 * 380 * 380)) * tun;
    const ash = band * dirWin(a, 180, 41);
    h += 7 * ash + 4 * ash * fbm(x * 0.006 + 13, z * 0.006, 3);
    const ar = Math.sqrt((x + 1350) * (x + 1350) + z * z);
    h += 24 * Math.exp(-(ar * ar) / (2 * 340 * 340)) * ash;
    const ar2 = Math.sqrt((x + 2700) * (x + 2700) + z * z);
    h += 22 * Math.exp(-(ar2 * ar2) / (2 * 320 * 320)) * ash;
    const can = dirWin(a, 90, 40) * smoother((r - 850) / 200);
    h += 8 * can;
    h -= 30 * Math.exp(-((z - 1180) * (z - 1180)) / (2 * 55 * 55)) * can;
    h -= 26 * Math.exp(-((z - 2600) * (z - 2600)) / (2 * 50 * 50)) * can;
    return h;
  }

  function regionAt(x, z) {
    const r = Math.sqrt(x * x + z * z);
    if (r < 190) return 'peak';
    if (lakeEllipse(x, z) < 340 && z < -220) return 'lake';
    let a = Math.atan2(z, x) * 57.2957795;
    a = (a + 360) % 360;
    if (a > 52 && a < 135 && r < 900) return 'echo';
    if (a > 135 && a < 225 && r < 900) return 'forge';
    if (r > 330 && r < 560 && a > 225 && a < 315) return 'lake';
    if (r > 320 && r < 860 && (a <= 52 || a >= 315)) return 'forest';
    if (r > 900 && r < 3800) {
      if (a <= 46 || a >= 318) return 'desert';
      if (a > 220 && a < 316) return 'tundra';
      if (a > 136 && a < 218) return 'ash';
      if (a > 50 && a < 130) return 'canyon';
    }
    return 'plains';
  }

  const REGION_NAME = {
    peak: '楔之峰', lake: '泪湖', echo: '回声山', forge: '熔炉峡谷',
    forest: '青铜林', desert: '沉沙外域', tundra: '冻原', ash: '烬野',
    canyon: '裂谷', plains: '荒原'
  };
  const REGION_RGB = {
    peak: [104, 95, 82], lake: [42, 62, 66], echo: [112, 102, 84],
    forge: [96, 54, 36], forest: [58, 84, 52], desert: [158, 130, 78],
    tundra: [122, 136, 140], ash: [64, 61, 56], canyon: [82, 66, 74],
    plains: [88, 94, 56]
  };

  // ---------- 世界 / 区块 ----------
  const CHUNK = 128;          // 米
  const PPM = 4;              // 每米像素
  const TEX = CHUNK * PPM;    // 512
  const YS = 0.82;            // 俯视纵向压缩

  const chunks = new Map();
  const exclusions = [];
  const WORLD = window.WORLD = {
    CHUNK, PPM, TEX, YS, REGION_NAME, REGION_RGB,
    heightAt, regionAt, noise2, fbm, smoother, lakeEllipse, hash2,
    exclusions,
    project(x, z, cam, W, H) {
      return [ (x - cam.x) * cam.zoom + W / 2 + (cam.shakeX || 0),
               (z - cam.z) * cam.zoom * YS + H / 2 + (cam.shakeY || 0) ];
    },
    unproject(px, py, cam, W, H) {
      return [ cam.x + (px - W / 2) / cam.zoom,
               cam.z + (py - H / 2) / (cam.zoom * YS) ];
    },
    slopeAt(x, z) {
      const h = heightAt(x, z);
      const hx = heightAt(x + 1.5, z) - heightAt(x - 1.5, z);
      const hz = heightAt(x, z + 1.5) - heightAt(x, z - 1.5);
      return { h, dx: hx / 3, dz: hz / 3, s: Math.sqrt(hx * hx + hz * hz) / 3 };
    },
    isWater(x, z) { return heightAt(x, z) < 6.2 || lakeEllipse(x, z) < 208; },
    label(x, z) { return REGION_NAME[regionAt(x, z)] || '荒原'; },
    clearCache() { chunks.clear(); }
  };

  function mulberry32(a) {
    return function () {
      a |= 0; a = a + 0x6D2B79F5 | 0;
      let t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  }

  function excluded(x, z) {
    for (let i = 0; i < exclusions.length; i++) {
      const e = exclusions[i];
      const dx = x - e.x, dz = z - e.z;
      const r = e.r || 7;
      if (dx * dx + dz * dz < r * r) return true;
    }
    return false;
  }

  // ---------- 地形着色 ----------
  function shadeAt(h, region, x, z) {
    const hx = heightAt(x + 2.2, z) - heightAt(x - 2.2, z);
    const hz = heightAt(x, z + 2.2) - heightAt(x, z - 2.2);
    const nx = -hx / 4.4, nz = -hz / 4.4;
    const inv = 1 / Math.sqrt(nx * nx + nz * nz + 1);
    const ny = inv;
    const lx = -0.48, ly = 0.72, lz = -0.50;
    const dot = (-nx * lx + ny * ly - nz * lz) * inv;
    return clamp(0.58 + dot * 0.62, 0.28, 1.34);
  }

  function colorAt(x, z, h, region, shade) {
    const base = REGION_RGB[region] || REGION_RGB.plains;
    let r = base[0], g = base[1], b = base[2];
    const n1 = fbm(x * 0.013 + 11, z * 0.013, 2);
    const n2 = fbm(x * 0.085 + 3, z * 0.085, 2);
    const n3 = noise2(x * 0.34, z * 0.34);
    r *= 0.88 + n1 * 0.20; g *= 0.88 + n1 * 0.20; b *= 0.88 + n1 * 0.20;

    if (region === 'forest') {
      const moss = clamp01(n2 * 0.9 + 0.25);
      const bronze = clamp01((n1 - 0.42) * 2.2);
      const under = clamp01((n2 - 0.12) * 1.6);
      r = r * (0.86 - moss * 0.08) + 30 * moss + bronze * 38 - under * 8;
      g = g * (0.92 + moss * 0.18) + bronze * 14 - under * 5;
      b = b * (0.82 + moss * 0.05) + bronze * 4 - under * 3;
    } else if (region === 'desert') {
      const band = Math.sin(h * 0.55 + n2 * 2.2) * 0.5 + 0.5;
      r += 9 * band; g += 7 * band; b += 2 * band;
    } else if (region === 'canyon') {
      const band = Math.sin(h * 0.7 + n2 * 1.4) * 0.5 + 0.5;
      r *= 0.84 + band * 0.22; g *= 0.80 + band * 0.18; b *= 0.92 + band * 0.18;
    } else if (region === 'tundra') {
      const ice = clamp01((n2 + 0.5) * 0.8);
      r += 22 * ice; g += 26 * ice; b += 30 * ice;
    } else if (region === 'ash') {
      r *= 0.9 + n2 * 0.1; g *= 0.9 + n2 * 0.1; b *= 0.9 + n2 * 0.1;
    } else if (region === 'forge') {
      const heat = clamp01(n2 * -1.2 + 0.2);
      r += 34 * heat; g += 7 * heat; b += 2 * heat;
    } else if (region === 'peak') {
      if (h > 96) {
        const snow = clamp01((h - 96) / 20 + n2 * 0.25);
        r += (198 - r) * snow; g += (203 - g) * snow; b += (208 - b) * snow;
      }
      const rock = clamp01((shade - 0.8) * 2.5);
      r += rock * 12; g += rock * 10; b += rock * 8;
    } else if (region === 'lake') {
      if (h < 4.4) { r = 36 + n2 * 6; g = 55 + n2 * 8; b = 60 + n2 * 8; }
      else if (h < 9) { r += 16; g += 14; b += 4; }
    }

    // 雪线 / 岸线 / 岩层
    if (h > 88 && region !== 'peak' && region !== 'tundra') {
      const snow = clamp01((h - 88) / 22 + n2 * 0.2);
      r += (176 - r) * snow * 0.85; g += (184 - g) * snow * 0.85; b += (188 - b) * snow * 0.85;
    }
    if (h < 2.5 && region !== 'lake') {
      const shore = clamp01((2.5 - h) / 8);
      r += (96 - r) * shore * 0.45; g += (94 - g) * shore * 0.4; b += (72 - b) * shore * 0.35;
    }
    const rockAmt = clamp01((shade - 1.02) * 2.2);
    r = r * (1 - rockAmt * 0.45) + 88 * rockAmt * 0.45;
    g = g * (1 - rockAmt * 0.45) + 82 * rockAmt * 0.45;
    b = b * (1 - rockAmt * 0.45) + 70 * rockAmt * 0.45;

    const contour = Math.abs((h % 12) - 6) < 0.28 ? 0.93 : 1;
    const fine = 0.94 + n3 * 0.055;
    const s = shade * fine * contour;
    // 日间环境光抬升，避免俯视视角下地表过暗
    const lift = 1.30;
    return [ clamp(r * s * lift + 14, 0, 255), clamp(g * s * lift + 14, 0, 255), clamp(b * s * lift + 14, 0, 255) ];
  }

  // ---------- 区块纹理 ----------
  function buildChunk(cx, cz) {
    const canvas = document.createElement('canvas');
    canvas.width = TEX; canvas.height = TEX;
    const c = canvas.getContext('2d');
    const img = c.createImageData(TEX, TEX);
    const d = img.data;
    const ox = cx * CHUNK, oz = cz * CHUNK;
    const HG = 256; // 半分辨率高度网格（含 1 格外扩，避免区块接缝处法线突变）
    const hg = new Float32Array((HG + 3) * (HG + 3));
    for (let gy = -1; gy <= HG + 1; gy++) {
      for (let gx = -1; gx <= HG + 1; gx++) {
        hg[(gy + 1) * (HG + 3) + (gx + 1)] = heightAt(ox + gx / HG * CHUNK, oz + gy / HG * CHUNK);
      }
    }
    const at = (gx, gy) => hg[(clamp(gy, -1, HG + 1) + 1) * (HG + 3) + (clamp(gx, -1, HG + 1) + 1)];
    for (let py = 0; py < TEX; py++) {
      const v = py / PPM;
      const gy = Math.floor(v / CHUNK * HG);
      const fy = v / CHUNK * HG - gy;
      for (let px = 0; px < TEX; px++) {
        const u = px / PPM;
        const gx = Math.floor(u / CHUNK * HG);
        const fx = u / CHUNK * HG - gx;
        const h00 = at(gx, gy), h10 = at(gx + 1, gy), h01 = at(gx, gy + 1), h11 = at(gx + 1, gy + 1);
        const h = (h00 * (1 - fx) + h10 * fx) * (1 - fy) + (h01 * (1 - fx) + h11 * fx) * fy;
        const wx = ox + u, wz = oz + v;
        const region = regionAt(wx, wz);
        const nx = (at(gx + 1, gy) - at(gx - 1, gy)) / 2;
        const nz = (at(gx, gy + 1) - at(gx, gy - 1)) / 2;
        const ny = 1 / Math.sqrt(nx * nx + nz * nz + 1);
        const dot = (nx * 0.48 + ny * 0.72 + nz * 0.5) / Math.sqrt(nx * nx + nz * nz + 1);
        const shade = clamp(0.88 + dot * 0.26, 0.66, 1.12);
        const rgb = colorAt(wx, wz, h, region, shade);
        const i = (py * TEX + px) * 4;
        d[i] = rgb[0]; d[i + 1] = rgb[1]; d[i + 2] = rgb[2]; d[i + 3] = 255;
      }
    }
    c.putImageData(img, 0, 0);

    // 区块内的细节笔触
    const rnd = mulberry32((cx * 73856093) ^ (cz * 19349663));
    c.globalCompositeOperation = 'overlay';
    for (let i = 0; i < 900; i++) {
      const x = rnd() * TEX, y = rnd() * TEX;
      const wx = ox + x / PPM, wz = oz + y / PPM;
      const h = heightAt(wx, wz);
      const reg = regionAt(wx, wz);
      if (WORLD.isWater(wx, wz)) continue;
      const alpha = 0.02 + rnd() * 0.05;
      c.fillStyle = reg === 'desert' ? `rgba(255,220,160,${alpha})` :
                    reg === 'forest' ? `rgba(120,180,110,${alpha})` :
                    h > 80 ? `rgba(220,225,230,${alpha})` : `rgba(210,190,150,${alpha})`;
      c.beginPath();
      c.ellipse(x, y, 1 + rnd() * 5, 0.6 + rnd() * 1.8, rnd() * 3.14, 0, 6.283);
      c.fill();
    }
    c.globalCompositeOperation = 'source-over';

    const chunk = { cx, cz, canvas, props: buildProps(cx, cz), ready: true };
    return chunk;
  }

  // ---------- 植被与碎屑 ----------
  function buildProps(cx, cz) {
    const rnd = mulberry32((cx * 83492791) ^ (cz * 29863241) ^ 0x5f3759df);
    const props = [];
    const ox = cx * CHUNK, oz = cz * CHUNK;
    const tries = 430;
    for (let i = 0; i < tries; i++) {
      const x = ox + rnd() * CHUNK;
      const z = oz + rnd() * CHUNK;
      const h = heightAt(x, z);
      if (h < 4.2 || lakeEllipse(x, z) < 210) continue;
      if (excluded(x, z)) continue;
      const region = regionAt(x, z);
      const roll = rnd();
      let type = null, size = 1;
      if (region === 'forest') {
        if (roll < 0.40) { type = rnd() < 0.55 ? 'pine' : 'tree'; size = 0.9 + rnd() * 0.7; }
        else if (roll < 0.66) { type = 'grass'; size = 0.7 + rnd() * 0.6; }
        else if (roll < 0.75) { type = 'rock'; size = 0.5 + rnd() * 0.7; }
        else if (roll < 0.82) { type = 'mushroom'; size = 0.5 + rnd() * 0.5; }
        else if (roll < 0.89) { type = 'fern'; size = 0.7 + rnd() * 0.5; }
      } else if (region === 'plains') {
        if (roll < 0.09) { type = 'tree'; size = 0.8 + rnd() * 0.7; }
        else if (roll < 0.58) { type = 'grass'; size = 0.7 + rnd() * 0.8; }
        else if (roll < 0.69) { type = 'rock'; size = 0.4 + rnd() * 0.6; }
        else if (roll < 0.75) { type = 'flower'; size = 0.5 + rnd() * 0.4; }
      } else if (region === 'echo') {
        if (roll < 0.13) { type = 'pillar'; size = 1 + rnd() * 1.2; }
        else if (roll < 0.30) { type = 'rock'; size = 0.7 + rnd() * 1.2; }
        else if (roll < 0.48) { type = 'grass'; size = 0.6 + rnd() * 0.4; }
      } else if (region === 'forge') {
        if (roll < 0.26) { type = 'rock'; size = 0.8 + rnd() * 1.2; }
        else if (roll < 0.38) { type = 'ember'; size = 0.6 + rnd() * 0.8; }
        else if (roll < 0.44) { type = 'deadtree'; size = 0.9 + rnd() * 0.6; }
      } else if (region === 'desert') {
        if (roll < 0.16) { type = 'bone'; size = 0.7 + rnd() * 0.8; }
        else if (roll < 0.36) { type = 'rock'; size = 0.5 + rnd() * 0.9; }
        else if (roll < 0.43) { type = 'cactus'; size = 0.8 + rnd() * 0.5; }
      } else if (region === 'tundra') {
        if (roll < 0.20) { type = 'deadtree'; size = 0.8 + rnd() * 0.7; }
        else if (roll < 0.36) { type = 'rock'; size = 0.5 + rnd() * 0.8; }
        else if (roll < 0.43) { type = 'snowrock'; size = 0.7 + rnd() * 0.7; }
      } else if (region === 'ash') {
        if (roll < 0.22) { type = 'deadtree'; size = 0.8 + rnd() * 0.9; }
        else if (roll < 0.37) { type = 'rock'; size = 0.5 + rnd() * 0.7; }
        else if (roll < 0.44) { type = 'ember'; size = 0.5 + rnd() * 0.6; }
      } else if (region === 'canyon') {
        if (roll < 0.20) { type = 'spire'; size = 1 + rnd() * 1.4; }
        else if (roll < 0.37) { type = 'rock'; size = 0.6 + rnd() * 1; }
        else if (roll < 0.44) { type = 'deadtree'; size = 0.7 + rnd() * 0.6; }
      } else if (region === 'peak') {
        if (roll < 0.18) { type = 'rock'; size = 0.8 + rnd() * 1.4; }
        else if (roll < 0.25) { type = 'snowrock'; size = 0.8 + rnd() * 1.1; }
      } else if (region === 'lake') {
        if (h < 9 && roll < 0.5) { type = 'reed'; size = 0.7 + rnd() * 0.6; }
      }
      if (!type) continue;
      props.push({ type, x, z, h, size, seed: rnd() * 100, flip: rnd() < 0.5 });
    }
    props.sort((a, b) => a.z - b.z);
    return props;
  }

  let generated = 0;
  function ensureChunk(cx, cz) {
    const key = cx + ',' + cz;
    let ch = chunks.get(key);
    if (ch) return ch;
    ch = buildChunk(cx, cz);
    chunks.set(key, ch);
    generated++;
    return ch;
  }

  WORLD.ensureChunk = ensureChunk;
  WORLD.chunkCount = () => chunks.size;
  WORLD.generatedCount = () => generated;
  WORLD.colorAt = colorAt;
  WORLD.chunkPixel = function (x, z, n) {
    const cx = Math.floor(x / CHUNK), cz = Math.floor(z / CHUNK);
    const ch = ensureChunk(cx, cz);
    const px = Math.floor((x - cx * CHUNK) * PPM);
    const py = Math.floor((z - cz * CHUNK) * PPM);
    const size = n || 1;
    const d = ch.canvas.getContext('2d').getImageData(px, py, size, size).data;
    return [d[0], d[1], d[2], d[3]];
  };

  WORLD.warmup = function (x, z, onDone) {
    const cx = Math.floor(x / CHUNK), cz = Math.floor(z / CHUNK);
    const list = [];
    for (let dz = -1; dz <= 1; dz++) for (let dx = -1; dx <= 1; dx++) list.push([cx + dx, cz + dz]);
    let i = 0;
    function step() {
      const t0 = performance.now();
      while (i < list.length && performance.now() - t0 < 14) {
        ensureChunk(list[i][0], list[i][1]);
        i++;
      }
      if (onDone) onDone(i / list.length);
      if (i < list.length) requestAnimationFrame(step);
      else if (onDone) onDone(1);
    }
    requestAnimationFrame(step);
  };

  WORLD.update = function (cam, W, H) {
    const z = cam.zoom;
    const halfW = W / (2 * z) + CHUNK;
    const halfH = H / (2 * z * YS) + CHUNK;
    const x0 = Math.floor((cam.x - halfW) / CHUNK), x1 = Math.floor((cam.x + halfW) / CHUNK);
    const z0 = Math.floor((cam.z - halfH) / CHUNK), z1 = Math.floor((cam.z + halfH) / CHUNK);
    // 每帧最多生成一块，避免卡顿
    let budget = 1;
    for (let cz = z0; cz <= z1; cz++) {
      for (let cx = x0; cx <= x1; cx++) {
        const key = cx + ',' + cz;
        if (!chunks.has(key)) {
          if (budget > 0) { ensureChunk(cx, cz); budget--; }
          return;
        }
      }
    }
  };

  WORLD.drawTerrain = function (ctx, cam, W, H) {
    const z = cam.zoom;
    const sx = cam.shakeX || 0, sy = cam.shakeY || 0;
    const x0 = Math.floor((cam.x - W / (2 * z) - CHUNK) / CHUNK);
    const x1 = Math.floor((cam.x + W / (2 * z) + CHUNK) / CHUNK);
    const z0 = Math.floor((cam.z - H / (2 * z * YS) - CHUNK) / CHUNK);
    const z1 = Math.floor((cam.z + H / (2 * z * YS) + CHUNK) / CHUNK);
    ctx.imageSmoothingEnabled = true;
    for (let cz = z0; cz <= z1; cz++) {
      for (let cx = x0; cx <= x1; cx++) {
        const ch = chunks.get(cx + ',' + cz);
        if (!ch) continue;
        const px = (cx * CHUNK - cam.x) * z + W / 2 + sx;
        const py = (cz * CHUNK - cam.z) * z * YS + H / 2 + sy;
        ctx.drawImage(ch.canvas, px, py, CHUNK * z, CHUNK * z * YS);
      }
    }
  };

  WORLD.visibleProps = function (cam, W, H) {
    const out = [];
    const z = cam.zoom;
    const pad = 260;
    for (const ch of chunks.values()) {
      const bx = ch.cx * CHUNK, bz = ch.cz * CHUNK;
      const [sx, sy] = WORLD.project(bx, bz, cam, W, H);
      if (sx > W + pad || sy > H + pad) continue;
      if (sx + CHUNK * z < -pad || sy + CHUNK * z * YS < -pad) continue;
      for (let i = 0; i < ch.props.length; i++) out.push(ch.props[i]);
    }
    return out;
  };

  // ---------- 水面 ----------
  WORLD.drawWater = function (ctx, cam, W, H, time) {
    const [cx, cy] = WORLD.project(0, -540, cam, W, H);
    const rx = 205 * cam.zoom, ry = 276 * cam.zoom * YS;
    if (cx + rx < 0 || cx - rx > W || cy + ry < 0 || cy - ry > H) return;
    ctx.save();
    ctx.beginPath();
    ctx.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
    ctx.clip();
    const g = ctx.createLinearGradient(cx, cy - ry, cx, cy + ry);
    g.addColorStop(0, 'rgba(26,48,54,.78)');
    g.addColorStop(0.5, 'rgba(18,38,44,.84)');
    g.addColorStop(1, 'rgba(30,52,50,.72)');
    ctx.fillStyle = g;
    ctx.fillRect(cx - rx, cy - ry, rx * 2, ry * 2);
    ctx.strokeStyle = 'rgba(150,190,185,.10)';
    ctx.lineWidth = 1;
    for (let i = 0; i < 22; i++) {
      const yy = cy - ry + (i / 22) * ry * 2 + Math.sin(time * 0.6 + i) * 3;
      ctx.beginPath();
      for (let x = -rx; x <= rx; x += 14) {
        const yy2 = yy + Math.sin(x * 0.05 + time * 1.4 + i) * 2.2;
        if (x === -rx) ctx.moveTo(cx + x, yy2); else ctx.lineTo(cx + x, yy2);
      }
      ctx.stroke();
    }
    // 岸边泡沫
    ctx.strokeStyle = 'rgba(210,220,205,.15)';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.ellipse(cx, cy, rx - 2, ry - 2, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
  };

  // ---------- 绘制道具 ----------
  function shadow(ctx, sx, sy, w, h, a) {
    ctx.fillStyle = 'rgba(0,0,0,' + (a || 0.28) + ')';
    ctx.beginPath();
    ctx.ellipse(sx, sy, w, h, 0, 0, Math.PI * 2);
    ctx.fill();
  }

  function sway(seed, time, amp) {
    return Math.sin(time * 0.85 + seed) * amp;
  }

  WORLD.drawProp = function (ctx, p, cam, W, H, time, wind) {
    const [sx, sy] = WORLD.project(p.x, p.z, cam, W, H);
    const z = cam.zoom;
    if (sx < -180 || sx > W + 180 || sy < -220 || sy > H + 220) return;
    const s = z * p.size;
    const sw = sway(p.seed, time, (wind || 1) * 1.8) * (p.type === 'grass' || p.type === 'reed' || p.type === 'fern' ? 1.5 : 0.5);
    switch (p.type) {
      case 'tree': {
        shadow(ctx, sx, sy + 2, s * 0.7, s * 0.24, 0.30);
        const h = s * 1.35;
        ctx.strokeStyle = '#4a3826'; ctx.lineWidth = Math.max(1.4, s * 0.10);
        ctx.beginPath(); ctx.moveTo(sx, sy); ctx.quadraticCurveTo(sx + sw * 0.3, sy - h * 0.55, sx + sw * 0.6, sy - h); ctx.stroke();
        const cx0 = sx + sw * 0.6, cy0 = sy - h;
        const r = s * 0.78;
        ctx.fillStyle = '#27402b';
        ctx.beginPath(); ctx.ellipse(cx0 - r * 0.45, cy0 + r * 0.18, r * 0.62, r * 0.5, 0, 0, 6.283); ctx.fill();
        ctx.beginPath(); ctx.ellipse(cx0 + r * 0.4, cy0 + r * 0.22, r * 0.58, r * 0.46, 0, 0, 6.283); ctx.fill();
        ctx.fillStyle = '#37543a';
        ctx.beginPath(); ctx.ellipse(cx0, cy0 - r * 0.12, r * 0.66, r * 0.54, 0, 0, 6.283); ctx.fill();
        ctx.fillStyle = 'rgba(150,150,90,.16)';
        ctx.beginPath(); ctx.ellipse(cx0 - r * 0.2, cy0 - r * 0.3, r * 0.32, r * 0.2, -0.4, 0, 6.283); ctx.fill();
        break;
      }
      case 'pine': {
        shadow(ctx, sx, sy + 2, s * 0.55, s * 0.2, 0.28);
        const h = s * 1.75;
        ctx.strokeStyle = '#3d3022'; ctx.lineWidth = Math.max(1.2, s * 0.07);
        ctx.beginPath(); ctx.moveTo(sx, sy); ctx.lineTo(sx + sw * 0.4, sy - h); ctx.stroke();
        for (let k = 0; k < 4; k++) {
          const yy = sy - h * (0.28 + k * 0.19);
          const ww = s * (0.62 - k * 0.11);
          ctx.fillStyle = k % 2 ? '#2b4533' : '#23392b';
          ctx.beginPath();
          ctx.moveTo(sx + sw * 0.4 * (k / 4), yy - h * 0.2);
          ctx.lineTo(sx - ww, yy);
          ctx.lineTo(sx + ww, yy);
          ctx.closePath(); ctx.fill();
        }
        ctx.fillStyle = 'rgba(160,180,120,.12)';
        ctx.beginPath(); ctx.moveTo(sx + sw * 0.4, sy - h); ctx.lineTo(sx - s * 0.16, sy - h * 0.86); ctx.lineTo(sx + s * 0.04, sy - h * 0.82); ctx.fill();
        break;
      }
      case 'deadtree': {
        shadow(ctx, sx, sy + 2, s * 0.5, s * 0.18, 0.24);
        const h = s * 1.5;
        ctx.strokeStyle = '#4a4238'; ctx.lineWidth = Math.max(1.2, s * 0.07);
        ctx.beginPath(); ctx.moveTo(sx, sy); ctx.lineTo(sx + sw, sy - h); ctx.stroke();
        ctx.lineWidth = Math.max(1, s * 0.04);
        for (let k = 0; k < 4; k++) {
          const yy = sy - h * (0.45 + k * 0.13);
          const dir = k % 2 ? 1 : -1;
          ctx.beginPath(); ctx.moveTo(sx + sw * (0.5 + k * 0.1), yy);
          ctx.lineTo(sx + sw + dir * s * (0.4 + k * 0.06), yy - s * (0.2 + k * 0.03)); ctx.stroke();
        }
        break;
      }
      case 'grass': {
        shadow(ctx, sx, sy, s * 0.24, s * 0.07, 0.12);
        for (let k = -2; k <= 2; k++) {
          const hh = s * (0.55 + Math.abs(k) * 0.05 + p.seed % 0.2);
          ctx.strokeStyle = k % 2 ? 'rgba(112,124,70,.75)' : 'rgba(88,104,58,.8)';
          ctx.lineWidth = Math.max(0.7, s * 0.035);
          ctx.beginPath();
          ctx.moveTo(sx + k * s * 0.08, sy);
          ctx.quadraticCurveTo(sx + k * s * 0.1 + sw * 0.5, sy - hh * 0.6, sx + k * s * 0.15 + sw, sy - hh);
          ctx.stroke();
        }
        break;
      }
      case 'fern': {
        for (let k = 0; k < 5; k++) {
          const a = -1.9 + k * 0.42;
          ctx.strokeStyle = 'rgba(74,104,64,.78)'; ctx.lineWidth = Math.max(0.8, s * 0.04);
          ctx.beginPath(); ctx.moveTo(sx, sy);
          ctx.quadraticCurveTo(sx + Math.cos(a) * s * 0.4 + sw, sy - s * 0.3, sx + Math.cos(a) * s * 0.72 + sw, sy - s * 0.52);
          ctx.stroke();
        }
        break;
      }
      case 'rock': case 'snowrock': {
        shadow(ctx, sx, sy + 1, s * 0.5, s * 0.17, 0.26);
        const w = s * 0.55, h = s * 0.38;
        ctx.fillStyle = p.type === 'snowrock' ? '#9aa2a0' : '#5d5a50';
        ctx.beginPath();
        ctx.moveTo(sx - w, sy); ctx.lineTo(sx - w * 0.75, sy - h * 0.85);
        ctx.lineTo(sx - w * 0.15, sy - h * 1.05); ctx.lineTo(sx + w * 0.5, sy - h * 0.7);
        ctx.lineTo(sx + w, sy - h * 0.1); ctx.closePath(); ctx.fill();
        ctx.fillStyle = p.type === 'snowrock' ? 'rgba(228,234,232,.9)' : 'rgba(150,142,120,.28)';
        ctx.beginPath();
        ctx.moveTo(sx - w * 0.75, sy - h * 0.85); ctx.lineTo(sx - w * 0.15, sy - h * 1.05);
        ctx.lineTo(sx + w * 0.5, sy - h * 0.7); ctx.lineTo(sx + w * 0.1, sy - h * 0.55); ctx.closePath(); ctx.fill();
        break;
      }
      case 'spire': {
        shadow(ctx, sx, sy + 2, s * 0.45, s * 0.16, 0.3);
        const h = s * 1.9;
        ctx.fillStyle = '#544550';
        ctx.beginPath(); ctx.moveTo(sx - s * 0.35, sy); ctx.lineTo(sx - s * 0.12, sy - h);
        ctx.lineTo(sx + s * 0.16, sy - h * 0.94); ctx.lineTo(sx + s * 0.4, sy); ctx.closePath(); ctx.fill();
        ctx.fillStyle = 'rgba(180,150,170,.18)';
        ctx.beginPath(); ctx.moveTo(sx - s * 0.12, sy - h); ctx.lineTo(sx + s * 0.16, sy - h * 0.94);
        ctx.lineTo(sx + s * 0.06, sy - h * 0.3); ctx.closePath(); ctx.fill();
        break;
      }
      case 'pillar': {
        shadow(ctx, sx, sy + 2, s * 0.42, s * 0.15, 0.28);
        const h = s * 1.25;
        ctx.fillStyle = '#7a7160';
        ctx.fillRect(sx - s * 0.18, sy - h, s * 0.36, h);
        ctx.fillStyle = 'rgba(210,196,160,.25)';
        ctx.fillRect(sx - s * 0.18, sy - h, s * 0.1, h);
        ctx.fillStyle = '#6a6254';
        ctx.fillRect(sx - s * 0.28, sy - h - s * 0.08, s * 0.56, s * 0.1);
        break;
      }
      case 'mushroom': {
        shadow(ctx, sx, sy, s * 0.16, s * 0.05, 0.12);
        ctx.strokeStyle = '#b9b09a'; ctx.lineWidth = Math.max(0.8, s * 0.035);
        ctx.beginPath(); ctx.moveTo(sx, sy); ctx.lineTo(sx, sy - s * 0.18); ctx.stroke();
        ctx.fillStyle = 'rgba(180,160,120,.8)';
        ctx.beginPath(); ctx.ellipse(sx, sy - s * 0.2, s * 0.11, s * 0.055, 0, 0, 6.283); ctx.fill();
        break;
      }
      case 'flower': {
        ctx.strokeStyle = 'rgba(96,112,64,.8)'; ctx.lineWidth = 0.8;
        ctx.beginPath(); ctx.moveTo(sx, sy); ctx.lineTo(sx + sw * 0.6, sy - s * 0.28); ctx.stroke();
        ctx.fillStyle = p.seed % 1 > 0.5 ? 'rgba(196,160,110,.85)' : 'rgba(150,170,150,.8)';
        ctx.beginPath(); ctx.arc(sx + sw * 0.6, sy - s * 0.3, Math.max(1, s * 0.05), 0, 6.283); ctx.fill();
        break;
      }
      case 'reed': {
        for (let k = -2; k <= 2; k++) {
          ctx.strokeStyle = 'rgba(120,130,80,.7)'; ctx.lineWidth = Math.max(0.7, s * 0.025);
          ctx.beginPath(); ctx.moveTo(sx + k * s * 0.05, sy);
          ctx.quadraticCurveTo(sx + k * s * 0.06 + sw, sy - s * 0.5, sx + k * s * 0.1 + sw * 1.6, sy - s * 0.9);
          ctx.stroke();
        }
        break;
      }
      case 'cactus': {
        shadow(ctx, sx, sy + 1, s * 0.25, s * 0.08, 0.22);
        ctx.fillStyle = '#4d6342';
        ctx.fillRect(sx - s * 0.09, sy - s * 0.8, s * 0.18, s * 0.8);
        ctx.fillRect(sx - s * 0.32, sy - s * 0.55, s * 0.18, s * 0.1);
        ctx.fillRect(sx - s * 0.32, sy - s * 0.55, s * 0.08, s * 0.3);
        break;
      }
      case 'bone': {
        shadow(ctx, sx, sy, s * 0.3, s * 0.08, 0.18);
        ctx.strokeStyle = 'rgba(196,186,158,.7)'; ctx.lineWidth = Math.max(1, s * 0.05);
        ctx.beginPath(); ctx.arc(sx, sy - s * 0.06, s * 0.24, 3.3, 6.0); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(sx - s * 0.24, sy - s * 0.05); ctx.lineTo(sx + s * 0.24, sy - s * 0.12); ctx.stroke();
        break;
      }
      case 'ember': {
        const glow = 0.4 + 0.3 * Math.sin(time * 2 + p.seed);
        ctx.strokeStyle = 'rgba(224,104,54,' + (0.35 + glow * 0.3) + ')';
        ctx.lineWidth = Math.max(1, s * 0.05);
        ctx.beginPath();
        ctx.moveTo(sx - s * 0.25, sy); ctx.lineTo(sx - s * 0.05, sy - s * 0.12);
        ctx.lineTo(sx + s * 0.08, sy - s * 0.03); ctx.lineTo(sx + s * 0.26, sy - s * 0.1);
        ctx.stroke();
        break;
      }
    }
  };

  // ---------- 小地图配色 ----------
  WORLD.miniColor = function (x, z) {
    if (lakeEllipse(x, z) < 208) return '#1d3a40';
    const reg = regionAt(x, z);
    const c = REGION_RGB[reg] || REGION_RGB.plains;
    const h = heightAt(x, z);
    const s = 0.75 + clamp(h / 130, 0, 1) * 0.45;
    return 'rgb(' + Math.round(c[0] * s) + ',' + Math.round(c[1] * s) + ',' + Math.round(c[2] * s) + ')';
  };

})();
