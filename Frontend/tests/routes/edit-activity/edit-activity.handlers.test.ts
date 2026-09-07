import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getEditActivityBackPath,
  loadEditActivityData,
} from "~/routes/edit-activity/edit-activity.handlers";

const { getActivitiesById } = vi.hoisted(() => ({
  getActivitiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivitiesById }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

describe("loadEditActivityData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing but stop loading when creating a new activity", async () => {
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadEditActivityData({
      isEdit: false,
      id: undefined,
      setActivity,
      setLoading,
    });

    expect(getActivitiesById).not.toHaveBeenCalled();
    expect(setActivity).not.toHaveBeenCalled();
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("fetches and sets the activity when editing", async () => {
    const activity = { id: 5, name: "Party" } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadEditActivityData({
      isEdit: true,
      id: "5",
      setActivity,
      setLoading,
    });

    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } });
    expect(setActivity).toHaveBeenCalledWith(activity);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("logs and shows an error toast when the fetch fails", async () => {
    getActivitiesById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setActivity = vi.fn();

    await loadEditActivityData({
      isEdit: true,
      id: "5",
      setActivity,
      setLoading: vi.fn(),
    });

    expect(setActivity).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("fetches and clones the source activity when cloneFromId is provided", async () => {
    const sourceActivity: ActivityResponseDto = {
      id: 42,
      name: "BBQ Evening",
      price: 10,
      dutchDescription: "Gezellige BBQ",
      englishDescription: "Fun BBQ",
      dateTimeStart: "2026-06-01T18:00:00Z",
      dateTimeEnd: "2026-06-01T22:00:00Z",
      location: "Park",
      participantLimit: 30,
      showInKoala: true,
      showOnWebsite: true,
      isEnrollable: true,
      areParticipantsVisible: true,
      isAdultOnly: false,
      isWeeklyDrinks: false,
      enrollmentDeadline: "2026-05-30T23:59:00Z",
      unenrollmentDeadline: "2026-05-31T12:00:00Z",
      enrollOpenDate: "2026-05-01T00:00:00Z",
      posterFileName: "old-poster.png",
      posterPath: "/posters/old-poster.png",
      isArchived: true,
      enrollments: [{ id: 1 } as any],
      specificationQuestions: [
        { id: 101, question: "Dietary", options: ["Meat", "Veggie"] } as any,
      ],
    };

    getActivitiesById.mockResolvedValue({ data: sourceActivity });
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadEditActivityData({
      isEdit: false,
      id: undefined,
      cloneFromId: "42",
      setActivity,
      setLoading,
    });

    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 42 } });
    expect(setLoading).toHaveBeenCalledWith(false);
    expect(setActivity).toHaveBeenCalledWith(
      expect.objectContaining({
        id: 0,
        name: expect.stringContaining("BBQ Evening"),
        enrollmentDeadline: null,
        unenrollmentDeadline: null,
        enrollOpenDate: null,
        posterFileName: null,
        posterPath: null,
        enrollments: [],
        isArchived: false,
        showInKoala: false,
        specificationQuestions: [
          expect.objectContaining({
            id: undefined,
            question: "Dietary",
            options: ["Meat", "Veggie"],
          }),
        ],
      }),
    );
  });

  it("handles error when cloning fails", async () => {
    getActivitiesById.mockResolvedValue({ error: "Source not found" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadEditActivityData({
      isEdit: false,
      id: undefined,
      cloneFromId: "999",
      setActivity,
      setLoading,
    });

    expect(setActivity).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
    expect(setLoading).toHaveBeenCalledWith(false);
    consoleError.mockRestore();
  });
});

describe("getEditActivityBackPath", () => {
  it("returns the admin activity detail path when editing in an admin context", () => {
    expect(getEditActivityBackPath("/admin/activities/edit/5", true, "5")).toBe(
      "/admin/activities/5",
    );
  });

  it("returns the plain activity detail path when editing outside admin", () => {
    expect(getEditActivityBackPath("/activities/edit/5", true, "5")).toBe(
      "/activities/5",
    );
  });

  it("returns the admin activities list path when creating in an admin context", () => {
    expect(
      getEditActivityBackPath("/admin/activities/create", false, undefined),
    ).toBe("/admin/activities");
  });

  it("returns the plain activities list path when creating outside admin", () => {
    expect(
      getEditActivityBackPath("/activities/create", false, undefined),
    ).toBe("/activities");
  });
});
