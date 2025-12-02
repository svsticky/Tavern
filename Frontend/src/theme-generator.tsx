function easeInOut(t: number) {
  return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
}

export function generateThemePalette() {
  const root = document.documentElement;

  for (let i = 0; i <= 1000; i++) {
    const eased = easeInOut(i / 1000);

    let mix: string;

    if (i < 500) {
      const percent = eased * 200;
      mix = `color-mix(in srgb, var(--theme) ${percent}%, white)`;
    } else if (i === 500) {
      mix = `var(--theme)`;
    } else {
      const percent = (1 - eased) * 200;
      mix = `color-mix(in srgb, var(--theme) ${percent}%, black)`;
    }

    root.style.setProperty(`--theme-${i}`, mix);
  }
}

generateThemePalette();
