import type { TargetAudience } from "~/api";

export const audienceMap: Record<number, TargetAudience> = {
    0: 'None',
    1: 'FirstYears',
    2: 'SecondYears',
    4: 'ThirdYearsAndAbove',
    8: 'Masters'
};