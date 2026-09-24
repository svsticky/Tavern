import { act, render } from "@testing-library/react";
import { MemoryRouter, useNavigate } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { GlobalScrollRestoration } from "~/components/GlobalScrollRestoration";
import {
  getScrollPosition,
  recordScrollPosition,
} from "~/util/scrollPosition.util";

let scrollY = 0;
// Like a real page: scrolling moves scrollY, so restoreScroll reaches its target at once.
const scrollTo = vi.fn((options: ScrollToOptions) => {
  scrollY = options.top ?? 0;
});

class NoopResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}
let navigateTo: ReturnType<typeof useNavigate>;

/** Exposes the router's navigate so tests can trigger PUSH / POP navigations. */
function NavigateHandle() {
  navigateTo = useNavigate();
  return null;
}

function renderAt(entries: string[], initialIndex = entries.length - 1) {
  return render(
    <MemoryRouter initialEntries={entries} initialIndex={initialIndex}>
      <GlobalScrollRestoration />
      <NavigateHandle />
    </MemoryRouter>,
  );
}

function scrollWindowTo(y: number) {
  scrollY = y;
  window.dispatchEvent(new Event("scroll"));
}

beforeEach(() => {
  vi.clearAllMocks();
  scrollY = 0;
  vi.stubGlobal("scrollTo", scrollTo);
  vi.stubGlobal("ResizeObserver", NoopResizeObserver);
  Object.defineProperty(window, "scrollY", {
    configurable: true,
    get: () => scrollY,
  });
  window.history.scrollRestoration = "auto";
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("GlobalScrollRestoration", () => {
  it("renders nothing", () => {
    const { container } = renderAt(["/"]);

    expect(container).toBeEmptyDOMElement();
  });

  it("disables native scroll restoration and puts it back on unmount", () => {
    const { unmount } = renderAt(["/"]);
    expect(window.history.scrollRestoration).toBe("manual");

    unmount();
    expect(window.history.scrollRestoration).toBe("auto");
  });

  it("records the scroll position of the current page", () => {
    // jsdom's window.location.pathname is "/", which is the path rendered here.
    renderAt(["/"]);

    scrollWindowTo(275);

    expect(getScrollPosition("/")).toBe(275);
  });

  it("ignores scroll events whose URL no longer matches the rendered page", () => {
    renderAt(["/stale-page"]);

    scrollWindowTo(999);

    expect(getScrollPosition("/stale-page")).toBeUndefined();
  });

  it("stops recording after unmount", () => {
    const { unmount } = renderAt(["/"]);
    scrollWindowTo(10);
    unmount();

    scrollWindowTo(500);

    expect(getScrollPosition("/")).toBe(10);
  });

  it("scrolls to the top when the page is first shown", () => {
    renderAt(["/first-visit"]);

    expect(scrollTo).toHaveBeenCalledWith({ top: 0 });
  });

  it("scrolls to the top on a PUSH navigation", () => {
    recordScrollPosition("/pushed", 300);
    renderAt(["/start"]);
    scrollTo.mockClear();

    act(() => {
      navigateTo("/pushed");
    });

    expect(scrollTo).toHaveBeenCalledWith({ top: 0 });
    expect(scrollTo).not.toHaveBeenCalledWith({ top: 300 });
  });

  it("restores the saved position on a POP navigation", () => {
    recordScrollPosition("/popped-back", 300);
    renderAt(["/popped-back", "/current"]);
    scrollTo.mockClear();

    act(() => {
      navigateTo(-1);
    });

    expect(scrollTo).toHaveBeenCalledWith({ top: 300 });
  });
});
