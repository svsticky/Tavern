import type { TargetAudience } from "~/api";

export const audienceMap: Record<number, TargetAudience> = {
    0: 'None',
    1: 'FirstYears',
    2: 'SecondYears',
    4: 'ThirdYearsAndAbove',
    8: 'Masters'
};

export const AudienceFlags = {
  FirstYears: 1,
  SecondYears: 2,
  ThirdYearsAndAbove: 4,
  Masters: 8,
  All: 15
};