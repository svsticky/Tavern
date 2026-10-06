import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  calculateAge,
  formatDate,
  formatDateOnly,
  formatForInput,
  getCommitteeYear,
  getFinancialYear,
  getGlobalCommitteeCreationDate,
  getGlobalFinancialYearStartDate,
  isSameDayInAssociationTimeZone,
  parseInputAsAssociationTime,
  setGlobalCommitteeCreationDate,
  setGlobalFinancialYearStartDate,
} from "~/util/date.util";

describe("formatDate", () => {
  // 2026-03-05T14:30:00Z, well clear of any timezone rollover into a different calendar day.
  const date = new Date("2026-03-05T14:30:00Z");

  it("formats fullDateTime with day, month, year, and time", () => {
    const result = formatDate(date, "fullDateTime");
    expect(result).toMatch(/2026/);
    expect(result).toMatch(/5/);
    expect(result).toMatch(/:/);
  });

  it("formats shortDate without a year or time", () => {
    const result = formatDate(date, "shortDate");
    expect(result).not.toMatch(/2026/);
    expect(result).not.toMatch(/:/);
  });

  it("formats shortDateWithWeekday with a weekday abbreviation, day, and month, but no year or time", () => {
    const result = formatDate(date, "shortDateWithWeekday");
    expect(result).not.toMatch(/2026/);
    expect(result).not.toMatch(/:/);
    expect(result).toMatch(/5/);
  });

  it("formats monthShort as just the month", () => {
    const result = formatDate(date, "monthShort");
    expect(result).not.toMatch(/2026/);
    expect(result).not.toMatch(/5/);
  });

  it("formats timeOnly as hours and minutes", () => {
    expect(formatDate(date, "timeOnly")).toMatch(/^\d{2}:\d{2}$/);
  });

  it("falls back to a plain date for unrecognized/default format", () => {
    const result = formatDate(date, "defaultDate");
    expect(result).toMatch(/2026/);
    expect(result).not.toMatch(/:/);
  });
});

describe("formatForInput", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("formats an ISO string as YYYY-MM-DDTHH:mm in the default Europe/Amsterdam timezone", () => {
    // 10:00Z in January is CET (UTC+1): 11:00 local, same calendar day.
    expect(formatForInput("2026-01-15T10:00:00.000Z")).toBe("2026-01-15T11:00");
  });

  it("rolls over to the next calendar day when the timezone offset pushes past midnight", () => {
    // 23:30Z in March (pre-DST, UTC+1) becomes 00:30 the next day in Amsterdam.
    expect(formatForInput("2026-03-05T23:30:00.000Z")).toBe("2026-03-06T00:30");
  });

  it("uses the AssociationTimeZone env var instead of the default when set", () => {
    vi.stubEnv("AssociationTimeZone", "America/New_York");
    // 10:00Z in January is EST (UTC-5): 05:00 local.
    expect(formatForInput("2026-01-15T10:00:00.000Z")).toBe("2026-01-15T05:00");
  });

  it("returns an empty string for undefined input", () => {
    expect(formatForInput(undefined)).toBe("");
  });

  it("returns an empty string for an invalid date string", () => {
    expect(formatForInput("not-a-date")).toBe("");
  });
});

describe("formatDateOnly", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("formats an ISO string as YYYY-MM-DD in the default Europe/Amsterdam timezone", () => {
    expect(formatDateOnly("2026-01-15T10:00:00.000Z")).toBe("2026-01-15");
  });

  it("rolls over to the next calendar day when the timezone offset pushes past midnight", () => {
    expect(formatDateOnly("2026-03-05T23:30:00.000Z")).toBe("2026-03-06");
  });

  it("uses the AssociationTimeZone env var instead of the default when set", () => {
    vi.stubEnv("AssociationTimeZone", "America/New_York");
    // 01:00Z in January is still 2026-01-14, 20:00 EST.
    expect(formatDateOnly("2026-01-15T01:00:00.000Z")).toBe("2026-01-14");
  });

  it("returns an empty string for undefined or invalid input", () => {
    expect(formatDateOnly(undefined)).toBe("");
    expect(formatDateOnly("not-a-date")).toBe("");
  });
});

describe("parseInputAsAssociationTime", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("interprets a datetime-local value as wall-clock time in the default Europe/Amsterdam timezone", () => {
    // 11:00 in Amsterdam (CET, UTC+1) in January is 10:00Z.
    const result = parseInputAsAssociationTime("2026-01-15T11:00");
    expect(result.toISOString()).toBe("2026-01-15T10:00:00.000Z");
  });

  it("interprets a date-only value as midnight wall-clock time in the association timezone", () => {
    const result = parseInputAsAssociationTime("2026-01-15");
    expect(result.toISOString()).toBe("2026-01-14T23:00:00.000Z");
  });

  it("uses the AssociationTimeZone env var instead of the default when set", () => {
    vi.stubEnv("AssociationTimeZone", "America/New_York");
    // 05:00 in New York (EST, UTC-5) in January is 10:00Z.
    const result = parseInputAsAssociationTime("2026-01-15T05:00");
    expect(result.toISOString()).toBe("2026-01-15T10:00:00.000Z");
  });

  it("round-trips with formatForInput", () => {
    const isoString = "2026-03-05T23:30:00.000Z";
    const inputValue = formatForInput(isoString);
    expect(parseInputAsAssociationTime(inputValue).toISOString()).toBe(
      isoString,
    );
  });

  it("returns an invalid date for undefined or malformed input", () => {
    expect(parseInputAsAssociationTime(undefined).getTime()).toBeNaN();
    expect(parseInputAsAssociationTime("").getTime()).toBeNaN();
    expect(parseInputAsAssociationTime("not-a-date").getTime()).toBeNaN();
  });
});

describe("calculateAge", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllEnvs();
  });

  it("returns null for empty or invalid input", () => {
    expect(calculateAge(undefined)).toBeNull();
    expect(calculateAge("")).toBeNull();
    expect(calculateAge("not-a-date")).toBeNull();
  });

  it("calculates age in the default Europe/Amsterdam timezone, adjusting for a birthday not yet reached this year", () => {
    vi.setSystemTime(new Date("2026-08-15T10:00:00Z"));
    expect(calculateAge("1990-08-15")).toBe(36); // birthday is today
    expect(calculateAge("1990-08-14")).toBe(36); // birthday already passed
    expect(calculateAge("1990-08-16")).toBe(35); // birthday not yet reached
  });

  it("resolves the association's calendar day rather than the raw UTC day at a UTC-midnight boundary", () => {
    // 22:30 UTC on Aug 14 is already 00:30 on Aug 15 in Europe/Amsterdam (CEST, UTC+2).
    // A naive UTC-day comparison would say the Aug 15 birthday hasn't arrived yet (age 35).
    vi.setSystemTime(new Date("2026-08-14T22:30:00Z"));
    expect(calculateAge("1990-08-15")).toBe(36);
  });

  it("uses the AssociationTimeZone env var instead of the default when set", () => {
    // 1990-08-15T00:00Z (the birth instant) is 1990-08-14 20:00 in America/New_York
    // (EDT, UTC-4) - the negative offset rolls the birthday back a calendar day there.
    vi.setSystemTime(new Date("2026-08-14T12:00:00Z"));

    // In the default Europe/Amsterdam (UTC+2), the birthday stays on the 15th, which
    // today (the 14th) hasn't reached yet, so age is one less.
    expect(calculateAge("1990-08-15")).toBe(35);

    // In America/New_York, both "today" and the birthday land on the 14th, so the
    // birthday counts as already reached and age is a full year higher.
    vi.stubEnv("AssociationTimeZone", "America/New_York");
    expect(calculateAge("1990-08-15")).toBe(36);
  });
});

describe("getCommitteeYear / getFinancialYear", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    setGlobalCommitteeCreationDate("");
    setGlobalFinancialYearStartDate("");
  });

  it("uses the default 08-01 rollover: before Aug 1 keeps the current calendar year", () => {
    vi.setSystemTime(new Date("2026-07-31T12:00:00Z"));
    expect(getCommitteeYear()).toBe(2026);
    expect(getFinancialYear()).toBe(2026);
  });

  it("uses the default 08-01 rollover: on/after Aug 1 rolls to next year", () => {
    vi.setSystemTime(new Date("2026-08-01T12:00:00Z"));
    expect(getCommitteeYear()).toBe(2027);
    expect(getFinancialYear()).toBe(2027);
  });

  it("respects an explicit override date argument", () => {
    vi.setSystemTime(new Date("2026-08-15T12:00:00Z"));
    expect(getCommitteeYear("09-15")).toBe(2026);
  });

  it("respects the global override set via setGlobalCommitteeCreationDate", () => {
    vi.setSystemTime(new Date("2026-08-15T12:00:00Z"));
    setGlobalCommitteeCreationDate("09-15");
    expect(getCommitteeYear()).toBe(2026);
  });

  it("respects the global override set via setGlobalFinancialYearStartDate", () => {
    vi.setSystemTime(new Date("2026-02-28T12:00:00Z"));
    setGlobalFinancialYearStartDate("03-01");
    expect(getFinancialYear()).toBe(2025);
  });

  it("exposes the global overrides via their getters", () => {
    setGlobalCommitteeCreationDate("09-15");
    expect(getGlobalCommitteeCreationDate()).toBe("09-15");

    setGlobalFinancialYearStartDate("03-01");
    expect(getGlobalFinancialYearStartDate()).toBe("03-01");
  });

  it("falls back to local date parts when Intl.DateTimeFormat fails", () => {
    vi.setSystemTime(new Date("2026-08-15T12:00:00Z"));
    const originalDateTimeFormat = Intl.DateTimeFormat;
    // @ts-expect-error - deliberately breaking the formatter to exercise the catch path
    Intl.DateTimeFormat = vi.fn(() => {
      throw new Error("boom");
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    expect(getCommitteeYear()).toBe(2027);
    expect(consoleError).toHaveBeenCalled();

    Intl.DateTimeFormat = originalDateTimeFormat;
    consoleError.mockRestore();
  });
});

describe("isSameDayInAssociationTimeZone", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns true for two instants on the same calendar day", () => {
    expect(
      isSameDayInAssociationTimeZone(
        new Date("2026-03-12T08:00:00Z"),
        new Date("2026-03-12T20:00:00Z"),
      ),
    ).toBe(true);
  });

  it("returns false for instants on different calendar days", () => {
    expect(
      isSameDayInAssociationTimeZone(
        new Date("2026-03-12T08:00:00Z"),
        new Date("2026-03-13T08:00:00Z"),
      ),
    ).toBe(false);
  });

  it("compares in the association timezone rather than the local one", () => {
    vi.stubEnv("AssociationTimeZone", "Pacific/Kiritimati");
    // 23:30 UTC on the 12th is already 12:30 on the 13th in UTC+14.
    expect(
      isSameDayInAssociationTimeZone(
        new Date("2026-03-12T23:30:00Z"),
        new Date("2026-03-13T01:30:00Z"),
      ),
    ).toBe(true);
  });
});
