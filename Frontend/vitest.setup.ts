import "@testing-library/jest-dom/vitest";
import i18next from "i18next";
import { initReactI18next } from "react-i18next";
import { vi } from "vitest";

// react-i18next's useTranslation() returns `i18n: undefined` entirely until an instance has been
// registered via initReactI18next - it's not enough to just set properties on the `i18next`
// singleton, components that destructure `i18n` from useTranslation() (e.g. to read
// `i18n.language` directly) would still crash. Do a real but minimal, synchronous, network-free
// init: no HttpBackend, no LanguageDetector, empty resources. `t()` still falls back to returning
// the key for any missing translation (same behavior tests already rely on), and `i18n.language`
// is a real, safe "en".
i18next.use(initReactI18next).init({
  lng: "en",
  fallbackLng: "en",
  resources: {},
  interpolation: { escapeValue: false },
});

// A handful of production files (some handlers, some layouts) import `~/i18n` directly rather
// than using useTranslation(). That module wires up the real HttpBackend + LanguageDetector and
// re-runs `.init()` with them on this same i18next singleton, which makes it try to fetch
// translation files over the network - in a test/jsdom environment that has no real server, this
// can hang for a long time rather than failing fast. Mock `~/i18n` globally so importing it just
// returns the already safely-initialized singleton above, regardless of which file does the
// importing.
vi.mock("~/i18n", () => ({ default: i18next }));

// jsdom doesn't implement ResizeObserver, which several components (e.g. NavBar's responsive
// compacting) use. A minimal stub is enough since layout measurement isn't meaningful in jsdom
// anyway - components just need it to exist and not crash.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
vi.stubGlobal("ResizeObserver", ResizeObserverStub);
