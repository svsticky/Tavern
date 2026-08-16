import { describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";
import {
  handleApplyFilters,
  handleResetFilters,
  loadStudies,
} from "~/components/Member/FilterMemberOverlay/FilterMemberOverlay.handlers";

const { getStudies } = vi.hoisted(() => ({ getStudies: vi.fn() }));

vi.mock("~/api", () => ({ getStudies }));
vi.mock("react-hot-toast", () => ({ default: { error: vi.fn() } }));

describe("loadStudies", () => {
  it("loads studies and sets loading false on success", async () => {
    const studies: Study[] = [{ id: 1, title: "CS" } as Study];
    getStudies.mockResolvedValue({ data: studies });
    const setLoading = vi.fn();
    const setStudies = vi.fn();

    await loadStudies(setLoading, setStudies);

    expect(setStudies).toHaveBeenCalledWith(studies);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("shows an error toast and does not set studies when the request fails", async () => {
    getStudies.mockResolvedValue({ error: { title: "Boom" } });
    const toast = (await import("react-hot-toast")).default;
    const setLoading = vi.fn();
    const setStudies = vi.fn();

    await loadStudies(setLoading, setStudies);

    expect(setStudies).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalled();
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });
});

describe("handleApplyFilters", () => {
  it("compiles the individual filter values into a single DTO", () => {
    const onFilter = vi.fn();
    handleApplyFilters({
      onFilter,
      studyId: 1,
      gratie: true,
      lidVanVerdienste: false,
      ereLid: null,
      begunstiger: true,
      suspended: false,
      inactive: null,
      studyType: "Bachelor",
    });

    expect(onFilter).toHaveBeenCalledWith({
      studyId: 1,
      gratie: true,
      lidVanVerdienste: false,
      ereLid: null,
      begunstiger: true,
      suspended: false,
      inactive: null,
      studyType: "Bachelor",
    });
  });
});

describe("handleResetFilters", () => {
  it("resets every filter setter to null", () => {
    const setters = {
      setStudy: vi.fn(),
      setGratie: vi.fn(),
      setLidVanVerdienste: vi.fn(),
      setEreLid: vi.fn(),
      setBegunstiger: vi.fn(),
      setSuspended: vi.fn(),
      setInactive: vi.fn(),
      setStudyType: vi.fn(),
    };

    handleResetFilters(setters);

    expect(setters.setStudy).toHaveBeenCalledWith(null);
    expect(setters.setGratie).toHaveBeenCalledWith(null);
    expect(setters.setLidVanVerdienste).toHaveBeenCalledWith(null);
    expect(setters.setEreLid).toHaveBeenCalledWith(null);
    expect(setters.setBegunstiger).toHaveBeenCalledWith(null);
    expect(setters.setSuspended).toHaveBeenCalledWith(null);
    expect(setters.setInactive).toHaveBeenCalledWith(null);
    expect(setters.setStudyType).toHaveBeenCalledWith(null);
  });
});
