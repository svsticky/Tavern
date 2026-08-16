import { describe, expect, it } from "vitest";
import type { TargetAudience } from "~/api";
import {
  AudienceFlags,
  getAudienceString,
  parseAudience,
} from "~/types/AudienceMap";

describe("parseAudience", () => {
  it("returns 0 for undefined or null", () => {
    expect(parseAudience(undefined)).toBe(0);
    expect(parseAudience(null)).toBe(0);
  });

  it("returns the value directly when given a number", () => {
    expect(parseAudience(5)).toBe(5);
  });

  it("resolves the 'All' and 'None' shortcuts", () => {
    expect(parseAudience("All")).toBe(AudienceFlags.All);
    expect(parseAudience("None")).toBe(0);
  });

  it("parses a single flag name", () => {
    expect(parseAudience("Masters")).toBe(AudienceFlags.Masters);
  });

  it("parses a comma-separated list of flag names, trimming whitespace", () => {
    // Combined flag strings like this are valid runtime inputs (see getAudienceString below,
    // which produces exactly this shape) even though they aren't individual TargetAudience
    // enum members.
    expect(parseAudience("FirstYears, SecondYears" as TargetAudience)).toBe(
      AudienceFlags.FirstYears | AudienceFlags.SecondYears,
    );
  });

  it("ignores unknown flag names", () => {
    expect(parseAudience("NotARealFlag" as TargetAudience)).toBe(0);
  });
});

describe("getAudienceString", () => {
  it("returns 'All' for the All mask and 'None' for 0", () => {
    expect(getAudienceString(AudienceFlags.All)).toBe("All");
    expect(getAudienceString(0)).toBe("None");
  });

  it("returns a comma-joined list of active flags for a partial mask", () => {
    const mask = AudienceFlags.FirstYears | AudienceFlags.Masters;
    expect(getAudienceString(mask)).toBe("FirstYears, Masters");
  });

  it("round-trips through parseAudience", () => {
    const original = "SecondYears, Gratie, Begunstigers" as TargetAudience;
    const mask = parseAudience(original);
    expect(getAudienceString(mask)).toBe(original);
  });
});
