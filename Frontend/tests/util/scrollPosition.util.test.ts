import { NavigationType } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  getScrollPosition,
  recordScrollPosition,
  restoreScroll,
  scrollToWithRetry,
} from "~/util/scrollPosition.util";

/**
 * jsdom has no layout, so `window.scrollTo` is replaced by a fake page whose
 * scrollable height (`maxScroll`) can grow later, like a page whose images load late.
 */
let scrollY = 0;
let maxScroll = 0;
const scrollTo = vi.fn((options: ScrollToOptions) => {
  scrollY = Math.min(options.top ?? 0, maxScroll);
});

let resizeCallback: (() => void) | undefined;
const observe = vi.fn();
const disconnect = vi.fn();

class CapturingResizeObserver {
  constructor(cb: () => void) {
    resizeCallback = cb;
  }
  observe = observe;
  unobserve() {}
  disconnect = disconnect;
}

beforeEach(() => {
  vi.useFakeTimers();
  scrollY = 0;
  maxScroll = 0;
  resizeCallback = undefined;
  vi.clearAllMocks();
  vi.stubGlobal("scrollTo", scrollTo);
  vi.stubGlobal("ResizeObserver", CapturingResizeObserver);
  Object.defineProperty(window, "scrollY", {
    configurable: true,
    get: () => scrollY,
  });
});

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe("recordScrollPosition / getScrollPosition", () => {
  it("returns undefined for a path that was never recorded", () => {
    expect(getScrollPosition("/never-recorded")).toBeUndefined();
  });

  it("returns the most recently recorded position per path", () => {
    recordScrollPosition("/record-a", 100);
    recordScrollPosition("/record-a", 250);
    recordScrollPosition("/record-b", 40);

    expect(getScrollPosition("/record-a")).toBe(250);
    expect(getScrollPosition("/record-b")).toBe(40);
  });
});

describe("scrollToWithRetry", () => {
  it("scrolls once and does not observe when the target is reached immediately", () => {
    maxScroll = 1000;

    const cancel = scrollToWithRetry(300);

    expect(scrollTo).toHaveBeenCalledWith({ top: 300 });
    expect(observe).not.toHaveBeenCalled();
    expect(() => cancel()).not.toThrow();
  });

  it("keeps retrying as the page grows and stops once the target is reached", () => {
    maxScroll = 100;

    scrollToWithRetry(300);
    expect(scrollY).toBe(100);
    expect(observe).toHaveBeenCalledWith(document.body);

    maxScroll = 200;
    resizeCallback?.();
    expect(scrollY).toBe(200);
    expect(disconnect).not.toHaveBeenCalled();

    maxScroll = 1000;
    resizeCallback?.();
    expect(scrollY).toBe(300);
    expect(disconnect).toHaveBeenCalledTimes(1);
  });

  it("gives up when the user has scrolled away from the last position it set", () => {
    maxScroll = 100;
    scrollToWithRetry(300);
    scrollTo.mockClear();

    scrollY = 50; // the user scrolled
    maxScroll = 1000;
    resizeCallback?.();

    expect(scrollTo).not.toHaveBeenCalled();
    expect(disconnect).toHaveBeenCalledTimes(1);
  });

  it("stops observing after the retry window", () => {
    maxScroll = 100;
    scrollToWithRetry(300);

    vi.advanceTimersByTime(999);
    expect(disconnect).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(disconnect).toHaveBeenCalledTimes(1);
  });

  it("cancel disconnects the observer and clears the timeout", () => {
    maxScroll = 100;
    const cancel = scrollToWithRetry(300);

    cancel();
    expect(disconnect).toHaveBeenCalledTimes(1);

    disconnect.mockClear();
    vi.advanceTimersByTime(5000);
    expect(disconnect).not.toHaveBeenCalled();
  });
});

describe("restoreScroll", () => {
  beforeEach(() => {
    maxScroll = 1000;
  });

  it("restores the saved position on POP navigation", () => {
    recordScrollPosition("/restore-pop", 420);

    restoreScroll("/restore-pop", NavigationType.Pop);

    expect(scrollTo).toHaveBeenCalledWith({ top: 420 });
  });

  it("scrolls to the top on POP when nothing was saved", () => {
    restoreScroll("/restore-unsaved", NavigationType.Pop);

    expect(scrollTo).toHaveBeenCalledWith({ top: 0 });
  });

  it.each([NavigationType.Push, NavigationType.Replace])(
    "ignores the saved position on %s navigation",
    (navigationType) => {
      recordScrollPosition("/restore-push", 420);

      restoreScroll("/restore-push", navigationType);

      expect(scrollTo).toHaveBeenCalledWith({ top: 0 });
    },
  );
});
