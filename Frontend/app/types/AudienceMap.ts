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

/**
 * Converts a TargetAudience value (string, number, list) into a numeric bitmask.
 */
export const parseAudience = (audience?: TargetAudience | number | null): number => {
  if (audience === undefined || audience === null) return 0;
  if (typeof audience === "number") return audience;

  if (audience === "All") return AudienceFlags.All;
  if (audience === "None") return 0;

  return audience
    .split(",")
    .map((s) => s.trim())
    .reduce((acc, flagName) => {
      const flagVal = AudienceFlags[flagName as keyof typeof AudienceFlags];
      return acc | (flagVal || 0);
    }, 0);
};

/**
 * Converts a numeric bitmask back to a TargetAudience string representation.
 */
export const getAudienceString = (mask: number): TargetAudience => {
  if (mask === AudienceFlags.All) return "All";
  if (mask === 0) return "None";

  const activeFlags: string[] = [];
  if (mask & AudienceFlags.FirstYears) activeFlags.push("FirstYears");
  if (mask & AudienceFlags.SecondYears) activeFlags.push("SecondYears");
  if (mask & AudienceFlags.ThirdYearsAndAbove) activeFlags.push("ThirdYearsAndAbove");
  if (mask & AudienceFlags.Masters) activeFlags.push("Masters");
  if (mask & AudienceFlags.Gratie) activeFlags.push("Gratie");
  if (mask & AudienceFlags.ActiveMembers) activeFlags.push("ActiveMembers");

  return activeFlags.join(", ") as TargetAudience;
};

