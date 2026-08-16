import { waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegisterSlideResponseDto } from "~/api";

const {
  deleteRegisterslidesById,
  postRegisterslides,
  postRegisterslidesByIdImage,
  putRegisterslidesById,
} = vi.hoisted(() => ({
  deleteRegisterslidesById: vi.fn(),
  postRegisterslides: vi.fn(),
  postRegisterslidesByIdImage: vi.fn(),
  putRegisterslidesById: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteRegisterslidesById,
  postRegisterslides,
  postRegisterslidesByIdImage,
  putRegisterslidesById,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>, opts: any) =>
      p.then(
        (data) =>
          typeof opts?.success === "function" ? opts.success(data) : data,
        (err) => (typeof opts?.error === "function" ? opts.error(err) : err),
      ),
    ),
  },
}));

import {
  handleSlideDelete,
  handleSlideSubmit,
} from "~/components/Register/EditRegisterSlideOverlay/EditRegisterSlideOverlay.handlers";

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

const existingSlide: RegisterSlideResponseDto = {
  id: 5,
  sortOrder: 2,
  imagePath: "existing.png",
} as RegisterSlideResponseDto;

describe("EditRegisterSlideOverlay.handlers", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("handleSlideSubmit", () => {
    it("creates a new slide with the supplied file when no slide is passed", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      postRegisterslides.mockResolvedValue({ data: { id: 10 } });
      const slideFile = new File(["img"], "slide.png", {
        type: "image/png",
      });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile,
        slide: undefined,
        setLoading,
        onComplete,
      });

      expect(postRegisterslides).toHaveBeenCalledWith({
        body: { Image: slideFile },
      });
      expect(setLoading).toHaveBeenCalledWith(true);
      expect(setLoading).toHaveBeenCalledWith(false);
      expect(onComplete).toHaveBeenCalledTimes(1);
      expect(postRegisterslidesByIdImage).not.toHaveBeenCalled();
    });

    it("throws when creating without a slide file", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile: null,
        slide: undefined,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(postRegisterslides).not.toHaveBeenCalled();
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("does not call onComplete when create fails with an error response", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postRegisterslides.mockResolvedValue({
        error: true,
        message: "Bad request",
      });
      const slideFile = new File(["img"], "slide.png", {
        type: "image/png",
      });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile,
        slide: undefined,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      expect(consoleError).toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("falls back to a generic error when create fails without a message", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postRegisterslides.mockResolvedValue({ error: true });
      const slideFile = new File(["img"], "slide.png", {
        type: "image/png",
      });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile,
        slide: undefined,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("falls back to a generic error when update fails without a message", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      putRegisterslidesById.mockResolvedValue({ error: true });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile: null,
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("updates an existing slide via putRegisterslidesById", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putRegisterslidesById.mockResolvedValue({});

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile: null,
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      expect(putRegisterslidesById).toHaveBeenCalledWith({
        path: { id: existingSlide.id },
      });
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("uploads the new image for an existing slide when a file was supplied", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putRegisterslidesById.mockResolvedValue({});
      postRegisterslidesByIdImage.mockResolvedValue({});
      const slideFile = new File(["img"], "slide.png", {
        type: "image/png",
      });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile,
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      expect(postRegisterslidesByIdImage).toHaveBeenCalledWith({
        path: { id: existingSlide.id },
        body: { image: slideFile },
      });
      await waitFor(() => expect(onComplete).toHaveBeenCalledTimes(1));
    });

    it("does not call onComplete when update fails with an error response", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      putRegisterslidesById.mockResolvedValue({
        error: true,
        message: "Update failed",
      });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile: null,
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("does not call onComplete when the image upload fails", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      putRegisterslidesById.mockResolvedValue({});
      postRegisterslidesByIdImage.mockResolvedValue({
        error: "Upload failed",
      });
      const slideFile = new File(["img"], "slide.png", {
        type: "image/png",
      });

      await handleSlideSubmit({
        e: makeEvent(),
        slideFile,
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("calls preventDefault on the passed event", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const e = makeEvent();
      putRegisterslidesById.mockResolvedValue({});

      await handleSlideSubmit({
        e,
        slideFile: null,
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      expect(e.preventDefault).toHaveBeenCalledTimes(1);
    });
  });

  describe("handleSlideDelete", () => {
    it("deletes a slide and calls onComplete on success", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      deleteRegisterslidesById.mockResolvedValue({});

      await handleSlideDelete({
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      expect(deleteRegisterslidesById).toHaveBeenCalledWith({
        path: { id: existingSlide.id },
      });
      expect(setLoading).toHaveBeenCalledWith(true);
      expect(setLoading).toHaveBeenCalledWith(false);
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("does not call onComplete when the delete response has an error", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      deleteRegisterslidesById.mockResolvedValue({
        error: true,
        message: "Cannot delete",
      });

      await handleSlideDelete({
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("falls back to a generic error when delete fails without a message", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      deleteRegisterslidesById.mockResolvedValue({ error: true });

      await handleSlideDelete({
        slide: existingSlide,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });
});
