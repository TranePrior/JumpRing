// Harness for the player-data storage layer in plugin.js.
// Loads the real file into a vm sandbox with the Yandex SDK, Unity bridge and DOM mocked.
const fs = require('fs');
const vm = require('vm');
const assert = require('assert');
const path = require('path');

const PLUGIN = process.env.PLUGIN_PATH || path.resolve(__dirname, '../../Assets/WebGLTemplates/PlatformLinkTemplate/TemplateData/plugin.js');
const source = fs.readFileSync(PLUGIN, 'utf8');

function makeSandbox({ cloud = {}, getDataFails = false }) {
  const state = {
    cloud: { ...cloud },
    setDataCalls: [],
    getDataCalls: 0,
    unityMessages: [],
    errors: [],
    timers: [],
    listeners: {},
  };

  const player = {
    getData() {
      state.getDataCalls++;
      if (getDataFails) return Promise.reject(new Error('network down'));
      return Promise.resolve({ ...state.cloud });
    },
    setData(data, flush) {
      state.setDataCalls.push({ data: { ...data }, flush });
      // The platform rejects a payload identical to the stored one.
      if (JSON.stringify(state.cloud) === JSON.stringify(data)) {
        return Promise.reject(new Error('The data does not differ from the previous ones.'));
      }
      state.cloud = { ...data }; // setData replaces the whole object, it does not merge.
      return Promise.resolve();
    },
  };

  const sandbox = {
    __mockPlayer: player,
    myGameInstance: {
      SendMessage: (_target, message, value) => state.unityMessages.push({ message, value }),
    },
    console: { log: (...a) => state.errors.push(a.join(' ')), warn: (...a) => state.errors.push(a.join(' ')), error: () => {} },
    setTimeout: (fn, ms) => { const id = state.timers.length; state.timers.push({ fn, ms, cancelled: false }); return id; },
    clearTimeout: (id) => { if (state.timers[id]) state.timers[id].cancelled = true; },
    document: {
      visibilityState: 'visible',
      addEventListener: (name, fn) => { (state.listeners[name] = state.listeners[name] || []).push(fn); },
    },
    window: {
      addEventListener: (name, fn) => { (state.listeners[name] = state.listeners[name] || []).push(fn); },
    },
    location: { href: '' },
    JSON, Object, Promise, Error, String,
  };
  sandbox.globalThis = sandbox;

  vm.createContext(sandbox);
  vm.runInContext(source, sandbox);
  // plugin.js declares `let player` at top level, which shadows the sandbox property.
  vm.runInContext('player = __mockPlayer;', sandbox);

  const api = {
    saveToPlatform: vm.runInContext('saveToPlatform', sandbox),
    loadFromPlatform: vm.runInContext('loadFromPlatform', sandbox),
    document: sandbox.document,
  };
  return { sandbox: api, state };
}

// Fires every pending debounce timer, then drains the microtask queue.
async function runTimers(state) {
  for (const t of state.timers) {
    if (!t.cancelled && !t.fired) { t.fired = true; t.fn(); }
  }
  for (let i = 0; i < 20; i++) await Promise.resolve();
}

const tests = [];
const test = (name, fn) => tests.push({ name, fn });

test('multiple keys are merged into ONE setData with the full object', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { BestScore: '10', DiamondBalance: '5' } });

  sandbox.loadFromPlatform('BestScore');
  await runTimers(state);

  sandbox.saveToPlatform('BestScore', '42');
  sandbox.saveToPlatform('DiamondBalance', '7');
  sandbox.saveToPlatform('ActiveSkinId', 'neon');
  await runTimers(state);

  assert.strictEqual(state.setDataCalls.length, 1, 'three writes must collapse into one setData');
  assert.deepStrictEqual(state.cloud, { BestScore: '42', DiamondBalance: '7', ActiveSkinId: 'neon' });
});

test('re-saving an unchanged value does not call setData at all', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { BestScore: '100' } });

  sandbox.loadFromPlatform('BestScore');
  await runTimers(state);

  sandbox.saveToPlatform('BestScore', '100');
  await runTimers(state);

  assert.strictEqual(state.setDataCalls.length, 0, 'identical payload must never reach the platform');
  assert.ok(state.unityMessages.some(m => m.message === 'fjs_onSaveDataSuccess'), 'Unity still gets a success callback');
  assert.strictEqual(state.errors.length, 0, 'no rejection is logged');
});

test('key order differences do not trigger a redundant setData', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { Zeta: '1', Alpha: '2' } });

  sandbox.loadFromPlatform('Alpha');
  await runTimers(state);

  sandbox.saveToPlatform('Alpha', '2');
  sandbox.saveToPlatform('Zeta', '1');
  await runTimers(state);

  assert.strictEqual(state.setDataCalls.length, 0, 'reordered but equal data must be treated as unchanged');
});

test('a save before any load fetches cloud data first and preserves untouched keys', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { OwnedSkins: 'a,b', DiamondBalance: '99' } });

  sandbox.saveToPlatform('BestScore', '5');
  await runTimers(state);

  assert.strictEqual(state.getDataCalls, 1, 'must read the cloud before overwriting it');
  assert.deepStrictEqual(state.cloud, { OwnedSkins: 'a,b', DiamondBalance: '99', BestScore: '5' });
});

test('a failed getData blocks cloud writes instead of wiping progress', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { DiamondBalance: '99' }, getDataFails: true });

  sandbox.loadFromPlatform('DiamondBalance');
  await runTimers(state);

  sandbox.saveToPlatform('BestScore', '5');
  await runTimers(state);

  assert.strictEqual(state.setDataCalls.length, 0, 'must not push a partial object over real progress');
  assert.deepStrictEqual(state.cloud, { DiamondBalance: '99' }, 'cloud stays intact');
});

test('loads are served from the cache with a single getData round-trip', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { A: '1', B: '2', C: '3' } });

  const seen = [];
  sandbox.loadFromPlatform('A');
  sandbox.loadFromPlatform('B');
  sandbox.loadFromPlatform('C');
  await runTimers(state);

  state.unityMessages.filter(m => m.message === 'fjs_onLoadDataSuccess').forEach(m => seen.push(m.value));
  assert.strictEqual(state.getDataCalls, 1, 'parallel loads must share one network request');
  assert.deepStrictEqual(seen, ['1', '2', '3'], 'each load resolves with its own key');
});

test('hiding the page flushes pending writes immediately', async () => {
  const { sandbox, state } = makeSandbox({ cloud: { BestScore: '1' } });

  sandbox.loadFromPlatform('BestScore');
  await runTimers(state);

  sandbox.saveToPlatform('BestScore', '77');
  sandbox.document.visibilityState = 'hidden';
  state.listeners['visibilitychange'].forEach(fn => fn());
  for (let i = 0; i < 20; i++) await Promise.resolve();

  assert.strictEqual(state.setDataCalls.length, 1, 'pending write must not die with the tab');
  assert.strictEqual(state.setDataCalls[0].flush, true, 'must be sent with flush=true');
  assert.strictEqual(state.cloud.BestScore, '77');
});

(async () => {
  let failed = 0;
  for (const { name, fn } of tests) {
    try {
      await fn();
      console.log(`PASS  ${name}`);
    } catch (e) {
      failed++;
      console.log(`FAIL  ${name}\n      ${e.message}`);
    }
  }
  console.log(failed === 0 ? `\nAll ${tests.length} passed` : `\n${failed}/${tests.length} FAILED`);
  process.exit(failed === 0 ? 0 : 1);
})();
