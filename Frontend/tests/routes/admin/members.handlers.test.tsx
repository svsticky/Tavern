import { describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import type { MembersFilterDto } from "~/types/MembersFilterDto";

const { getMembers } = vi.hoisted(() => ({
  getMembers: vi.fn(),
}));

vi.mock("~/api", () => ({ getMembers }));

import { fetchMembersPage } from "~/routes/admin/members.handlers";

describe("fetchMembersPage", () => {
  it("fetches members for the given page/search with no filters", async () => {
    const members: MemberResponseDto[] = [
      { id: "1", firstName: "Jane" } as MemberResponseDto,
    ];
    getMembers.mockResolvedValue({ data: members });

    const result = await fetchMembersPage(2, "jane", null);

    expect(getMembers).toHaveBeenCalledWith({
      query: {
        Page: 2,
        PageSize: 20,
        Search: "jane",
        StudyId: undefined,
        Gratie: undefined,
        LidVanVerdienste: undefined,
        EreLid: undefined,
        Begunstiger: undefined,
        Suspended: undefined,
        Inactive: undefined,
        StudyType: undefined,
      },
    });
    expect(result).toEqual(members);
  });

  it("forwards filter fields when present", async () => {
    getMembers.mockResolvedValue({ data: [] });
    const filters: MembersFilterDto = {
      studyId: 5,
      gratie: true,
      lidVanVerdienste: null,
      ereLid: null,
      begunstiger: null,
      suspended: true,
      inactive: null,
      studyType: null,
    };

    await fetchMembersPage(1, "", filters);

    expect(getMembers).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ StudyId: 5, Suspended: true }),
      }),
    );
  });

  it("throws the response error when present", async () => {
    getMembers.mockResolvedValue({ error: "bad", data: null });

    await expect(fetchMembersPage(1, "", null)).rejects.toBe("bad");
  });

  it("throws a generic error when there is no data and no error", async () => {
    getMembers.mockResolvedValue({ error: null, data: null });

    await expect(fetchMembersPage(1, "", null)).rejects.toThrow(
      "Failed to fetch members",
    );
  });
});
