import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  formatDate,
  formatDateOnly,
  formatForInput,
  getCommitteeYear,
  getFinancialYear,
  getGlobalCommitteeCreationDate,
  getGlobalFinancialYearStartDate,
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
  it("formats an ISO string as YYYY-MM-DDTHH:mm in local time", () => {
    const isoString = "2026-03-05T14:30:00.000Z";
    const expected = (() => {
      const d = new Date(isoString);
      const pad = (n: number) => String(n).padStart(2, "0");
      return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    })();
    expect(formatForInput(isoString)).toBe(expected);
  });

  it("returns an empty string for undefined input", () => {
    expect(formatForInput(undefined)).toBe("");
  });

  it("returns an empty string for an invalid date string", () => {
    expect(formatForInput("not-a-date")).toBe("");
  });
});

describe("formatDateOnly", () => {
  it("formats an ISO string as YYYY-MM-DD in local time", () => {
    const isoString = "2026-03-05T14:30:00.000Z";
    const d = new Date(isoString);
    const pad = (n: number) => String(n).padStart(2, "0");
    const expected = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
    expect(formatDateOnly(isoString)).toBe(expected);
  });

  it("returns an empty string for undefined or invalid input", () => {
    expect(formatDateOnly(undefined)).toBe("");
    expect(formatDateOnly("not-a-date")).toBe("");
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
