import { waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegistrationDocumentResponseDto } from "~/api";

const {
  deleteRegistrationdocumentsById,
  postRegistrationdocuments,
  putRegistrationdocumentsById,
} = vi.hoisted(() => ({
  deleteRegistrationdocumentsById: vi.fn(),
  postRegistrationdocuments: vi.fn(),
  putRegistrationdocumentsById: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteRegistrationdocumentsById,
  postRegistrationdocuments,
  putRegistrationdocumentsById,
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
  handleDocumentDelete,
  handleDocumentSubmit,
} from "~/components/Register/EditRegistrationDocumentOverlay/EditRegistrationDocumentOverlay.handlers";

function baseFormData() {
  return {
    nameDutch: "Naam",
    nameEnglish: "Name",
    url: "https://example.com/doc.pdf",
    sortOrder: 1,
  };
}

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

const existingDocument: RegistrationDocumentResponseDto = {
  id: 5,
  nameDutch: "Oud",
  nameEnglish: "Old",
  url: "https://old.example.com/doc.pdf",
  sortOrder: 2,
} as RegistrationDocumentResponseDto;

describe("EditRegistrationDocumentOverlay.handlers", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("handleDocumentSubmit", () => {
    it("creates a new document when no document is passed", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      postRegistrationdocuments.mockResolvedValue({ data: { id: 10 } });

      await handleDocumentSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        document: undefined,
        setLoading,
        onComplete,
      });

      expect(postRegistrationdocuments).toHaveBeenCalledWith({
        body: baseFormData(),
      });
      expect(setLoading).toHaveBeenCalledWith(true);
      expect(setLoading).toHaveBeenCalledWith(false);
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("updates an existing document via putRegistrationdocumentsById", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      putRegistrationdocumentsById.mockResolvedValue({});

      await handleDocumentSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        document: existingDocument,
        setLoading,
        onComplete,
      });

      expect(putRegistrationdocumentsById).toHaveBeenCalledWith({
        path: { id: existingDocument.id },
        body: baseFormData(),
      });
      expect(onComplete).toHaveBeenCalledTimes(1);
    });

    it("does not call onComplete when create fails with an error response", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postRegistrationdocuments.mockResolvedValue({
        error: true,
        message: "Bad request",
      });

      await handleDocumentSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        document: undefined,
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
      putRegistrationdocumentsById.mockResolvedValue({
        error: true,
        message: "Update failed",
      });

      await handleDocumentSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        document: existingDocument,
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
      postRegistrationdocuments.mockResolvedValue({ error: true });

      await handleDocumentSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        document: undefined,
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
      putRegistrationdocumentsById.mockResolvedValue({ error: true });

      await handleDocumentSubmit({
        e: makeEvent(),
        formData: baseFormData(),
        document: existingDocument,
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
      postRegistrationdocuments.mockResolvedValue({ data: { id: 10 } });

      await handleDocumentSubmit({
        e,
        formData: baseFormData(),
        document: undefined,
        setLoading,
        onComplete,
      });

      expect(e.preventDefault).toHaveBeenCalledTimes(1);
    });
  });

  describe("handleDocumentDelete", () => {
    it("deletes a document and calls onComplete on success", async () => {
      const setLoading = vi.fn();
      const onComplete = vi.fn();
      deleteRegistrationdocumentsById.mockResolvedValue({});

      await handleDocumentDelete({
        document: existingDocument,
        setLoading,
        onComplete,
      });

      expect(deleteRegistrationdocumentsById).toHaveBeenCalledWith({
        path: { id: existingDocument.id },
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
      deleteRegistrationdocumentsById.mockResolvedValue({
        error: true,
        message: "Cannot delete",
      });

      await handleDocumentDelete({
        document: existingDocument,
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
      deleteRegistrationdocumentsById.mockResolvedValue({ error: true });

      await handleDocumentDelete({
        document: existingDocument,
        setLoading,
        onComplete,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(onComplete).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });
});
