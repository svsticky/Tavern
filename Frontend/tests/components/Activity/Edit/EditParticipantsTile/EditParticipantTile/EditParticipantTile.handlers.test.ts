import { beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import {
  handleParticipantUnenroll,
  handlePriceBlur,
  handlePriceChange,
  savePriceToServer,
} from "~/components/Activity/Edit/EditParticipantsTile/EditParticipantTile/EditParticipantTile.handlers";

const {
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
} = vi.hoisted(() => ({
  deleteEnrollmentsByActivityIdByMemberId: vi.fn(),
  patchEnrollmentsByActivityIdByMemberId: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
}));

const toastFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => toastFn(...args), {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => {
          opts.error?.(err);
        },
      ).catch(() => {});
      return p;
    }),
  }),
}));

function buildEnrollment(
  overrides: Partial<EnrollmentResponseDto> = {},
): EnrollmentResponseDto {
  return {
    member: { id: "member-1", firstName: "Alice", lastName: "Smith" },
    price: 5,
    ...overrides,
  } as EnrollmentResponseDto;
}

describe("handleParticipantUnenroll", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls onUnenroll on success", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onUnenroll = vi.fn();
    const setLoading = vi.fn();

    handleParticipantUnenroll({
      activityId: 1,
      enrollment: buildEnrollment(),
      setLoading,
      onUnenroll,
    });

    await vi.waitFor(() => expect(onUnenroll).toHaveBeenCalled());
    expect(deleteEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: "member-1" },
    });
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("does not call onUnenroll when the response has an error", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: "nope",
    });
    const onUnenroll = vi.fn();
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    handleParticipantUnenroll({
      activityId: 1,
      enrollment: buildEnrollment(),
      setLoading: vi.fn(),
      onUnenroll,
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(onUnenroll).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("savePriceToServer", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when the target price equals the current price", async () => {
    await savePriceToServer({
      activityId: 1,
      targetPrice: 5,
      enrollment: buildEnrollment({ price: 5 }),
      setLoading: vi.fn(),
      setPrice: vi.fn(),
    });
    expect(patchEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
  });

  it("patches the price and updates the enrollment on success", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const enrollment = buildEnrollment({ price: 5 });
    const setLoading = vi.fn();

    await savePriceToServer({
      activityId: 1,
      targetPrice: 10,
      enrollment,
      setLoading,
      setPrice: vi.fn(),
    });

    expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: "member-1" },
      body: [{ op: "replace", path: "/price", value: 10 }],
    });
    expect(enrollment.price).toBe(10);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("reverts the price and rethrows on failure", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: "fail",
    });
    const setPrice = vi.fn();

    await expect(
      savePriceToServer({
        activityId: 1,
        targetPrice: 10,
        enrollment: buildEnrollment({ price: 5 }),
        setLoading: vi.fn(),
        setPrice,
      }),
    ).rejects.toBeTruthy();

    expect(setPrice).toHaveBeenCalledWith(5);
  });
});

describe("handlePriceChange", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  it("sets the price immediately and debounces the save action", () => {
    const setPrice = vi.fn();
    const setDebounceTimeout = vi.fn();
    const saveAction = vi.fn().mockResolvedValue(undefined);

    handlePriceChange({
      e: { target: { value: "12.5" } } as any,
      debounceTimeout: null,
      setPrice,
      setDebounceTimeout,
      saveAction,
    });

    expect(setPrice).toHaveBeenCalledWith(12.5);
    expect(saveAction).not.toHaveBeenCalled();
    expect(setDebounceTimeout).toHaveBeenCalled();

    vi.advanceTimersByTime(600);
    expect(saveAction).toHaveBeenCalledWith(12.5);
    vi.useRealTimers();
  });

  it("clears an existing debounce timeout before setting a new one", () => {
    const existingTimeout = setTimeout(() => {}, 1000);
    const clearSpy = vi.spyOn(global, "clearTimeout");

    handlePriceChange({
      e: { target: { value: "1" } } as any,
      debounceTimeout: existingTimeout,
      setPrice: vi.fn(),
      setDebounceTimeout: vi.fn(),
      saveAction: vi.fn().mockResolvedValue(undefined),
    });

    expect(clearSpy).toHaveBeenCalledWith(existingTimeout);
    vi.useRealTimers();
  });

  it("defaults to 0 when the input value is not a number", () => {
    const setPrice = vi.fn();
    handlePriceChange({
      e: { target: { value: "abc" } } as any,
      debounceTimeout: null,
      setPrice,
      setDebounceTimeout: vi.fn(),
      saveAction: vi.fn().mockResolvedValue(undefined),
    });
    expect(setPrice).toHaveBeenCalledWith(0);
    vi.useRealTimers();
  });
});

describe("handlePriceBlur", () => {
  it("clears the timeout and saves immediately when a debounce is pending", () => {
    const timeout = setTimeout(() => {}, 1000);
    const clearSpy = vi.spyOn(global, "clearTimeout");
    const saveAction = vi.fn().mockResolvedValue(undefined);

    handlePriceBlur({ debounceTimeout: timeout, price: 12.345, saveAction });

    expect(clearSpy).toHaveBeenCalledWith(timeout);
    expect(saveAction).toHaveBeenCalledWith(12.35);
  });

  it("does nothing when there is no pending debounce", () => {
    const saveAction = vi.fn();
    handlePriceBlur({ debounceTimeout: null, price: 5, saveAction });
    expect(saveAction).not.toHaveBeenCalled();
  });
});
