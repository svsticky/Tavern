import { act, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import UpcomingActivities from "~/components/Activity/UpcomingActivities";
import { renderWithProviders } from "~/testUtils";

type ResizeObserverCallback = (
  entries: Array<{ contentRect: { width: number } }>,
) => void;

/**
 * Captures the callback passed to `new ResizeObserver(cb)` so tests can invoke it manually
 * with a fake `contentRect.width`, since jsdom's ResizeObserver stub (vitest.setup.ts) never
 * fires real layout callbacks.
 */
/** Mirrors vitest.setup.ts's global ResizeObserver stub, used to restore it after a test
 * that swaps in a callback-capturing version. */
class NoopResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}

function stubResizeObserverCapturingCallback() {
  let capturedCallback: ResizeObserverCallback | undefined;
  const disconnect = vi.fn();
  class CapturingResizeObserver {
    constructor(cb: ResizeObserverCallback) {
      capturedCallback = cb;
    }
    observe() {}
    unobserve() {}
    disconnect() {
      disconnect();
    }
  }
  vi.stubGlobal("ResizeObserver", CapturingResizeObserver);
  return {
    trigger: (width: number) =>
      capturedCallback?.([{ contentRect: { width } }]),
    disconnect,
  };
}

function buildActivity(id: number): ActivityResponseDto {
  return {
    id,
    name: `Activity ${id}`,
    price: 0,
    location: "Enschede",
    dateTimeStart: "2026-08-01T10:00:00Z",
    dateTimeEnd: "2026-08-01T12:00:00Z",
    posterFileName: null,
    enrollments: [],
  } as unknown as ActivityResponseDto;
}

describe("UpcomingActivities", () => {
  it("shows a no-content message when there are no activities", () => {
    render(<UpcomingActivities activities={[]} />);
    expect(screen.getByText("no_upcoming_activities")).toBeInTheDocument();
  });

  it("renders a tile for each activity (up to the initial visible count)", () => {
    renderWithProviders(
      <UpcomingActivities activities={[buildActivity(1), buildActivity(2)]} />,
    );

    expect(screen.getByText("Activity 1")).toBeInTheDocument();
    expect(screen.getByText("Activity 2")).toBeInTheDocument();
  });

  afterEach(() => {
    vi.stubGlobal("ResizeObserver", NoopResizeObserver);
  });

  it("stacks vertically when the container is too narrow to fit 2 tiles side-by-side", () => {
    const { trigger } = stubResizeObserverCapturingCallback();

    const { container } = renderWithProviders(
      <UpcomingActivities
        activities={[buildActivity(1), buildActivity(2), buildActivity(3)]}
      />,
    );

    act(() => trigger(100));

    const grid = container.querySelector(".grid");
    expect(grid).toHaveStyle({ gridTemplateColumns: "1fr" });
  });

  it("lays out side-by-side columns when the container is wide enough", () => {
    const { trigger } = stubResizeObserverCapturingCallback();

    const { container } = renderWithProviders(
      <UpcomingActivities
        activities={[buildActivity(1), buildActivity(2), buildActivity(3)]}
      />,
    );

    act(() => trigger(1000));

    const grid = container.querySelector(".grid");
    expect(grid).toHaveStyle({
      gridTemplateColumns: "repeat(3, minmax(250px, 400px))",
    });
  });

  it("disconnects the resize observer on unmount", () => {
    const { trigger, disconnect } = stubResizeObserverCapturingCallback();

    const { unmount } = renderWithProviders(
      <UpcomingActivities activities={[buildActivity(1)]} />,
    );
    act(() => trigger(1000));

    unmount();

    expect(disconnect).toHaveBeenCalled();
  });
});
