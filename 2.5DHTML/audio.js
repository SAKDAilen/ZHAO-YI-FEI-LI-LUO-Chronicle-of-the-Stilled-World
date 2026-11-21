/* 招亦废丽罗 · 程序化音景（WebAudio，无外部资源） */
(function () {
  'use strict';
  const SND = window.SND = {
    ctx: null, master: null, muted: false, started: false,
    windGain: null, rainGain: null, droneGain: null, crackleT: 0
  };

  function now() { return SND.ctx ? SND.ctx.currentTime : 0; }

  SND.init = function () {
    if (SND.ctx) return;
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return;
    const ctx = SND.ctx = new AC();
    const master = SND.master = ctx.createGain();
    master.gain.value = 0.55;
    master.connect(ctx.destination);

    // 噪声缓冲
    const len = ctx.sampleRate * 2;
    const buf = ctx.createBuffer(1, len, ctx.sampleRate);
    const d = buf.getChannelData(0);
    let last = 0;
    for (let i = 0; i < len; i++) {
      const w = Math.random() * 2 - 1;
      last = (last + 0.02 * w) / 1.02;
      d[i] = last * 3.5 + w * 0.3;
    }
    SND.noise = buf;

    // 风
    const wind = ctx.createBufferSource();
    wind.buffer = buf; wind.loop = true;
    const wf = ctx.createBiquadFilter(); wf.type = 'bandpass'; wf.frequency.value = 420; wf.Q.value = 0.7;
    const wg = SND.windGain = ctx.createGain(); wg.gain.value = 0.035;
    wind.connect(wf); wf.connect(wg); wg.connect(master); wind.start();

    // 雨
    const rain = ctx.createBufferSource();
    rain.buffer = buf; rain.loop = true;
    const rf = ctx.createBiquadFilter(); rf.type = 'highpass'; rf.frequency.value = 2400;
    const rg = SND.rainGain = ctx.createGain(); rg.gain.value = 0;
    rain.connect(rf); rf.connect(rg); rg.connect(master); rain.start();

    // 低音持续音
    const drone = ctx.createGain(); drone.gain.value = 0.018;
    const lp = ctx.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 240;
    [55, 82.5, 110.3].forEach((f, i) => {
      const o = ctx.createOscillator(); o.type = i === 2 ? 'triangle' : 'sine';
      o.frequency.value = f;
      const g = ctx.createGain(); g.gain.value = i === 2 ? 0.25 : 0.6;
      o.connect(g); g.connect(lp); o.start();
    });
    lp.connect(drone); drone.connect(master);
    SND.droneGain = drone;

    SND.started = true;
    setInterval(() => {
      if (!SND.ctx || SND.muted) return;
      if (SND.crackleT > 0) {
        SND.crackleT -= 0.25;
        if (Math.random() < 0.6) SND.sfx('fire', 0.35);
      }
    }, 250);
  };

  SND.ensure = function () {
    if (!SND.ctx) SND.init();
    if (SND.ctx && SND.ctx.state === 'suspended') SND.ctx.resume();
  };

  SND.setWeather = function (key, night) {
    if (!SND.ctx) return;
    const t = now();
    const rain = key === 'rain' ? 0.075 : key === 'crystal' ? 0.018 : 0;
    SND.rainGain.gain.cancelScheduledValues(t);
    SND.rainGain.gain.linearRampToValueAtTime(rain, t + 1.5);
    const wind = 0.028 + (night ? 0.012 : 0) + (key === 'dust' ? 0.03 : 0) + (key === 'fog' ? 0.012 : 0);
    SND.windGain.gain.cancelScheduledValues(t);
    SND.windGain.gain.linearRampToValueAtTime(wind, t + 1.5);
  };

  SND.campfireNear = function (on) { SND.crackleT = on ? 4 : 0; };

  function env(gain, t0, a, d) {
    gain.gain.setValueAtTime(0.0001, t0);
    gain.gain.exponentialRampToValueAtTime(a, t0 + 0.01);
    gain.gain.exponentialRampToValueAtTime(0.0001, t0 + d);
  }

  function tone(freq, type, dur, vol, slideTo) {
    const ctx = SND.ctx; if (!ctx) return;
    const t = now();
    const o = ctx.createOscillator(); o.type = type || 'sine';
    o.frequency.setValueAtTime(freq, t);
    if (slideTo) o.frequency.exponentialRampToValueAtTime(slideTo, t + dur);
    const g = ctx.createGain();
    env(g, t, vol || 0.1, dur);
    o.connect(g); g.connect(SND.master);
    o.start(t); o.stop(t + dur + 0.02);
  }

  function noise(dur, vol, type, f0, f1, q) {
    const ctx = SND.ctx; if (!ctx || !SND.noise) return;
    const t = now();
    const src = ctx.createBufferSource(); src.buffer = SND.noise;
    src.loop = true;
    const f = ctx.createBiquadFilter(); f.type = type || 'bandpass';
    f.frequency.setValueAtTime(f0 || 800, t);
    if (f1) f.frequency.exponentialRampToValueAtTime(f1, t + dur);
    f.Q.value = q || 1;
    const g = ctx.createGain(); env(g, t, vol || 0.12, dur);
    src.connect(f); f.connect(g); g.connect(SND.master);
    src.start(t); src.stop(t + dur + 0.02);
  }

  SND.sfx = function (name, vol) {
    if (!SND.ctx || SND.muted) return;
    vol = vol === undefined ? 1 : vol;
    switch (name) {
      case 'swing': noise(0.16, 0.20 * vol, 'bandpass', 1400, 380, 0.8); break;
      case 'heavy': noise(0.35, 0.28 * vol, 'lowpass', 700, 120, 0.6); tone(70, 'sine', 0.3, 0.15 * vol, 40); break;
      case 'hit': noise(0.09, 0.25 * vol, 'bandpass', 900, 250, 1.2); tone(95, 'square', 0.10, 0.10 * vol, 60); break;
      case 'parry': tone(1320, 'triangle', 0.25, 0.18 * vol, 990); tone(1980, 'sine', 0.18, 0.08 * vol); noise(0.12, 0.14 * vol, 'highpass', 3000, 0, 1); break;
      case 'block': noise(0.10, 0.20 * vol, 'highpass', 1800, 0, 2); tone(180, 'square', 0.08, 0.07 * vol); break;
      case 'dodge': noise(0.20, 0.13 * vol, 'bandpass', 500, 1800, 0.7); break;
      case 'pickup': tone(660, 'sine', 0.12, 0.10 * vol); setTimeout(() => tone(990, 'sine', 0.14, 0.08 * vol), 70); break;
      case 'crystal': tone(880, 'sine', 0.10, 0.09 * vol); setTimeout(() => tone(1320, 'sine', 0.16, 0.07 * vol), 60); break;
      case 'heal': tone(330, 'sine', 0.5, 0.09 * vol, 660); break;
      case 'level': [440, 554, 659, 880].forEach((f, i) => setTimeout(() => tone(f, 'sine', 0.6, 0.10 * vol), i * 110)); break;
      case 'death': tone(120, 'sawtooth', 1.2, 0.12 * vol, 40); noise(0.8, 0.10 * vol, 'lowpass', 400, 80, 0.5); break;
      case 'ui': tone(1200, 'sine', 0.05, 0.045 * vol); break;
      case 'error': tone(180, 'square', 0.18, 0.06 * vol, 120); break;
      case 'fire': noise(0.04 + Math.random() * 0.05, 0.05 * vol, 'bandpass', 600 + Math.random() * 1200, 0, 3); break;
      case 'shrine': [330, 440, 550, 660, 880].forEach((f, i) => setTimeout(() => tone(f, 'sine', 1.2, 0.07 * vol), i * 90)); break;
      case 'quest': tone(523, 'sine', 0.5, 0.08 * vol); setTimeout(() => tone(784, 'sine', 0.8, 0.08 * vol), 160); break;
      case 'blockBreak': noise(0.28, 0.22 * vol, 'lowpass', 900, 200, 0.8); tone(110, 'square', 0.22, 0.10 * vol, 55); break;
    }
  };

  SND.toggle = function () {
    SND.muted = !SND.muted;
    if (SND.master) SND.master.gain.value = SND.muted ? 0 : 0.55;
    return SND.muted;
  };
})();
