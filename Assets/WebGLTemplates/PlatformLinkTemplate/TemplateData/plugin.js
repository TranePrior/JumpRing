let ysdk;
let player;
let lastOpenedUrl = '';
let lastOpenTimestamp = 0;

function initializePlugin()
{
  YaGames
    .init()
    .then(sdk => {
    console.log('Yandex SDK initialized');
    ysdk = sdk;
    window.ysdk = sdk;
    initializePlayer();
});
}

function refreshPlayer() {
  if (!ysdk || typeof ysdk.getPlayer !== 'function') {
    return Promise.reject('Yandex SDK getPlayer is not available');
  }

  return ysdk.getPlayer({ scopes: false }).then(_player => {
    player = _player;
    return player;
  });
}

function initializePlayer() {
  return refreshPlayer().then(_player => {
    const auth = (typeof _player.isAuthorized === 'function') ? _player.isAuthorized() : false;
    console.log("Yandex SDK player initialized: [authorized = " + auth + "]");
    console.log("PlatformLink initialized");
    sendMessageToUnity('fjs_platformLinkInitialized');
  });
}

function isPlayerAuthorized() {
  if (!player || typeof player.isAuthorized !== 'function') {
    return false;
  }

  try {
    return player.isAuthorized() === true;
  } catch (error) {
    console.warn('player.isAuthorized error:', error);
    return false;
  }
}

function openAuthDialog() {
  if (!ysdk || !ysdk.auth || typeof ysdk.auth.openAuthDialog !== 'function') {
    console.warn('Yandex SDK auth.openAuthDialog is not available');
    sendMessageToUnity('fjs_onAuthorizedFailed');
    return;
  }

  ysdk.auth.openAuthDialog()
    .then(() => {
      return refreshPlayer().catch(error => {
        console.log('getPlayer after auth error:', error);
      });
    })
    .then(() => {
      if (isPlayerAuthorized()) {
        sendMessageToUnity('fjs_onAuthorized');
      } else {
        sendMessageToUnity('fjs_onAuthorizedFailed');
      }
    })
    .catch(error => {
      console.log('openAuthDialog error:', error);
      sendMessageToUnity('fjs_onAuthorizedFailed');
    });
}

function openLink(url) {
  if (typeof url !== 'string' || url.trim().length === 0) {
    console.warn('Open link failed: url is empty.');
    return;
  }

  const normalizedUrl = url.trim();
  const now = Date.now();
  if (normalizedUrl === lastOpenedUrl && now - lastOpenTimestamp < 1000) {
    console.warn('Open link skipped: duplicate call detected.');
    return;
  }

  lastOpenedUrl = normalizedUrl;
  lastOpenTimestamp = now;

  if (typeof document !== 'undefined' && document.body) {
    const link = document.createElement('a');
    link.href = normalizedUrl;
    link.target = '_blank';
    link.rel = 'noopener noreferrer';
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    return;
  }

  if (typeof window !== 'undefined' && typeof window.open === 'function') {
    const popup = window.open(normalizedUrl, '_blank', 'noopener,noreferrer');
    if (popup) {
      popup.opener = null;
    }
    return;
  }

  console.warn('Open link failed: browser open APIs are not available.');
}

function getAllGames() {
  const api = ysdk?.features?.GamesAPI;
  if (!api || typeof api.getAllGames !== 'function') {
    console.warn('Yandex SDK features.GamesAPI.getAllGames is not available');
    sendMessageToUnity('fjs_onGetAllGamesFailed');
    return;
  }

  api.getAllGames()
    .then(({ games, developerURL }) => {
      try {
        const mapped = (games || []).map(g => ({
          appID: g?.appID || '',
          title: g?.title || '',
          url: g?.url || '',
          coverURL: g?.coverURL || '',
          iconURL: g?.iconURL || ''
        }));

        const payload = JSON.stringify({ games: mapped, developerURL: developerURL || '' });
        sendMessageToUnity('fjs_onGetAllGamesSuccess', payload);
      } catch (error) {
        console.error('Error serializing games list:', error);
        sendMessageToUnity('fjs_onGetAllGamesFailed');
      }
    })
    .catch(err => {
      console.log('getAllGames error:', err);
      sendMessageToUnity('fjs_onGetAllGamesFailed');
    });
}

function loadRemoteConfig() {
  if (!ysdk || typeof ysdk.getFlags !== 'function') {
    console.warn('Yandex SDK getFlags is not available');
    sendMessageToUnity('fjs_onRemoteConfigFailed');
    return;
  }

  ysdk.getFlags({ defaultFlags: {} })
    .then(flags => {
      try {
        const items = Object.keys(flags || {}).map(key => {
          const value = flags[key];
          return {
            key: key,
            value: (value === null || value === undefined) ? null : String(value)
          };
        });

        const payload = JSON.stringify({ items: items });
        sendMessageToUnity('fjs_onRemoteConfigLoaded', payload);
      } catch (error) {
        console.error('Error serializing remote config:', error);
        sendMessageToUnity('fjs_onRemoteConfigFailed');
      }
    })
    .catch(error => {
      console.log('getFlags error:', error);
      sendMessageToUnity('fjs_onRemoteConfigFailed');
    });
}

function sendMessageToUnity(message, value = undefined) {
  myGameInstance.SendMessage('#!_platform_link_#!', message, value);
}

function getLanguage() {
  return ysdk.environment.i18n.lang;
}

function getAppId() {
  return ysdk?.environment?.app?.id || '';
}

function getMetrikaCounterId() {
  const rawCounterId = (typeof window !== 'undefined')
    ? window.__platformLinkYandexMetrikaCounterId
    : null;

  const counterId = Number(rawCounterId);
  if (!Number.isFinite(counterId) || counterId <= 0) {
    return null;
  }

  return counterId;
}

function sendAnalyticsEvent(eventName) {
  if (!eventName || typeof eventName !== 'string') {
    console.warn('Yandex Metrika event name is empty.');
    return;
  }

  const normalizedEventName = eventName.trim();
  if (normalizedEventName.length === 0) {
    console.warn('Yandex Metrika event name is empty.');
    return;
  }

  if (typeof window.ym !== 'function') {
    console.warn('Yandex Metrika is not initialized.');
    return;
  }

  const counterId = getMetrikaCounterId();
  if (counterId === null) {
    console.warn('Yandex Metrika counter id is not configured.');
    return;
  }

  try {
    window.ym(counterId, 'reachGoal', normalizedEventName);
    console.log('Yandex Metrika event sent:', normalizedEventName);
  } catch (error) {
    console.warn('Yandex Metrika event send failed:', error);
  }
}

function sendAnalyticsEventWithData(eventName, eventDataJson) {
  if (!eventName || typeof eventName !== 'string') {
    console.warn('Yandex Metrika event name is empty.');
    return;
  }

  const normalizedEventName = eventName.trim();
  if (normalizedEventName.length === 0) {
    console.warn('Yandex Metrika event name is empty.');
    return;
  }

  if (!eventDataJson || typeof eventDataJson !== 'string') {
    sendAnalyticsEvent(normalizedEventName);
    return;
  }

  if (typeof window.ym !== 'function') {
    console.warn('Yandex Metrika is not initialized.');
    return;
  }

  const counterId = getMetrikaCounterId();
  if (counterId === null) {
    console.warn('Yandex Metrika counter id is not configured.');
    return;
  }

  let eventData;
  try {
    eventData = JSON.parse(eventDataJson);
  } catch (error) {
    console.warn('Yandex Metrika event data JSON parse failed:', error);
    return;
  }

  try {
    window.ym(counterId, 'reachGoal', normalizedEventName, eventData);
    console.log('Yandex Metrika event sent with data:', normalizedEventName, eventData);
  } catch (error) {
    console.warn('Yandex Metrika event send with data failed:', error);
  }
}

function sendGameReadyMessage() {
  if (!ysdk || !ysdk.features || !ysdk.features.LoadingAPI) {
    console.warn('Yandex SDK LoadingAPI is not available yet');
    return;
  }
  ysdk.features.LoadingAPI.ready();
}

// Called from GameplayApi.jslib, which is driven by GameplayApiService on the Unity side.
// Yandex uses these to know when the player is actually playing (it holds back interstitials and
// counts session time off them). The jslib guards on `typeof === 'function'`, so while these were
// missing the whole chain was silently dead.
let gameplayApiIsActive = false;

function gameplayApiStart() {
  if (gameplayApiIsActive) {
    return;
  }
  if (!ysdk || !ysdk.features || !ysdk.features.GameplayAPI) {
    console.warn('Yandex SDK GameplayAPI is not available');
    return;
  }
  gameplayApiIsActive = true;
  ysdk.features.GameplayAPI.start();
}

function gameplayApiStop() {
  if (!gameplayApiIsActive) {
    return;
  }
  if (!ysdk || !ysdk.features || !ysdk.features.GameplayAPI) {
    return;
  }
  gameplayApiIsActive = false;
  ysdk.features.GameplayAPI.stop();
}

function showInterstitialAd() {
  if (!ysdk || !ysdk.adv || typeof ysdk.adv.showFullscreenAdv !== 'function') {
    console.warn('Yandex SDK adv.showFullscreenAdv is not available');
    sendMessageToUnity('fjs_onInterstitialAdError');
    return;
  }

  const onOffline = () => {
    console.log('The interstitial is not open because the user is Offline.');
    sendMessageToUnity('fjs_onInterstitialAdError');
  };

  try {
    ysdk.adv.showFullscreenAdv({
        callbacks: {
            onOpen: () => {
                console.log('Interstitial opened.');
                sendMessageToUnity('fjs_onInterstitialAdOpened');
            },
            onClose: (wasShown) => {
                console.log('Interstitial closed.');
                sendMessageToUnity('fjs_onInterstitialAdClosed');
            }, 
            onError: (error) => {
                console.log('Error while open video ad:', error);
                sendMessageToUnity('fjs_onInterstitialAdError'); // TODO: Add error message
            },
            onoffline: onOffline,
            onOffline: onOffline
        }
    });
  } catch (error) {
    console.log('showFullscreenAdv error:', error);
    sendMessageToUnity('fjs_onInterstitialAdError');
  }
}

function showRewardedAd() {
  if (!ysdk || !ysdk.adv || typeof ysdk.adv.showRewardedVideo !== 'function') {
    console.warn('Yandex SDK adv.showRewardedVideo is not available');
    sendMessageToUnity('fjs_onRewardedAdError');
    return;
  }

  try {
    ysdk.adv.showRewardedVideo({
        callbacks: {
            onOpen: () => {
                console.log('Rewarded ad opened.');
                sendMessageToUnity('fjs_onRewardedAdOpened');
            },
            onRewarded: () => {
                console.log('Rewarded!');
                sendMessageToUnity('fjs_onRewarded');
            },
            onClose: () => {
                console.log('Rewarded ad closed.');
                sendMessageToUnity('fjs_onRewardedAdClosed');
            }, 
            onError: (error) => {
                console.log('Error while open video ad:', error);
                sendMessageToUnity('fjs_onRewardedAdError');
            }
        }
    });
  } catch (error) {
    console.log('showRewardedVideo error:', error);
    sendMessageToUnity('fjs_onRewardedAdError');
  }
}

function purchase(id) {
  ysdk.payments.purchase({ id: id }).then(purchase => {
    console.log('Purchase success');
    sendMessageToUnity('fjs_onPurchaseSuccess');
  }).catch(error => {
    console.log('Purchase error:', error);
    sendMessageToUnity('fjs_onPurchaseError');
  });
}

function getCatalog() {
  if (!ysdk || !ysdk.payments || !ysdk.payments.getCatalog) {
    console.warn('Yandex SDK payments.getCatalog is not available');
    sendMessageToUnity('fjs_onGetCatalogFailed');
    return;
  }

  ysdk.payments.getCatalog()
    .then(products => {
      try {
        const mapped = (products || []).map(p => ({
          id: p.id || '',
          title: p.title || '',
          description: p.description || '',
          iconUrl: p.imageURI || '',
          currencyIconUrl: (typeof p.getPriceCurrencyImage === 'function') ? p.getPriceCurrencyImage('small') : '',
          price: p.price || '',
          priceValue: p.priceValue || '',
          priceCurrencyCode: p.priceCurrencyCode || ''
        }));
        const payload = JSON.stringify({ items: mapped });
        sendMessageToUnity('fjs_onGetCatalogSuccess', payload);
      } catch (e) {
        console.error('Error serializing catalog:', e);
        sendMessageToUnity('fjs_onGetCatalogFailed');
      }
    })
    .catch(error => {
      console.log('getCatalog error:', error);
      sendMessageToUnity('fjs_onGetCatalogFailed');
    });
}

function getProduct(id) {
  if (!ysdk || !ysdk.payments || !ysdk.payments.getCatalog) {
    console.warn('Yandex SDK payments.getCatalog is not available');
    sendMessageToUnity('fjs_onGetProductFailed');
    return;
  }

  ysdk.payments.getCatalog()
    .then(products => {
      const product = (products || []).find(p => p.id === id);
      if (!product) {
        sendMessageToUnity('fjs_onGetProductFailed');
        return;
      }

      try {
        const mapped = {
          id: product.id || '',
          title: product.title || '',
          description: product.description || '',
          iconUrl: product.imageURI || '',
          currencyIconUrl: (typeof product.getPriceCurrencyImage === 'function') ? product.getPriceCurrencyImage('small') : '',
          price: product.price || '',
          priceValue: product.priceValue || '',
          priceCurrencyCode: product.priceCurrencyCode || ''
        };
        const payload = JSON.stringify(mapped);
        sendMessageToUnity('fjs_onGetProductSuccess', payload);
      } catch (e) {
        console.error('Error serializing product:', e);
        sendMessageToUnity('fjs_onGetProductFailed');
      }
    })
    .catch(error => {
      console.log('getProduct error:', error);
      sendMessageToUnity('fjs_onGetProductFailed');
    });
}

function getPurchases() {
  if (!ysdk || !ysdk.payments || !ysdk.payments.getPurchases) {
    console.warn('Yandex SDK payments.getPurchases is not available');
    sendMessageToUnity('fjs_onGetPurchasesFailed');
    return;
  }

  ysdk.payments.getPurchases()
    .then(purchases => {
      try {
        const mapped = (purchases || []).map(p => ({
          productID: p.productID || '',
          purchaseToken: p.purchaseToken || '',
          developerPayload: p.developerPayload || ''
        }));
        const payload = JSON.stringify({ items: mapped });
        sendMessageToUnity('fjs_onGetPurchasesSuccess', payload);
      } catch (e) {
        console.error('Error serializing purchases:', e);
        sendMessageToUnity('fjs_onGetPurchasesFailed');
      }
    })
    .catch(error => {
      console.log('getPurchases error:', error);
      sendMessageToUnity('fjs_onGetPurchasesFailed');
    });
}

function consumePurchase(token) {
  if (!ysdk || !ysdk.payments || !ysdk.payments.consumePurchase) {
    console.warn('Yandex SDK payments.consumePurchase is not available');
    sendMessageToUnity('fjs_onConsumePurchaseFailed', token || '');
    return;
  }

  ysdk.payments.consumePurchase(token)
    .then(() => {
      sendMessageToUnity('fjs_onConsumePurchaseSuccess', token || '');
    })
    .catch(error => {
      console.log('consumePurchase error:', error);
      sendMessageToUnity('fjs_onConsumePurchaseFailed', token || '');
    });
}

// player.setData() replaces the whole player data object instead of merging keys, so the
// full object is mirrored here and always sent as a whole. Writing a single key directly
// would wipe every other key stored in the cloud.
//
// The cache is populated by the first load with a single player.getData() round-trip and
// then reused, so per-key loads resolve instantly instead of hitting the network per key.
const SaveDebounceMs = 1000;

let playerDataCache = null;
// True only after a successful getData(). Writing before that would push a partial object
// over real cloud progress, so saves stay local until the load succeeds.
let playerDataLoaded = false;
let playerDataRequestCallbacks = null;

let pendingWrites = {};
// Canonical form of the object the platform last accepted. setData() rejects with
// "The data does not differ from the previous ones" when the payload matches it.
let lastSentSnapshot = null;
let saveTimerId = null;
let saveInFlight = false;
let saveRequestedWhileInFlight = false;

// Key order from the platform and from local writes differs, so plain JSON.stringify
// would report a change where there is none and trigger a rejected setData().
function snapshotPlayerData(data) {
  return JSON.stringify(Object.keys(data).sort().map(key => [key, data[key]]));
}

function ensurePlayerData(onReady) {
  if (playerDataCache !== null) {
    onReady();
    return;
  }

  if (playerDataRequestCallbacks !== null) {
    playerDataRequestCallbacks.push(onReady);
    return;
  }

  playerDataRequestCallbacks = [onReady];

  player.getData().then(data => {
    playerDataCache = data || {};
    playerDataLoaded = true;
    lastSentSnapshot = snapshotPlayerData(playerDataCache);
    resolvePlayerDataRequests();
  }).catch(error => {
    console.log('getData error:', error);
    playerDataCache = {};
    playerDataLoaded = false;
    resolvePlayerDataRequests();
  });
}

function resolvePlayerDataRequests() {
  const callbacks = playerDataRequestCallbacks;
  playerDataRequestCallbacks = null;
  callbacks.forEach(callback => callback());
}

function commitPlayerData() {
  if (saveInFlight) {
    saveRequestedWhileInFlight = true;
    return;
  }

  ensurePlayerData(() => {
    Object.assign(playerDataCache, pendingWrites);
    pendingWrites = {};

    if (playerDataLoaded === false) {
      console.warn('Skipping setData: player data was never loaded, a write would wipe cloud progress');
      return;
    }

    const snapshot = snapshotPlayerData(playerDataCache);
    if (snapshot === lastSentSnapshot) {
      sendMessageToUnity('fjs_onSaveDataSuccess');
      return;
    }

    saveInFlight = true;

    player.setData(playerDataCache, true).then(() => {
      lastSentSnapshot = snapshot;
      sendMessageToUnity('fjs_onSaveDataSuccess');
    }).catch(error => {
      console.log('setData error:', error);
    }).finally(() => {
      saveInFlight = false;

      if (saveRequestedWhileInFlight) {
        saveRequestedWhileInFlight = false;
        commitPlayerData();
      }
    });
  });
}

// Unity flushes per key, so a single flush produces several calls in one frame. Debouncing
// collapses them into one setData() and keeps the game far below the platform request limit.
function saveToPlatform(key, data)
{
  pendingWrites[key] = data;

  // Keep the cache in sync so a later load reads the freshly written value.
  if (playerDataCache !== null) {
    playerDataCache[key] = data;
  }

  if (saveTimerId !== null) {
    clearTimeout(saveTimerId);
  }

  saveTimerId = setTimeout(() => {
    saveTimerId = null;
    commitPlayerData();
  }, SaveDebounceMs);
}

// The debounce window outlives a tab switch or a close, so pending writes are pushed out
// as soon as the page stops being visible.
function commitPlayerDataImmediately() {
  if (saveTimerId === null) {
    return;
  }

  clearTimeout(saveTimerId);
  saveTimerId = null;
  commitPlayerData();
}

document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'hidden') {
    commitPlayerDataImmediately();
  }
});

window.addEventListener('pagehide', commitPlayerDataImmediately);

function respondLoadFromCache(key) {
  const value = playerDataCache ? playerDataCache[key] : undefined;
  if (value !== undefined && value !== null) {
    sendMessageToUnity('fjs_onLoadDataSuccess', String(value));
  } else {
    sendMessageToUnity('fjs_onLoadDataSuccess', "");
  }
}

function loadFromPlatform(key) {
  ensurePlayerData(() => respondLoadFromCache(key));
}

function saveToLocalStorage(key, data) {
  try {
    localStorage.setItem(key, data);
  }
  catch (error) {
    console.error('Save to local storage error: ', error.message);
  }
}

function loadFromLocalStorage(key)
{
  return localStorage.getItem(key);
}

function getDeviceInfo()
{
  return ysdk.deviceInfo.type;
}

function isNativeShareAvailable() {
  if (typeof navigator === 'undefined' || typeof navigator.share !== 'function') {
    return false;
  }

  if (typeof window !== 'undefined' && window.isSecureContext === false) {
    return false;
  }

  if (typeof document !== 'undefined') {
    const permissionsPolicy = document.permissionsPolicy || document.featurePolicy;
    if (permissionsPolicy && typeof permissionsPolicy.allowsFeature === 'function') {
      try {
        if (!permissionsPolicy.allowsFeature('web-share')) {
          return false;
        }
      } catch (error) {
        console.warn('Failed to check web-share permissions policy:', error);
      }
    }
  }

  return true;
}

function showNativeShare(payloadJson) {
  if (!isNativeShareAvailable()) {
    console.warn('Native share dialog is not available.');
    return;
  }

  let payload = {};
  if (payloadJson) {
    try {
      payload = JSON.parse(payloadJson);
    } catch (error) {
      console.error('Failed to parse native share payload:', error);
      return;
    }
  }

  const shareData = {};
  if (payload.title) {
    shareData.title = payload.title;
  }
  if (payload.text) {
    shareData.text = payload.text;
  }
  if (payload.url) {
    shareData.url = payload.url;
  }

  if (typeof navigator.canShare === 'function') {
    try {
      if (!navigator.canShare(shareData)) {
        console.warn('Native share payload is not supported in this environment.');
        return;
      }
    } catch (error) {
      console.warn('Failed to validate native share payload:', error);
      return;
    }
  }

  navigator.share(shareData).catch(error => {
    if (error && error.name === 'NotAllowedError') {
      console.warn('Native share blocked: call must be from direct user interaction and web-share must be allowed by host permissions policy.');
      return;
    }

    console.warn('Native share was rejected:', error);
  });
}

function getVibrationNavigator() {
  if (typeof navigator !== 'undefined') {
    return navigator;
  }

  if (typeof window !== 'undefined' && window.navigator) {
    return window.navigator;
  }

  if (typeof globalThis !== 'undefined' && globalThis.navigator) {
    return globalThis.navigator;
  }

  return null;
}

function resolveVibrationFunction() {
  const nav = getVibrationNavigator();
  if (!nav) {
    return {
      fn: null,
      source: ''
    };
  }

  if (typeof nav.vibrate === 'function') {
    return {
      fn: nav.vibrate.bind(nav),
      source: 'navigator.vibrate'
    };
  }

  if (typeof nav.webkitVibrate === 'function') {
    return {
      fn: nav.webkitVibrate.bind(nav),
      source: 'navigator.webkitVibrate'
    };
  }

  if (typeof nav.mozVibrate === 'function') {
    return {
      fn: nav.mozVibrate.bind(nav),
      source: 'navigator.mozVibrate'
    };
  }

  if (typeof nav.msVibrate === 'function') {
    return {
      fn: nav.msVibrate.bind(nav),
      source: 'navigator.msVibrate'
    };
  }

  return {
    fn: null,
    source: ''
  };
}

function getVibrationSupportInfo() {
  const nav = getVibrationNavigator();
  if (!nav) {
    return {
      supported: false,
      reason: 'navigator is undefined in this environment'
    };
  }

  const vibration = resolveVibrationFunction();
  if (!vibration.fn) {
    return {
      supported: false,
      reason: 'no vibration method found (vibrate/webkitVibrate/mozVibrate/msVibrate)'
    };
  }

  return {
    supported: true,
    reason: `${vibration.source} is available`
  };
}

function isVibrationSupported() {
  return getVibrationSupportInfo().supported;
}

function getVibrationAttemptBlockReason() {
  if (typeof document !== 'undefined' && document.visibilityState !== 'visible') {
    return 'document.visibilityState is not visible';
  }

  const nav = getVibrationNavigator();

  // navigator.userActivation is not available in all browsers.
  if (nav && nav.userActivation && nav.userActivation.hasBeenActive === false) {
    return 'no user activation has occurred yet';
  }

  return '';
}

function canAttemptVibration() {
  if (!isVibrationSupported()) {
    return false;
  }

  if (getVibrationAttemptBlockReason()) {
    return false;
  }

  return true;
}

function vibrate(durationMs) {
  if (!canAttemptVibration()) {
    return false;
  }

  if (!durationMs || durationMs <= 0) {
    return false;
  }

  const vibration = resolveVibrationFunction();
  if (!vibration.fn) {
    return false;
  }

  try {
    return vibration.fn(durationMs) === true;
  } catch (error) {
    console.warn('Vibration call failed:', error);
    return false;
  }
}

function vibratePattern(patternCsv) {
  if (!canAttemptVibration()) {
    return false;
  }

  if (!patternCsv || typeof patternCsv !== 'string') {
    return false;
  }

  const pattern = patternCsv
    .split(',')
    .map(p => parseInt(p, 10))
    .filter(n => Number.isFinite(n) && n >= 0);

  if (pattern.length === 0) {
    return false;
  }

  const vibration = resolveVibrationFunction();
  if (!vibration.fn) {
    return false;
  }

  try {
    return vibration.fn(pattern) === true;
  } catch (error) {
    console.warn('Vibration pattern call failed:', error);
    return false;
  }
}

function copyToClipboard(text) {
  const value = (typeof text === 'string') ? text : '';

  if (typeof navigator !== 'undefined' &&
      navigator.clipboard &&
      typeof navigator.clipboard.writeText === 'function') {
    navigator.clipboard.writeText(value).catch(error => {
      console.warn('Clipboard API write failed:', error);
      fallbackCopyToClipboard(value);
    });
    return;
  }

  fallbackCopyToClipboard(value);
}

function fallbackCopyToClipboard(text) {
  if (typeof document === 'undefined' || !document.body) {
    console.warn('Clipboard copy is not available in this environment.');
    return;
  }

  const textArea = document.createElement('textarea');
  textArea.value = text;
  textArea.setAttribute('readonly', '');
  textArea.style.position = 'fixed';
  textArea.style.top = '-1000px';
  textArea.style.left = '-1000px';
  textArea.style.opacity = '0';

  document.body.appendChild(textArea);
  textArea.focus();
  textArea.select();

  try {
    document.execCommand('copy');
  } catch (error) {
    console.warn('Fallback clipboard copy failed:', error);
  } finally {
    document.body.removeChild(textArea);
  }
}

function setLeaderboardScore(leaderboardId, score)
{
  if (!ysdk || !ysdk.leaderboards || !ysdk.leaderboards.setScore) {
    console.warn('Yandex SDK leaderboards.setScore is not available');
    return;
  }

  // A rejected setScore is not fatal: the platform simply drops the request
  // (rate limit, missing auth, network). Swallow it so it does not surface as
  // an uncaught rejection, which Unity turns into a modal error dialog.
  ysdk.leaderboards.setScore(leaderboardId, score)
    .catch(err => {
      console.warn('Failed to set leaderboard score:', err);
    });
}

function getLeaderboardPlayerEntry(leaderboardId) {
  if (!ysdk || !ysdk.leaderboards || !ysdk.leaderboards.getPlayerEntry) {
    console.warn('Yandex SDK leaderboards.getPlayerEntry is not available');
    sendMessageToUnity('fjs_onGetPlayerEntryFailed');
    return;
  }

  ysdk.leaderboards.getPlayerEntry(leaderboardId)
    .then(res => {
      try {
        const player = res.player || {};
        const scope = player.scopePermissions || {};
        const payload = {
          score: res.score || 0,
          extraData: res.extraData || '',
          rank: res.rank || 0,
          formattedScore: res.formattedScore || '',
          player: {
            lang: player.lang || '',
            publicName: player.publicName || '',
            uniqueID: player.uniqueID || '',
            avatarUrl: (typeof player.getAvatarSrc === 'function') ? player.getAvatarSrc('medium') : '',
            scopePermissions: {
              avatar: scope.avatar || '',
              public_name: scope.public_name || ''
            }
          }
        };

        const json = JSON.stringify(payload);
        sendMessageToUnity('fjs_onGetPlayerEntrySuccess', json);
      } catch (e) {
        console.error('Error serializing player entry:', e);
        sendMessageToUnity('fjs_onGetPlayerEntryFailed');
      }
    })
    .catch(err => {
      console.log('getPlayerEntry error:', err);
      if (err && err.code === 'LEADERBOARD_PLAYER_NOT_PRESENT') {
        sendMessageToUnity('fjs_onGetPlayerEntryNotPresent');
      } else {
        sendMessageToUnity('fjs_onGetPlayerEntryFailed');
      }
    });
}

function getLeaderboardEntries(leaderboardId, includeUser, quantityAround, quantityTop) {
  if (!ysdk || !ysdk.leaderboards || !ysdk.leaderboards.getEntries) {
    console.warn('Yandex SDK leaderboards.getEntries is not available');
    sendMessageToUnity('fjs_onGetLeaderboardEntriesFailed');
    return;
  }

  const options = {};
  if (includeUser) {
    options.includeUser = true;
  }
  if (quantityAround > 0) {
    options.quantityAround = quantityAround;
  }
  if (quantityTop > 0) {
    options.quantityTop = quantityTop;
  }

  ysdk.leaderboards.getEntries(leaderboardId, options)
    .then(res => {
      try {
        const entries = (res.entries || []).map(e => {
          const player = e.player || {};
          const scope = player.scopePermissions || {};
          return {
            score: e.score || 0,
            extraData: e.extraData || '',
            rank: e.rank || 0,
            formattedScore: e.formattedScore || '',
            player: {
              lang: player.lang || '',
              publicName: player.publicName || '',
              uniqueID: player.uniqueID || '',
              avatarUrl: (typeof player.getAvatarSrc === 'function') ? player.getAvatarSrc('medium') : '',
              scopePermissions: {
                avatar: scope.avatar || '',
                public_name: scope.public_name || ''
              }
            }
          };
        });

        const ranges = (res.ranges || []).map(r => ({
          start: r.start || 0,
          size: r.size || 0
        }));

        const payload = {
          leaderboardId: (res.leaderboard && (res.leaderboard.id || res.leaderboard.name)) || '',
          userRank: res.userRank || 0,
          ranges: ranges,
          entries: entries
        };

        const json = JSON.stringify(payload);
        sendMessageToUnity('fjs_onGetLeaderboardEntriesSuccess', json);
      } catch (e) {
        console.error('Error serializing leaderboard entries:', e);
        sendMessageToUnity('fjs_onGetLeaderboardEntriesFailed');
      }
    })
    .catch(err => {
      console.log('getEntries error:', err);
      sendMessageToUnity('fjs_onGetLeaderboardEntriesFailed');
    });
}
