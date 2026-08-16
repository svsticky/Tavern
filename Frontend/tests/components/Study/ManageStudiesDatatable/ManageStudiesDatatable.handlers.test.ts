import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";
import {
  fetchStudies,
  handleStudyEdited,
} from "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable.handlers";

const { getStudies } = vi.hoisted(() => ({
  getStudies: vi.fn(),
}));

vi.mock("~/api", () => ({ getStudies }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

describe("fetchStudies", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets studies on success", async () => {
    const studies: Study[] = [{ id: 1, title: "CS" } as Study];
    getStudies.mockResolvedValue({ data: studies });
    const setStudies = vi.fn();
    const setLoading = vi.fn();

    fetchStudies(setLoading, setStudies);
    await vi.waitFor(() => expect(setStudies).toHaveBeenCalledWith(studies));
    expect(setLoading).toHaveBeenCalledWith(true);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("shows an error toast on failure", async () => {
    getStudies.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setStudies = vi.fn();

    fetchStudies(vi.fn(), setStudies);

    await vi.waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    expect(setStudies).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("handleStudyEdited", () => {
  it("removes the edited study from the list when study is undefined (deletion)", () => {
    const setStudies = vi.fn();
    const setIsEditModalOpen = vi.fn();
    const setEditedStudy = vi.fn();

    handleStudyEdited({
      study: undefined,
      editedStudy: { id: 1 } as Study,
      setStudies,
      setIsEditModalOpen,
      setEditedStudy,
    });

    const updater = setStudies.mock.calls[0][0];
    expect(updater([{ id: 1 } as Study, { id: 2 } as Study])).toEqual([
      { id: 2 },
    ]);
    expect(setIsEditModalOpen).toHaveBeenCalledWith(false);
    expect(setEditedStudy).toHaveBeenCalledWith(undefined);
  });

  it("does not touch studies when deleting without an editedStudy", () => {
    const setStudies = vi.fn();
    handleStudyEdited({
      study: undefined,
      editedStudy: undefined,
      setStudies,
      setIsEditModalOpen: vi.fn(),
      setEditedStudy: vi.fn(),
    });
    expect(setStudies).not.toHaveBeenCalled();
  });

  it("replaces an existing study when the id matches", () => {
    const setStudies = vi.fn();
    const updatedStudy = { id: 1, title: "Updated" } as Study;

    handleStudyEdited({
      study: updatedStudy,
      setStudies,
      setIsEditModalOpen: vi.fn(),
      setEditedStudy: vi.fn(),
    });

    const updater = setStudies.mock.calls[0][0];
    expect(updater([{ id: 1, title: "Old" } as Study])).toEqual([updatedStudy]);
  });

  it("appends a new study when no matching id exists", () => {
    const setStudies = vi.fn();
    const newStudy = { id: 2, title: "New" } as Study;

    handleStudyEdited({
      study: newStudy,
      setStudies,
      setIsEditModalOpen: vi.fn(),
      setEditedStudy: vi.fn(),
    });

    const updater = setStudies.mock.calls[0][0];
    expect(updater([{ id: 1, title: "Old" } as Study])).toEqual([
      { id: 1, title: "Old" },
      newStudy,
    ]);
  });
});
