// Yandex Games 1.6.2.1 / 1.10.1: the play field has to reach the edge of the available area
// without ever spilling outside it.
//
// The gameplay is tuned for 9:16 and the orthographic size is fixed on the vertical axis, so a
// WIDER field would hand the player extra forward visibility and make the run easier. A NARROWER
// one only trims that visibility, while the vertical fail bounds stay identical. Hence the clamp:
// portrait screens longer than 9:16 (every modern phone in fullscreen) fill the viewport outright,
// anything wider is pillarboxed at 9:16.
const MAX_RATIO = 9 / 16; // widest allowed field — beyond this the field would grow, not just fill
const MIN_RATIO = 9 / 21; // narrowest — 21:9 is the longest real phone; below it the shop panel would clip

function fit(container) {
  const viewportRatio = window.innerWidth / window.innerHeight;
  const ratio = Math.min(Math.max(viewportRatio, MIN_RATIO), MAX_RATIO);

  let width, height;

  if (viewportRatio > ratio) {
    height = window.innerHeight;
    width = Math.floor(height * ratio);
  } else {
    width = window.innerWidth;
    height = Math.floor(width / ratio);
  }

  container.style.width = width + "px";
  container.style.height = height + "px";
}
