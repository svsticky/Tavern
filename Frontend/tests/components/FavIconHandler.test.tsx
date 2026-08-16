import { render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import FaviconHandler from "~/components/FavIconHandler";

describe("FaviconHandler", () => {
  beforeEach(() => {
    document.querySelectorAll("link[rel~='icon']").forEach((el) => {
      el.remove();
    });
    vi.stubGlobal("URL", {
      ...URL,
      createObjectURL: vi.fn(() => "blob:favicon-url"),
      revokeObjectURL: vi.fn(),
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders nothing", () => {
    const { container } = render(<FaviconHandler />);
    expect(container).toBeEmptyDOMElement();
  });

  it("creates a link[rel=icon] element when none exists", () => {
    render(<FaviconHandler />);
    const link = document.querySelector("link[rel~='icon']") as HTMLLinkElement;
    expect(link).toBeTruthy();
    expect(link.href).toContain("blob:favicon-url");
  });

  it("reuses an existing link[rel=icon] element", () => {
    const existing = document.createElement("link");
    existing.rel = "icon";
    document.head.appendChild(existing);

    render(<FaviconHandler />);

    const links = document.querySelectorAll("link[rel~='icon']");
    expect(links.length).toBe(1);
    expect(links[0]).toBe(existing);
  });

  it("revokes the blob URL on unmount", () => {
    const { unmount } = render(<FaviconHandler />);
    unmount();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:favicon-url");
  });
});
