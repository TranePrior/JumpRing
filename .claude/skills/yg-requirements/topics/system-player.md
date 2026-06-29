# Topic — System media player (1.6.1.6, 1.6.2.5)

Source: <https://yandex.ru/dev/games/doc/ru/requirements/1/6>

## Rule
> В любых браузерах не отображается системный плеер, вызываемый игрой.

The OS media notification / lock-screen controls must not appear because of the game's audio.

## What to verify in code
1. Audio API used: WebAudio is fine; raw `<audio>` / `<video>` elements can trigger MediaSession.
2. Grep for `MediaSession`, `navigator.mediaSession`, `setActionHandler`. Remove if present.
3. Grep for `<audio` / `<video` tags with `controls` or long-form audio — those promote the page to "playing media" and Android can show the system player.
4. Prefer WebAudio (`AudioContext.decodeAudioData`) for SFX and music; do not attach `MediaSession` metadata.

## Common failures
- Howler.js with `html5: true` falls back to `<audio>` and creates a MediaSession entry.
- Setting `navigator.mediaSession.metadata` to show "Now playing" — must be removed.
