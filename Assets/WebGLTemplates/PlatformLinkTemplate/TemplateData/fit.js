function fit(container) {
  const RATIO = 9 / 16;

  let width, height;

  if (window.innerWidth / window.innerHeight > RATIO) {
    height = window.innerHeight;
    width = Math.floor(height * RATIO);
  } else {
    width = window.innerWidth;
    height = Math.floor(width / RATIO);
  }

  container.style.width = width + "px";
  container.style.height = height + "px";
}
