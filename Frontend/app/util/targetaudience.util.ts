import type { MemberResponseDto } from "~/api";
import { AudienceFlags } from "~/types/AudienceMap";

/**
 * Determines if a member is in a specific target audience.
 * @param member The member to check.
 * @param targetAudience The target audience criteria.
 * @returns True if the member is in the target audience, false otherwise.
 */
export const isMemberInTargetAudience = (
  member: MemberResponseDto | null,
  targetAudience: any,
): boolean => {
  if (!member || !member.studyEnrollments) return false;

  const audienceMask =
    typeof targetAudience === "number"
      ? targetAudience
      : AudienceFlags[targetAudience as keyof typeof AudienceFlags] || 0;

  if (audienceMask === 0) return false;
  if (audienceMask === AudienceFlags.All) return true;

  const now = new Date();
  const yearsAgo = (n: number) => {
    const d = new Date();
    d.setFullYear(now.getFullYear() - n);
    return d;
  };

  const activeEnrollments = member.studyEnrollments.filter(
    (se) => se.status === "Enrolled",
  );

  // 1. First Years
  if (audienceMask & AudienceFlags.FirstYears) {
    if (
      activeEnrollments.some(
        (se) =>
          se.studyType === "Bachelor" &&
          (se.completionDate == null ||
            new Date(se.completionDate) > new Date(Date.now())) &&
          new Date(se.enrollmentDate) >= yearsAgo(1),
      )
    )
      return true;
  }

  // 2. Second Years
  if (audienceMask & AudienceFlags.SecondYears) {
    if (
      activeEnrollments.some(
        (se) =>
          se.studyType === "Bachelor" &&
          (se.completionDate == null ||
            new Date(se.completionDate) > new Date(Date.now())) &&
          new Date(se.enrollmentDate) >= yearsAgo(2) &&
          new Date(se.enrollmentDate) < yearsAgo(1),
      )
    )
      return true;
  }

  // 3. Third Years+
  if (audienceMask & AudienceFlags.ThirdYearsAndAbove) {
    if (
      activeEnrollments.some(
        (se) =>
          se.studyType === "Bachelor" &&
          (se.completionDate == null ||
            new Date(se.completionDate) > new Date(Date.now())) &&
          new Date(se.enrollmentDate) < yearsAgo(2),
      )
    )
      return true;
  }

  // 4. Masters
  if (audienceMask & AudienceFlags.Masters) {
    if (
      activeEnrollments.some(
        (se) =>
          se.studyType === "Master" &&
          (se.completionDate == null ||
            new Date(se.completionDate) > new Date(Date.now())),
      )
    )
      return true;
  }

  // 5. Gratie
  if (audienceMask & AudienceFlags.Gratie) {
    if (member.gratie) return true;
  }

  return false;
};
