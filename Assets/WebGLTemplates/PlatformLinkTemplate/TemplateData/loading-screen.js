(function () {
  const MODE = {
    PC: "pc",
    MOBILE: "mobile"
  };

  const DESIGN = {
    [MODE.PC]: {
      width: 1440,
      height: 1024,
      catTop: 259,
      catSize: 318.78,
      loadingTop: 606,
      progressTop: 684
    },
    [MODE.MOBILE]: {
      width: 375,
      height: 812,
      catTop: 259,
      catSize: 204.01919555664062,
      loadingTop: 477,
      progressTop: 525
    }
  };

  const MOBILE_MAX_WIDTH = 812;
  const MOBILE_PORTRAIT_ASPECT = 1;
  const MOBILE_FORCE_WIDTH = 480;
  const CAT_SAFE_GAP_PX = 14;
  const CAT_ANIMATION_MAX_SCALE = 1.1;

  const PROGRESS_EPSILON = 0.0001;
  const STALL_DELAY_MS = 900;
  const STALL_START_RATE_PER_SEC = 0.02;
  const STALL_DECAY_PER_SEC = 0.75;
  const STALL_MAX_PROGRESS = 0.995;
  const CATCH_UP_BASE_RATE_PER_SEC = 0.35;
  const CATCH_UP_GAP_RATE_PER_SEC = 2.6;
  const FINAL_RATE_PER_SEC = 3.5;

  let currentOverlayElement = null;
  let currentRootElement = null;
  let currentProgressBarLight = null;
  let currentProgressFillClip = null;
  let currentProgressGlow = null;
  let currentProgressPercent = null;
  let currentDisplayedProgress = 0;
  let currentSourceProgress = 0;
  let hasReceivedSourceProgress = false;
  let lastSourceProgressChangeMs = 0;
  let progressAnimationFrameId = 0;
  let lastAnimationTimeMs = 0;
  let resizeBound = false;

  function getMarkup() {
    return [
      '<div id="loading-page" class="loading-screen loading-screen--pc" data-node-id="3408:21099">',
      '  <div class="loading-screen__content">',
      '    <img class="loading-screen__cat" src="TemplateData/loading-cat.png" alt="RETRO CAT">',
      '    <p class="loading-screen__text">Загрузка...</p>',
      '    <div class="loading-screen__progress">',
      '      <div class="loading-screen__progress-track"></div>',
      '      <div class="loading-screen__progress-glow"></div>',
      '      <div class="loading-screen__progress-fill-clip">',
      '        <div id="progress-bar-light" class="loading-screen__progress-light"></div>',
      '        <div id="progress-bar-fill" class="loading-screen__progress-fill"></div>',
      '      </div>',
      '      </div>',
      '    <p id="loading-progress-percent" class="loading-screen__percent" data-node-id="3447:21103">0%</p>',
      '    <p class="loading-screen__powered">Powered by "RETRO CAT"</p>',
      '  </div>',
      '</div>'
    ].join("");
  }

  function getMode(width, height) {
    const aspect = height > 0 ? width / height : 1;
    return width <= MOBILE_FORCE_WIDTH || (width <= MOBILE_MAX_WIDTH && aspect <= MOBILE_PORTRAIT_ASPECT)
      ? MODE.MOBILE
      : MODE.PC;
  }

  function applyLayout() {
    if (!currentOverlayElement || !currentRootElement) {
      return;
    }

    const rect = currentOverlayElement.getBoundingClientRect();
    const mode = getMode(rect.width, rect.height);
    const design = DESIGN[mode];

    currentRootElement.classList.toggle("loading-screen--mobile", mode === MODE.MOBILE);
    currentRootElement.classList.toggle("loading-screen--pc", mode === MODE.PC);

    const scaleX = design.width > 0 ? rect.width / design.width : 1;
    const scaleY = design.height > 0 ? rect.height / design.height : 1;

    currentRootElement.style.setProperty("--pl-scale-x", `${scaleX}`);
    currentRootElement.style.setProperty("--pl-scale-y", `${scaleY}`);

    const baseCatSizePx = design.catSize;
    const scaledDownCatSizePx = baseCatSizePx * Math.min(scaleY, 1);

    const loadingTopPx = design.loadingTop * scaleY;
    const progressTopPx = design.progressTop * scaleY;
    const minTopLimitPx = Math.min(loadingTopPx, progressTopPx);

    const maxCatSizeBySpacePx = Math.max(0, (minTopLimitPx - CAT_SAFE_GAP_PX) / CAT_ANIMATION_MAX_SCALE);
    const catSizePx = Math.min(baseCatSizePx, scaledDownCatSizePx, maxCatSizeBySpacePx);

    const desiredCatTop = design.catTop * scaleY;
    const maxCatTop = minTopLimitPx - (catSizePx * CAT_ANIMATION_MAX_SCALE) - CAT_SAFE_GAP_PX;
    const catTopPx = Math.max(0, Math.min(desiredCatTop, maxCatTop));

    currentRootElement.style.setProperty("--pl-cat-size-px", `${catSizePx}`);
    currentRootElement.style.setProperty("--pl-cat-top-px", `${catTopPx}`);

    renderProgress(currentDisplayedProgress);
  }

  function onResize() {
    applyLayout();
  }

  function formatPercent(progress) {
    return `${Math.round(progress * 100)}%`;
  }

  function renderProgress(progress) {
    if (!currentProgressFillClip) {
      return;
    }

    const clampedProgress = Math.max(0, Math.min(1, progress));
    currentProgressFillClip.style.setProperty("--pl-progress-value", `${clampedProgress}`);

    if (currentProgressGlow) {
      currentProgressGlow.style.setProperty("--pl-progress-width-percent", `${clampedProgress * 100}`);
    }

    if (currentProgressBarLight) {
      currentProgressBarLight.style.opacity = clampedProgress > 0 ? "1" : "0";
    }

    if (currentProgressPercent) {
      currentProgressPercent.textContent = formatPercent(clampedProgress);
    }
  }

  function close() {
    if (!currentOverlayElement) {
      return;
    }

    stopProgressAnimation();

    currentOverlayElement.style.display = "none";
    currentOverlayElement.innerHTML = "";
    currentOverlayElement = null;
    currentRootElement = null;
    currentProgressBarLight = null;
    currentProgressFillClip = null;
    currentProgressGlow = null;
    currentProgressPercent = null;
    currentDisplayedProgress = 0;
    currentSourceProgress = 0;
    hasReceivedSourceProgress = false;
    lastSourceProgressChangeMs = 0;
  }

  function normalizeProgressValue(progress) {
    const numericProgress = Number(progress);
    if (!Number.isFinite(numericProgress)) {
      return currentSourceProgress;
    }

    const normalized = numericProgress > 1 && numericProgress <= 100
      ? numericProgress / 100
      : numericProgress;

    return Math.max(0, Math.min(1, normalized));
  }

  function setSourceProgress(progress) {
    const nowMs = performance.now();
    const normalizedProgress = normalizeProgressValue(progress);

    if (!hasReceivedSourceProgress) {
      hasReceivedSourceProgress = true;
      currentSourceProgress = normalizedProgress;
      lastSourceProgressChangeMs = nowMs;
      return;
    }

    if (normalizedProgress > currentSourceProgress + PROGRESS_EPSILON) {
      currentSourceProgress = normalizedProgress;
      lastSourceProgressChangeMs = nowMs;
      return;
    }

    if (normalizedProgress >= 1 - PROGRESS_EPSILON) {
      currentSourceProgress = 1;
      lastSourceProgressChangeMs = nowMs;
    }
  }

  function updateDisplayedProgress(nowMs) {
    if (!currentProgressFillClip) {
      return;
    }

    if (lastAnimationTimeMs === 0) {
      lastAnimationTimeMs = nowMs;
      renderProgress(currentDisplayedProgress);
      return;
    }

    const deltaTime = Math.min((nowMs - lastAnimationTimeMs) / 1000, 0.1);
    lastAnimationTimeMs = nowMs;

    if (currentSourceProgress >= 1 - PROGRESS_EPSILON) {
      if (currentDisplayedProgress < 1) {
        const finalGap = 1 - currentDisplayedProgress;
        const finalRate = Math.max(0.35, finalGap * FINAL_RATE_PER_SEC);
        currentDisplayedProgress = Math.min(1, currentDisplayedProgress + finalRate * deltaTime);
      }
      renderProgress(currentDisplayedProgress);
      return;
    }

    if (currentDisplayedProgress + PROGRESS_EPSILON < currentSourceProgress) {
      const gap = currentSourceProgress - currentDisplayedProgress;
      const catchUpRate = CATCH_UP_BASE_RATE_PER_SEC + (gap * CATCH_UP_GAP_RATE_PER_SEC);
      currentDisplayedProgress = Math.min(currentSourceProgress, currentDisplayedProgress + catchUpRate * deltaTime);
      renderProgress(currentDisplayedProgress);
      return;
    }

    if (hasReceivedSourceProgress && currentDisplayedProgress < STALL_MAX_PROGRESS) {
      const idleMs = nowMs - lastSourceProgressChangeMs;
      if (idleMs > STALL_DELAY_MS) {
        const stallSeconds = (idleMs - STALL_DELAY_MS) / 1000;
        const stallRate = STALL_START_RATE_PER_SEC / (1 + (stallSeconds * STALL_DECAY_PER_SEC));
        currentDisplayedProgress = Math.min(STALL_MAX_PROGRESS, currentDisplayedProgress + (stallRate * deltaTime));
      }
    }

    renderProgress(currentDisplayedProgress);
  }

  function startProgressAnimation() {
    if (progressAnimationFrameId !== 0) {
      return;
    }

    lastAnimationTimeMs = 0;

    const step = (nowMs) => {
      if (!currentOverlayElement) {
        progressAnimationFrameId = 0;
        return;
      }

      updateDisplayedProgress(nowMs);
      progressAnimationFrameId = window.requestAnimationFrame(step);
    };

    progressAnimationFrameId = window.requestAnimationFrame(step);
  }

  function stopProgressAnimation() {
    if (progressAnimationFrameId !== 0) {
      window.cancelAnimationFrame(progressAnimationFrameId);
      progressAnimationFrameId = 0;
    }
    lastAnimationTimeMs = 0;
  }

  function create(overlayElement) {
    if (!overlayElement) {
      return null;
    }

    currentOverlayElement = overlayElement;
    overlayElement.style.display = "";
    overlayElement.innerHTML = getMarkup();

    currentRootElement = overlayElement.querySelector("#loading-page");
    currentProgressBarLight = overlayElement.querySelector("#progress-bar-light");
    currentProgressFillClip = overlayElement.querySelector(".loading-screen__progress-fill-clip");
    currentProgressGlow = overlayElement.querySelector(".loading-screen__progress-glow");
    currentProgressPercent = overlayElement.querySelector("#loading-progress-percent");

    currentDisplayedProgress = 0;
    currentSourceProgress = 0;
    hasReceivedSourceProgress = false;
    lastSourceProgressChangeMs = performance.now();

    applyLayout();
    renderProgress(0);
    startProgressAnimation();

    if (!resizeBound) {
      window.addEventListener("resize", onResize);
      resizeBound = true;
    }

    return {
      setProgress(progress) {
        setSourceProgress(progress);
      },
      hide: close
    };
  }

  window.RetroLoadingScreen = {
    create,
    close,
    refreshLayout: applyLayout
  };

  window.closeLoadingScreen = close;
})();
