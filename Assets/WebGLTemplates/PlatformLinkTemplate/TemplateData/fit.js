function fit(container, minRatioW, minRatioH, maxRatioW, maxRatioH) {
  let currentR = window.innerWidth / window.innerHeight;

  let minR = minRatioW / minRatioH;
  let maxR = maxRatioW / maxRatioH;

  let clampedR = Math.min(Math.max(currentR, minR), maxR);

  let width = window.innerWidth;
  let height = width / clampedR;

  if (height > window.innerHeight) {
    width = Math.min(width, Math.ceil(window.innerHeight * clampedR));
  }

  height = Math.floor(width / clampedR);
  
  container.style.width = width + "px";
  container.style.height = height + "px";
}