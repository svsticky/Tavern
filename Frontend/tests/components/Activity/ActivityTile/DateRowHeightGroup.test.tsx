import { act, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  DateRowHeightGroup,
  useDateRowHeight,
} from "~/components/Activity/ActivityTile/DateRowHeightGroup";

type ResizeObserverCallback = (
  entries: Array<{ contentRect: { height: number } }>,
) => void;

/** Mirrors vitest.setup.ts's global ResizeObserver stub, used to restore it after a test
 * that swaps in a callback-capturing version. */
class NoopResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}

/**
 * Captures every `new ResizeObserver(cb)` instance created while active, in construction
 * order, so a test can trigger any one of them individually with a fake
 * `contentRect.height` - since jsdom's ResizeObserver stub never fires real callbacks.
 */
function stubResizeObserverCapturingInstances() {
  const instances: {
    callback: ResizeObserverCallback;
    disconnect: ReturnType<typeof vi.fn>;
  }[] = [];
  class CapturingResizeObserver {
    private disconnectSpy = vi.fn();
    constructor(cb: ResizeObserverCallback) {
      instances.push({ callback: cb, disconnect: this.disconnectSpy });
    }
    observe() {}
    unobserve() {}
    disconnect() {
      this.disconnectSpy();
    }
  }
  vi.stubGlobal("ResizeObserver", CapturingResizeObserver);
  return {
    trigger: (index: number, height: number) =>
      instances[index]?.callback([{ contentRect: { height } }]),
    instances,
  };
}

function Row({ testId }: { testId: string }) {
  const { ref, minHeight } = useDateRowHeight();
  return (
    <div>
      <span ref={ref} data-testid={`${testId}-content`} />
      <span data-testid={`${testId}-min-height`}>{minHeight ?? "none"}</span>
    </div>
  );
}

describe("DateRowHeightGroup", () => {
  afterEach(() => {
    vi.stubGlobal("ResizeObserver", NoopResizeObserver);
  });

  it("leaves minHeight undefined outside of a group", () => {
    render(<Row testId="solo" />);
    expect(screen.getByTestId("solo-min-height")).toHaveTextContent("none");
  });

  it("shares the tallest row's height across every row in the group", () => {
    const { trigger } = stubResizeObserverCapturingInstances();

    render(
      <DateRowHeightGroup>
        <Row testId="a" />
        <Row testId="b" />
      </DateRowHeightGroup>,
    );

    act(() => {
      trigger(0, 20);
      trigger(1, 40);
    });

    expect(screen.getByTestId("a-min-height")).toHaveTextContent("40");
    expect(screen.getByTestId("b-min-height")).toHaveTextContent("40");
  });

  it("recomputes the shared height after a row unmounts", () => {
    const { trigger, instances } = stubResizeObserverCapturingInstances();

    const { rerender } = render(
      <DateRowHeightGroup>
        <Row testId="a" />
        <Row testId="b" />
      </DateRowHeightGroup>,
    );

    act(() => {
      trigger(0, 20);
      trigger(1, 40);
    });

    rerender(
      <DateRowHeightGroup>
        <Row testId="a" />
      </DateRowHeightGroup>,
    );

    expect(instances[1]?.disconnect).toHaveBeenCalled();
    expect(screen.getByTestId("a-min-height")).toHaveTextContent("20");
  });
});
