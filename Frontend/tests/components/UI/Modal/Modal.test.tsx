import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Modal from "~/components/UI/Modal/Modal";
import { render, screen } from "~/testUtils";

describe("Modal", () => {
  afterEach(() => {
    document.body.style.overflow = "";
  });

  it("renders nothing when closed", () => {
    const { container } = render(
      <Modal isOpen={false} onClose={vi.fn()} title="Details">
        <p>Content</p>
      </Modal>,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("renders the title and children when open", () => {
    render(
      <Modal isOpen onClose={vi.fn()} title="Details">
        <p>Content</p>
      </Modal>,
    );
    expect(screen.getByText("Details")).toBeInTheDocument();
    expect(screen.getByText("Content")).toBeInTheDocument();
  });

  it("locks body scroll while open", () => {
    render(
      <Modal isOpen onClose={vi.fn()} title="Details">
        <p>Content</p>
      </Modal>,
    );
    expect(document.body.style.overflow).toBe("hidden");
  });

  it("calls onClose when the close button is clicked", async () => {
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Details">
        <p>Content</p>
      </Modal>,
    );

    await userEvent.click(screen.getByRole("button"));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose when the backdrop is clicked", async () => {
    const onClose = vi.fn();
    const { baseElement } = render(
      <Modal isOpen onClose={onClose} title="Details">
        <p>Content</p>
      </Modal>,
    );

    // Modal renders via a portal onto document.body, so it's outside the render's own
    // `container` - `baseElement` (defaults to document.body) is what actually contains it.
    const backdrop = baseElement.querySelector(
      ".bg-slate-900\\/60",
    ) as HTMLElement;
    await userEvent.click(backdrop);

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose when the Escape key is pressed", async () => {
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Details">
        <p>Content</p>
      </Modal>,
    );

    await userEvent.keyboard("{Escape}");

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("restores body scroll on unmount", () => {
    const { unmount } = render(
      <Modal isOpen onClose={vi.fn()} title="Details">
        <p>Content</p>
      </Modal>,
    );
    expect(document.body.style.overflow).toBe("hidden");
    unmount();
    expect(document.body.style.overflow).toBe("unset");
  });
});
