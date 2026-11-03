import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockAuthService, makeTokenParsed } from "~/testUtils";
import { getCommitteeYear } from "~/util/date.util";
import {
  loadCommitteeYear,
  requireBoardOrCandidateBoard,
  requireTokenParsed,
} from "~/util/loaderAuth.util";

const { waitForAuthReady, getSettingsById } = vi.hoisted(() => ({
  waitForAuthReady: vi.fn(),
  getSettingsById: vi.fn(),
}));

vi.mock("~/layout/auth-service", () => ({ waitForAuthReady }));
vi.mock("~/api", () => ({ getSettingsById }));

/** Lets pending promises settle, to check that one never resolves. */
const flushPromises = () => new Promise((resolve) => setTimeout(resolve, 0));

describe("requireTokenParsed", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the logged-in user's token", async () => {
    const token = makeTokenParsed();
    waitForAuthReady.mockResolvedValue(
      createMockAuthService({ getTokenParsed: vi.fn(async () => token) }),
    );

    await expect(requireTokenParsed()).resolves.toBe(token);
  });

  it("redirects to the login page and never resolves when not logged in", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    waitForAuthReady.mockResolvedValue(authService);
    const resolved = vi.fn();

    requireTokenParsed().then(resolved);
    await flushPromises();

    expect(authService.login).toHaveBeenCalledWith(window.location.href);
    expect(resolved).not.toHaveBeenCalled();
  });
});

describe("requireBoardOrCandidateBoard", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the token of a board member", async () => {
    const token = makeTokenParsed({ is_admin: true });
    waitForAuthReady.mockResolvedValue(
      createMockAuthService({ getTokenParsed: vi.fn(async () => token) }),
    );

    await expect(requireBoardOrCandidateBoard()).resolves.toBe(token);
  });

  it("throws a 403 response for other members", async () => {
    waitForAuthReady.mockResolvedValue(
      createMockAuthService({
        getTokenParsed: vi.fn(async () => makeTokenParsed()),
      }),
    );

    await expect(requireBoardOrCandidateBoard()).rejects.toMatchObject({
      init: { status: 403 },
    });
  });
});

describe("loadCommitteeYear", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the committee year based on the configured creation date", async () => {
    getSettingsById.mockResolvedValue({ data: { value: "08-01" } });

    await expect(loadCommitteeYear()).resolves.toBe(getCommitteeYear("08-01"));
    expect(getSettingsById).toHaveBeenCalledWith({
      path: { id: "CommitteeCreationDate" },
    });
  });

  it("falls back to the default creation date when the setting fails to load", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    getSettingsById.mockRejectedValue(new Error("boom"));

    await expect(loadCommitteeYear()).resolves.toBe(getCommitteeYear());
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
