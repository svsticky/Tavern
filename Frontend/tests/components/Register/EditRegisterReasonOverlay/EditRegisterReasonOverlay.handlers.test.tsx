import { waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegisterReasonResponseDto } from "~/api";

const {
  deleteRegisterreasonsById,
  postRegisterreasons,
  postRegisterreasonsByIdIcon,
  putRegisterreasonsById,
} = vi.hoisted(() => ({
  deleteRegisterreasonsById: vi.fn(),
  postRegisterreasons: vi.fn(),
  postRegisterreasonsByIdIcon: vi.fn(),
  putRegisterreasonsById: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteRegisterreasonsById,
  postRegisterreasons,
  postRegisterreasonsByIdIcon,
  putRegisterreasonsById,
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
  handleReasonDelete,
  handleReasonSubmit,
} from "~/components/Register/EditRegisterReasonOverlay/EditRegisterReasonOverlay.handlers";

function baseFormData() {
  return {
    titleDutch: "Titel",
    titleEnglish: "Title",
    descriptionDutch: "Omschrijving",
    descriptionEnglish: "Description",
    sortOrder: 1,
  };
}

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

const existingReason: RegisterReasonResponseDto = {
  id: 5,
  titleDutch: "Oud",
  titleEnglish: "Old",
  descriptionDutch: "Oude omschrijving",
  descriptionEnglish: "Old description",
  sortOrder: 2,
  iconPath: null,
} as RegisterReasonResponseDto;

describe("EditRegisterReasonOverlay.handlers", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("handleReasonSubmit", () => {
    it("creates a new reason when no reason is passed", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      postRegisterreasons.mockResolvedValue({ data: { id: 10 } });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        reason: undefined,
        setLoading,
        onComplete,
      });

      expect(postRegisterreasons).toHaveBeenCalledWith({
        body: baseFormData(),
      });
      expect(setLoading).toHaveBeenCalledWith(true);
      expect(setLoading).toHaveBeenCalledWith(false);
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("uploads the icon after creating a new reason when a file was supplied", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      postRegisterreasons.mockResolvedValue({ data: { id: 10 } });
      postRegisterreasonsByIdIcon.mockResolvedValue({});
      const iconFile = new File(["icon"], "icon.png", { type: "image/png" });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile,
        reason: undefined,
        setLoading,
        onComplete,
      });

      expect(postRegisterreasonsByIdIcon).toHaveBeenCalledWith({
        path: { id: 10 },
        body: { icon: iconFile },
      });
      await waitFor(() => expect(onComplete).toHaveBeenCalledTimes(1));
    });

    it("updates an existing reason via putRegisterreasonsById", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putRegisterreasonsById.mockResolvedValue({});

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        reason: existingReason,
        setLoading,
        onComplete,
      });

      expect(putRegisterreasonsById).toHaveBeenCalledWith({
        path: { id: existingReason.id },
        body: baseFormData(),
      });
      expect(postRegisterreasonsByIdIcon).not.toHaveBeenCalled();
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("uploads the icon for an existing reason using the existing reason id", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putRegisterreasonsById.mockResolvedValue({});
      postRegisterreasonsByIdIcon.mockResolvedValue({});
      const iconFile = new File(["icon"], "icon.png", { type: "image/png" });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile,
        reason: existingReason,
        setLoading,
        onComplete,
      });

      expect(postRegisterreasonsByIdIcon).toHaveBeenCalledWith({
        path: { id: existingReason.id },
        body: { icon: iconFile },
      });
    });

    it("does not call onComplete when create fails with an error response", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postRegisterreasons.mockResolvedValue({
        error: true,
        message: "Bad request",
      });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        reason: undefined,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      expect(consoleError).toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("does not call onComplete when update fails with an error response", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      putRegisterreasonsById.mockResolvedValue({
        error: true,
        message: "Update failed",
      });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        reason: existingReason,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("falls back to a generic error when create fails without a message", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postRegisterreasons.mockResolvedValue({ error: true });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        reason: undefined,
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
      putRegisterreasonsById.mockResolvedValue({ error: true });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        reason: existingReason,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("does not call onComplete when the icon upload fails", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      putRegisterreasonsById.mockResolvedValue({});
      postRegisterreasonsByIdIcon.mockResolvedValue({
        error: "Upload failed",
      });
      const iconFile = new File(["icon"], "icon.png", { type: "image/png" });

      await handleReasonSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile,
        reason: existingReason,
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
      postRegisterreasons.mockResolvedValue({ data: { id: 10 } });

      await handleReasonSubmit({
        e,
        formData: baseFormData(),
        iconFile: null,
        reason: undefined,
        setLoading,
        onComplete,
      });

      expect(e.preventDefault).toHaveBeenCalledTimes(1);
    });
  });

  describe("handleReasonDelete", () => {
    it("deletes a reason and calls onComplete on success", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      deleteRegisterreasonsById.mockResolvedValue({});

      await handleReasonDelete({
        reason: existingReason,
        setLoading,
        onComplete,
      });

      expect(deleteRegisterreasonsById).toHaveBeenCalledWith({
        path: { id: existingReason.id },
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
      deleteRegisterreasonsById.mockResolvedValue({
        error: true,
        message: "Cannot delete",
      });

      await handleReasonDelete({
        reason: existingReason,
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
      deleteRegisterreasonsById.mockResolvedValue({ error: true });

      await handleReasonDelete({
        reason: existingReason,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });
});
