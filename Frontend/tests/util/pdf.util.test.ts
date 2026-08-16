import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { addImage, addPage, save, jsPDFCtor } = vi.hoisted(() => {
  const addImage = vi.fn();
  const addPage = vi.fn();
  const save = vi.fn();
  const getWidth = vi.fn(() => 297);
  const getHeight = vi.fn(() => 420);
  const jsPDFCtor = vi.fn(function MockJsPDF(this: unknown) {
    return {
      internal: { pageSize: { getWidth, getHeight } },
      addImage,
      addPage,
      save,
    };
  });
  return { addImage, addPage, save, jsPDFCtor };
});

vi.mock("jspdf", () => ({ jsPDF: jsPDFCtor }));

describe("generateA3Pdf", () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it("fetches each image with the auth token and adds it as a page", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      blob: () => Promise.resolve(new Blob(["fake"], { type: "image/jpeg" })),
    });

    const { generateA3Pdf } = await import("~/util/pdf.util");
    await generateA3Pdf(["https://img.example.com/1.jpg"], "test-token");

    expect(global.fetch).toHaveBeenCalledWith("https://img.example.com/1.jpg", {
      headers: { Authorization: "Bearer test-token" },
    });
    expect(addImage).toHaveBeenCalledTimes(1);
    expect(addImage.mock.calls[0][1]).toBe("JPEG");
    expect(save).toHaveBeenCalledWith("a3.pdf");
  });

  it("adds a new page for every image after the first", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      blob: () => Promise.resolve(new Blob(["fake"], { type: "image/jpeg" })),
    });

    const { generateA3Pdf } = await import("~/util/pdf.util");
    await generateA3Pdf(
      ["https://img.example.com/1.jpg", "https://img.example.com/2.jpg"],
      "test-token",
    );

    expect(addImage).toHaveBeenCalledTimes(2);
    expect(addPage).toHaveBeenCalledTimes(1);
    expect(addPage).toHaveBeenCalledWith("a3", "p");
  });

  it("skips an image whose fetch response is not ok, without throwing", async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: false });

    const { generateA3Pdf } = await import("~/util/pdf.util");
    await generateA3Pdf(["https://img.example.com/missing.jpg"], "token");

    expect(addImage).not.toHaveBeenCalled();
    expect(save).toHaveBeenCalledWith("a3.pdf");
  });

  it("continues past a fetch rejection instead of failing the whole export", async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error("network down"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    const { generateA3Pdf } = await import("~/util/pdf.util");
    await generateA3Pdf(["https://img.example.com/1.jpg"], "token");

    expect(addImage).not.toHaveBeenCalled();
    expect(save).toHaveBeenCalledWith("a3.pdf");
    consoleError.mockRestore();
  });
});
