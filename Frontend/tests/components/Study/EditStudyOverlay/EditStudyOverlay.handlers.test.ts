import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";
import {
  handleStudyDelete,
  handleStudySubmit,
} from "~/components/Study/EditStudyOverlay/EditStudyOverlay.handlers";

const { deleteStudiesById, postStudies, putStudiesById } = vi.hoisted(() => ({
  deleteStudiesById: vi.fn(),
  postStudies: vi.fn(),
  putStudiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ deleteStudiesById, postStudies, putStudiesById }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastErrorFn(...args),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

describe("handleStudySubmit", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when required fields are missing", async () => {
    await handleStudySubmit({
      e: makeEvent(),
      formData: { title: "", type: "Bachelor", active: true },
      setLoading: vi.fn(),
      onComplete: vi.fn(),
    });

    expect(postStudies).not.toHaveBeenCalled();
    expect(putStudiesById).not.toHaveBeenCalled();
  });

  it("creates a new study when no existing study is given", async () => {
    postStudies.mockResolvedValue({ data: { id: 10 } });
    const onComplete = vi.fn();

    await handleStudySubmit({
      e: makeEvent(),
      formData: {
        title: "Computer Science",
        type: "Bachelor",
        nominalDurationYears: 3,
        active: true,
      },
      setLoading: vi.fn(),
      onComplete,
    });

    expect(postStudies).toHaveBeenCalledWith({
      body: {
        title: "Computer Science",
        type: "Bachelor",
        nominalDurationYears: 3,
      },
    });
    await vi.waitFor(() =>
      expect(onComplete).toHaveBeenCalledWith(
        expect.objectContaining({ id: 10, title: "Computer Science" }),
      ),
    );
  });

  it("updates an existing study when one is given", async () => {
    putStudiesById.mockResolvedValue({});
    const study = { id: 5, title: "Old", type: "Bachelor" } as Study;
    const onComplete = vi.fn();

    await handleStudySubmit({
      e: makeEvent(),
      formData: {
        title: "New title",
        type: "Master",
        nominalDurationYears: 2,
        active: false,
      },
      study,
      setLoading: vi.fn(),
      onComplete,
    });

    expect(putStudiesById).toHaveBeenCalledWith({
      path: { id: 5 },
      body: {
        title: "New title",
        type: "Master",
        nominalDurationYears: 2,
        active: false,
      },
    });
    await vi.waitFor(() =>
      expect(onComplete).toHaveBeenCalledWith(
        expect.objectContaining({ id: 5, title: "New title" }),
      ),
    );
  });

  it("logs and rethrows on failure", async () => {
    postStudies.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleStudySubmit({
      e: makeEvent(),
      formData: {
        title: "X",
        type: "Bachelor",
        nominalDurationYears: 3,
        active: true,
      },
      setLoading: vi.fn(),
      onComplete: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});

describe("handleStudyDelete", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows an error toast when there is no study to delete", async () => {
    await handleStudyDelete({
      study: undefined,
      setLoading: vi.fn(),
      onComplete: vi.fn(),
    });

    expect(deleteStudiesById).not.toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
  });

  it("deletes the study and calls onComplete", async () => {
    deleteStudiesById.mockResolvedValue({});
    const onComplete = vi.fn();

    await handleStudyDelete({
      study: { id: 5 } as Study,
      setLoading: vi.fn(),
      onComplete,
    });

    expect(deleteStudiesById).toHaveBeenCalledWith({ path: { id: 5 } });
    await vi.waitFor(() => expect(onComplete).toHaveBeenCalled());
  });

  it("logs and rethrows on failure", async () => {
    deleteStudiesById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleStudyDelete({
      study: { id: 5 } as Study,
      setLoading: vi.fn(),
      onComplete: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});
