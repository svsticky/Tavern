import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NavBarLayout from "~/layout/navbar";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getMembersByIdProfilePicture } = vi.hoisted(() => ({
  getMembersByIdProfilePicture: vi.fn(),
}));

vi.mock("~/api", () => ({ getMembersByIdProfilePicture }));

const boardToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Board",
  family_name: "Member",
  name: "Board Member",
  is_admin: true,
};

const regularToken: TokenParsed = { ...boardToken, is_admin: false };

function renderLayout(authService: ReturnType<typeof createMockAuthService>) {
  return renderWithProviders(
    <Routes>
      <Route element={<NavBarLayout />}>
        <Route index element={<div>Page content</div>} />
      </Route>
    </Routes>,
    { authService },
  );
}

describe("NavBarLayout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 404,
      data: undefined,
    });
  });

  it("renders the standard nav items and the outlet content", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => regularToken),
    });

    renderLayout(authService);

    expect(screen.getByText("dashboard")).toBeInTheDocument();
    expect(screen.getByText("activities")).toBeInTheDocument();
    expect(screen.getByText("announcements")).toBeInTheDocument();
    expect(screen.getByText("external_links")).toBeInTheDocument();
    expect(screen.getByText("Page content")).toBeInTheDocument();
  });

  it("does not show admin links in the profile dropdown for a regular member", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => regularToken),
    });

    renderLayout(authService);

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    const header = document.querySelector("header:not(.hidden)")!;
    await userEvent.click(
      within(header as HTMLElement)
        .getByAltText(/avatar/i)
        .closest("button")!,
    );
    expect(screen.queryByText("finances")).not.toBeInTheDocument();
    expect(screen.getByText("account")).toBeInTheDocument();
  });

  it("shows admin links in the profile dropdown for a board member", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => boardToken),
    });

    renderLayout(authService);

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    const header = document.querySelector("header:not(.hidden)")!;
    await userEvent.click(
      within(header as HTMLElement)
        .getByAltText(/avatar/i)
        .closest("button")!,
    );
    expect(screen.getByText("finances")).toBeInTheDocument();
    expect(screen.getByText("koala_settings")).toBeInTheDocument();
  });

  it("logs an error when the token fails to parse", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });

    renderLayout(authService);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows the fetched profile picture as a blob URL", async () => {
    const authService = createMockAuthService({
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => regularToken),
    });
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 200,
      data: new Blob(["fake-image"], { type: "image/png" }),
    });
    const createObjectURLSpy = vi
      .spyOn(URL, "createObjectURL")
      .mockReturnValue("blob:avatar-url");
    const revokeObjectURLSpy = vi
      .spyOn(URL, "revokeObjectURL")
      .mockImplementation(() => {});

    const { unmount } = renderLayout(authService);

    await waitFor(() => expect(createObjectURLSpy).toHaveBeenCalled());
    unmount();
    expect(revokeObjectURLSpy).toHaveBeenCalledWith("blob:avatar-url");
    createObjectURLSpy.mockRestore();
    revokeObjectURLSpy.mockRestore();
  });

  it("logs an error when the profile picture request fails", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => regularToken),
    });
    getMembersByIdProfilePicture.mockRejectedValue(new Error("boom"));

    renderLayout(authService);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("falls back to the default avatar when the profile picture request 404s", async () => {
    const authService = createMockAuthService({
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => regularToken),
    });
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 404,
      data: undefined,
    });

    renderLayout(authService);

    await waitFor(() =>
      expect(getMembersByIdProfilePicture).toHaveBeenCalledWith({
        path: { id: regularToken.UserId },
        responseType: "blob",
      }),
    );
  });
});
