import { describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import { handleEditClick } from "~/components/Activity/ActivityTile/ActivityTile.handlers";

describe("handleEditClick", () => {
  it("prevents default, stops propagation, and navigates to the edit page", () => {
    const preventDefault = vi.fn();
    const stopPropagation = vi.fn();
    const navigate = vi.fn();
    const activity = { id: 42 } as ActivityResponseDto;

    handleEditClick(
      { preventDefault, stopPropagation } as unknown as React.MouseEvent,
      navigate,
      activity,
    );

    expect(preventDefault).toHaveBeenCalled();
    expect(stopPropagation).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith("/activities/edit/42");
  });
});
