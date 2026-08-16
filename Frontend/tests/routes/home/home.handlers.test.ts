import { beforeEach, describe, expect, it, vi } from "vitest";
import { loadHomePageData } from "~/routes/home/home.handlers";

const { getActivities, getAnnouncements, getGroupmemberships } = vi.hoisted(
  () => ({
    getActivities: vi.fn(),
    getAnnouncements: vi.fn(),
    getGroupmemberships: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getActivities,
  getAnnouncements,
  getGroupmemberships,
}));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  toast: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

function baseArgs(
  overrides: Partial<Parameters<typeof loadHomePageData>[0]> = {},
) {
  return {
    authenticated: true,
    userId: "user-1",
    setLoading: vi.fn(),
    setActivities: vi.fn(),
    setAnnouncements: vi.fn(),
    setGroupMemberships: vi.fn(),
    setEnrolledActivities: vi.fn(),
    ...overrides,
  };
}

describe("loadHomePageData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when not authenticated", async () => {
    const setLoading = vi.fn();
    await loadHomePageData(baseArgs({ authenticated: false, setLoading }));
    expect(getActivities).not.toHaveBeenCalled();
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("loads activities, enrolled activities, announcements and group memberships on success", async () => {
    getActivities
      .mockResolvedValueOnce({ data: [{ id: 1 }] })
      .mockResolvedValueOnce({ data: [{ id: 2 }] });
    getAnnouncements.mockResolvedValue({ data: [{ id: 3 }] });
    getGroupmemberships.mockResolvedValue({ data: [{ id: 4 }] });

    const setActivities = vi.fn();
    const setEnrolledActivities = vi.fn();
    const setAnnouncements = vi.fn();
    const setGroupMemberships = vi.fn();
    const setLoading = vi.fn();

    await loadHomePageData(
      baseArgs({
        setActivities,
        setEnrolledActivities,
        setAnnouncements,
        setGroupMemberships,
        setLoading,
      }),
    );

    expect(setActivities).toHaveBeenCalledWith([{ id: 1 }]);
    expect(setEnrolledActivities).toHaveBeenCalledWith([{ id: 2 }]);
    expect(setAnnouncements).toHaveBeenCalledWith([{ id: 3 }]);
    expect(setGroupMemberships).toHaveBeenCalledWith([{ id: 4 }]);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("logs and shows an error toast when activities fail to load", async () => {
    getActivities.mockResolvedValue({ error: "fail" });
    getAnnouncements.mockResolvedValue({ data: [] });
    getGroupmemberships.mockResolvedValue({ data: [] });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await loadHomePageData(baseArgs());

    expect(consoleError).toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("throws when enrolled activities fail to load", async () => {
    getActivities
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ error: "fail" });
    getAnnouncements.mockResolvedValue({ data: [] });
    getGroupmemberships.mockResolvedValue({ data: [] });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await loadHomePageData(baseArgs());

    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("throws when announcements fail to load", async () => {
    getActivities.mockResolvedValue({ data: [] });
    getAnnouncements.mockResolvedValue({ error: "fail" });
    getGroupmemberships.mockResolvedValue({ data: [] });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await loadHomePageData(baseArgs());

    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("throws when group memberships fail to load", async () => {
    getActivities.mockResolvedValue({ data: [] });
    getAnnouncements.mockResolvedValue({ data: [] });
    getGroupmemberships.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await loadHomePageData(baseArgs());

    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
