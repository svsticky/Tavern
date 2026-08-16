import { waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLinkResponseDto } from "~/api";

const {
  deleteExternallinksById,
  postExternallinks,
  postExternallinksByIdIcon,
  putExternallinksById,
} = vi.hoisted(() => ({
  deleteExternallinksById: vi.fn(),
  postExternallinks: vi.fn(),
  postExternallinksByIdIcon: vi.fn(),
  putExternallinksById: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteExternallinksById,
  postExternallinks,
  postExternallinksByIdIcon,
  putExternallinksById,
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
  handleLinkDelete,
  handleLinkSubmit,
} from "~/components/Admin/EditExternalLinkOverlay/EditExternalLinkOverlay.handlers";

function baseFormData() {
  return {
    titleDutch: "Titel",
    titleEnglish: "Title",
    descriptionDutch: "Omschrijving",
    descriptionEnglish: "Description",
    url: "https://example.com",
    sortOrder: 1,
  };
}

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

const existingLink: ExternalLinkResponseDto = {
  id: 5,
  titleDutch: "Oud",
  titleEnglish: "Old",
  descriptionDutch: "Oude omschrijving",
  descriptionEnglish: "Old description",
  url: "https://old.example.com",
  sortOrder: 2,
  iconPath: null,
} as ExternalLinkResponseDto;

describe("EditExternalLinkOverlay.handlers", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("handleLinkSubmit", () => {
    it("creates a new link when no link is passed", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      postExternallinks.mockResolvedValue({ data: { id: 10 } });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        link: undefined,
        setLoading,
        onComplete,
      });

      expect(postExternallinks).toHaveBeenCalledWith({
        body: baseFormData(),
      });
      expect(setLoading).toHaveBeenCalledWith(true);
      expect(setLoading).toHaveBeenCalledWith(false);
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("uploads the icon after creating a new link when a file was supplied", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      postExternallinks.mockResolvedValue({ data: { id: 10 } });
      postExternallinksByIdIcon.mockResolvedValue({});
      const iconFile = new File(["icon"], "icon.png", { type: "image/png" });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile,
        link: undefined,
        setLoading,
        onComplete,
      });

      expect(postExternallinksByIdIcon).toHaveBeenCalledWith({
        path: { id: 10 },
        body: { icon: iconFile },
      });
      await waitFor(() => expect(onComplete).toHaveBeenCalledTimes(1));
    });

    it("updates an existing link via putExternallinksById", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putExternallinksById.mockResolvedValue({});

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        link: existingLink,
        setLoading,
        onComplete,
      });

      expect(putExternallinksById).toHaveBeenCalledWith({
        path: { id: existingLink.id },
        body: baseFormData(),
      });
      expect(postExternallinksByIdIcon).not.toHaveBeenCalled();
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("uploads the icon for an existing link using the existing link id", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putExternallinksById.mockResolvedValue({});
      postExternallinksByIdIcon.mockResolvedValue({});
      const iconFile = new File(["icon"], "icon.png", { type: "image/png" });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile,
        link: existingLink,
        setLoading,
        onComplete,
      });

      expect(postExternallinksByIdIcon).toHaveBeenCalledWith({
        path: { id: existingLink.id },
        body: { icon: iconFile },
      });
    });

    it("does not call onComplete when create fails with an error response", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postExternallinks.mockResolvedValue({
        error: true,
        message: "Bad request",
      });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        link: undefined,
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
      putExternallinksById.mockResolvedValue({
        error: true,
        message: "Update failed",
      });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        link: existingLink,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("does not call onComplete when the icon upload fails", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      putExternallinksById.mockResolvedValue({});
      postExternallinksByIdIcon.mockResolvedValue({ error: "Upload failed" });
      const iconFile = new File(["icon"], "icon.png", { type: "image/png" });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile,
        link: existingLink,
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
      postExternallinks.mockResolvedValue({ error: true });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        link: undefined,
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
      putExternallinksById.mockResolvedValue({ error: true });

      await handleLinkSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        iconFile: null,
        link: existingLink,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("calls preventDefault on the passed event", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const e = makeEvent();
      postExternallinks.mockResolvedValue({ data: { id: 10 } });

      await handleLinkSubmit({
        e,
        formData: baseFormData(),
        iconFile: null,
        link: undefined,
        setLoading,
        onComplete,
      });

      expect(e.preventDefault).toHaveBeenCalledTimes(1);
    });
  });

  describe("handleLinkDelete", () => {
    it("deletes a link and calls onComplete on success", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      deleteExternallinksById.mockResolvedValue({});

      await handleLinkDelete({ link: existingLink, setLoading, onComplete });

      expect(deleteExternallinksById).toHaveBeenCalledWith({
        path: { id: existingLink.id },
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
      deleteExternallinksById.mockResolvedValue({
        error: true,
        message: "Cannot delete",
      });

      await handleLinkDelete({ link: existingLink, setLoading, onComplete });

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
      deleteExternallinksById.mockResolvedValue({ error: true });

      await handleLinkDelete({ link: existingLink, setLoading, onComplete });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });
});
