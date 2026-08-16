import { describe, expect, it, vi } from "vitest";
import {
  handleClickOutside,
  handleOptionClick,
  toggleDropdown,
} from "~/components/Menu/NavBar/ProfileDropdown/ProfileDropdown.handlers";

describe("toggleDropdown", () => {
  it("toggles isOpen when not compact", () => {
    const setIsOpen = vi.fn();
    toggleDropdown(false, setIsOpen);
    expect(setIsOpen).toHaveBeenCalledWith(expect.any(Function));
    const updater = setIsOpen.mock.calls[0][0];
    expect(updater(false)).toBe(true);
    expect(updater(true)).toBe(false);
  });

  it("does nothing when compact", () => {
    const setIsOpen = vi.fn();
    toggleDropdown(true, setIsOpen);
    expect(setIsOpen).not.toHaveBeenCalled();
  });
});

describe("handleOptionClick", () => {
  it("calls the action and closes the dropdown when not compact", () => {
    const action = vi.fn();
    const setIsOpen = vi.fn();
    handleOptionClick(action, false, setIsOpen);
    expect(action).toHaveBeenCalled();
    expect(setIsOpen).toHaveBeenCalledWith(false);
  });

  it("calls the action but leaves the dropdown state alone when compact", () => {
    const action = vi.fn();
    const setIsOpen = vi.fn();
    handleOptionClick(action, true, setIsOpen);
    expect(action).toHaveBeenCalled();
    expect(setIsOpen).not.toHaveBeenCalled();
  });
});

describe("handleClickOutside", () => {
  it("closes the dropdown when the click target is outside the ref", () => {
    const outsideEl = document.createElement("div");
    const containerEl = document.createElement("div");
    const containsSpy = vi
      .spyOn(containerEl, "contains")
      .mockReturnValue(false);
    const setIsOpen = vi.fn();

    handleClickOutside(
      { target: outsideEl } as unknown as MouseEvent,
      { current: containerEl },
      setIsOpen,
    );

    expect(setIsOpen).toHaveBeenCalledWith(false);
    containsSpy.mockRestore();
  });

  it("does nothing when the click target is inside the ref", () => {
    const containerEl = document.createElement("div");
    const innerEl = document.createElement("span");
    containerEl.appendChild(innerEl);
    const setIsOpen = vi.fn();

    handleClickOutside(
      { target: innerEl } as unknown as MouseEvent,
      { current: containerEl },
      setIsOpen,
    );

    expect(setIsOpen).not.toHaveBeenCalled();
  });

  it("does nothing when the ref is not yet attached", () => {
    const setIsOpen = vi.fn();
    handleClickOutside(
      { target: document.createElement("div") } as unknown as MouseEvent,
      { current: null },
      setIsOpen,
    );
    expect(setIsOpen).not.toHaveBeenCalled();
  });
});
