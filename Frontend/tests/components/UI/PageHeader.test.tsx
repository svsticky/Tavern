import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { PageHeader } from "~/components/UI/PageHeader";
import { renderWithProviders, screen } from "~/testUtils";

describe("PageHeader", () => {
  it("renders the title", () => {
    renderWithProviders(<PageHeader title="Members" />);
    expect(
      screen.getByRole("heading", { name: "Members" }),
    ).toBeInTheDocument();
  });

  it("does not render a back button when neither backTo nor onBack is provided", () => {
    renderWithProviders(<PageHeader title="Members" />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  });

  it("renders a clickable back button when onBack is provided", async () => {
    const onBack = vi.fn();
    renderWithProviders(<PageHeader title="Members" onBack={onBack} />);

    await userEvent.click(screen.getByRole("button", { name: "back" }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it("renders a navigation link when backTo is provided", () => {
    renderWithProviders(<PageHeader title="Members" backTo="/home" />);

    const link = screen.getByRole("link", { name: "back" });
    expect(link).toHaveAttribute("href", "/home");
  });

  it("renders the action content on the right side", () => {
    renderWithProviders(
      <PageHeader
        title="Members"
        action={<button type="button">New</button>}
      />,
    );
    expect(screen.getByRole("button", { name: "New" })).toBeInTheDocument();
  });
});
