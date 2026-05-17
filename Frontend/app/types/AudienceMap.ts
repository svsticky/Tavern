import type { TargetAudience } from "~/api";

/**
 * Maps numeric values to their corresponding target audience types.
 */
export const audienceMap: Record<number, TargetAudience> = {
  0: "None",
  1: "FirstYears",
  2: "SecondYears",
  4: "ThirdYearsAndAbove",
  8: "Masters",
};

/**
 * Flags for different target audiences.
 */
export const AudienceFlags = {
  FirstYears: 1,
  SecondYears: 2,
  ThirdYearsAndAbove: 4,
  Masters: 8,
  Gratie: 16,
  ActiveMembers: 32,
  All: 63,
};
