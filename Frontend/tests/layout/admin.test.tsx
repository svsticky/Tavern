import { render, screen } from "@testing-library/react";
import { data } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderRoute } from "~/testUtils";

const { requireBoardOrCandidateBoard } = vi.hoisted(() => ({
  requireBoardOrCandidateBoard: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

import * as route from "~/layout/admin";

describe("AdminLayout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("only lets board and candidate board members through", async () => {
    requireBoardOrCandidateBoard.mockResolvedValue({});

    await expect(route.clientLoader()).resolves.toBeNull();
    expect(requireBoardOrCandidateBoard).toHaveBeenCalledTimes(1);
  });

  it("shows the forbidden page for everyone else", async () => {
    requireBoardOrCandidateBoard.mockRejectedValue(
      data("Forbidden", { status: 403 }),
    );

    renderRoute(route, { path: "/admin" });

    expect(await screen.findByRole("alert")).toHaveTextContent("403");
  });

  it("shows the loading koala instead of a loading text while checking access", () => {
    render(<route.HydrateFallback />);

    expect(screen.getByRole("img", { name: "Loading" })).toBeInTheDocument();
    expect(screen.queryByText("loading")).not.toBeInTheDocument();
  });
});
