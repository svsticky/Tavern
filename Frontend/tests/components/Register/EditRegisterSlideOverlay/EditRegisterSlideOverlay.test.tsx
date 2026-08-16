import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegisterSlideResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { handleSlideSubmit, handleSlideDelete } = vi.hoisted(() => ({
  handleSlideSubmit: vi.fn(),
  handleSlideDelete: vi.fn(),
}));

vi.mock(
  "~/components/Register/EditRegisterSlideOverlay/EditRegisterSlideOverlay.handlers",
  () => ({
    handleSlideSubmit,
    handleSlideDelete,
  }),
);

import EditRegisterSlideOverlay from "~/components/Register/EditRegisterSlideOverlay/EditRegisterSlideOverlay";

const existingSlide: RegisterSlideResponseDto = {
  id: 5,
  sortOrder: 2,
  imagePath: "existing.png",
} as RegisterSlideResponseDto;

describe("EditRegisterSlideOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders a disabled create button and requires a file when creating a new slide", () => {
    renderWithProviders(<EditRegisterSlideOverlay onComplete={vi.fn()} />);

    expect(screen.getByRole("button", { name: "create" })).toBeDisabled();
    expect(
      screen.queryByRole("button", { name: "delete" }),
    ).not.toBeInTheDocument();
    expect(document.querySelector("input[type='file']")).toBeRequired();
  });

  it("enables save and shows delete when editing an existing slide, file no longer required", () => {
    renderWithProviders(
      <EditRegisterSlideOverlay onComplete={vi.fn()} slide={existingSlide} />,
    );

    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "delete" })).toBeInTheDocument();
    expect(document.querySelector("input[type='file']")).not.toBeRequired();
    expect(screen.getByText("leave_empty_to_keep_current")).toBeInTheDocument();
  });

  it("enables the create button once a file is chosen", async () => {
    const user = userEvent.setup();
    renderWithProviders(<EditRegisterSlideOverlay onComplete={vi.fn()} />);

    const file = new File(["img"], "slide.png", { type: "image/png" });
    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    await user.upload(fileInput, file);

    expect(screen.getByRole("button", { name: "create" })).toBeEnabled();
  });

  it("calls handleSlideSubmit with the current slide file on submit", async () => {
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegisterSlideOverlay
        onComplete={onComplete}
        slide={existingSlide}
      />,
    );

    const form = screen.getByRole("button", { name: "save" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form as HTMLFormElement);

    expect(handleSlideSubmit).toHaveBeenCalledTimes(1);
    const callArgs = handleSlideSubmit.mock.calls[0][0];
    expect(callArgs.slide).toBe(existingSlide);
    expect(callArgs.slideFile).toBeNull();
    expect(callArgs.onComplete).toBe(onComplete);
  });

  it("calls handleSlideDelete with the slide when the delete button is clicked", async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegisterSlideOverlay
        onComplete={onComplete}
        slide={existingSlide}
      />,
    );

    await user.click(screen.getByRole("button", { name: "delete" }));

    expect(handleSlideDelete).toHaveBeenCalledTimes(1);
    const callArgs = handleSlideDelete.mock.calls[0][0];
    expect(callArgs.slide).toBe(existingSlide);
    expect(callArgs.onComplete).toBe(onComplete);
  });
});
