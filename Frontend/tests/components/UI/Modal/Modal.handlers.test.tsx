import { describe, expect, it, vi } from "vitest";
import {
  createModalKeyDownHandler,
  handleModalKeyDown,
} from "~/components/UI/Modal/Modal.handlers";

function makeEvent(key: string): KeyboardEvent {
  return { key } as KeyboardEvent;
}

describe("handleModalKeyDown", () => {
  it("calls onClose when the Escape key is pressed", () => {
    const onClose = vi.fn();
    handleModalKeyDown(makeEvent("Escape"), onClose);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("does not call onClose for other keys", () => {
    const onClose = vi.fn();
    handleModalKeyDown(makeEvent("Enter"), onClose);
    expect(onClose).not.toHaveBeenCalled();
  });
});

describe("createModalKeyDownHandler", () => {
  it("returns a handler that calls onClose on Escape", () => {
    const onClose = vi.fn();
    const handler = createModalKeyDownHandler(onClose);

    handler(makeEvent("Escape"));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("returns a handler that ignores non-Escape keys", () => {
    const onClose = vi.fn();
    const handler = createModalKeyDownHandler(onClose);

    handler(makeEvent("a"));

    expect(onClose).not.toHaveBeenCalled();
  });
});
