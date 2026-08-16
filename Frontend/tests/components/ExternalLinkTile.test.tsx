import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import ExternalLinkTile from "~/components/ExternalLinkTile";

describe("ExternalLinkTile", () => {
  it("renders the title, description and link attributes", () => {
    render(
      <ExternalLinkTile
        title="Koala"
        description="Membership system"
        url="https://koala.example.com"
        icon={<span>icon</span>}
      />,
    );

    expect(screen.getByText("Koala")).toBeInTheDocument();
    expect(screen.getByText("Membership system")).toBeInTheDocument();
    const link = screen.getByRole("link");
    expect(link).toHaveAttribute("href", "https://koala.example.com");
    expect(link).toHaveAttribute("target", "_blank");
    expect(link).toHaveAttribute("rel", "noopener noreferrer");
  });

  it("renders the provided icon", () => {
    render(
      <ExternalLinkTile
        title="Koala"
        description="Membership system"
        url="https://koala.example.com"
        icon={<span data-testid="custom-icon">icon</span>}
      />,
    );
    expect(screen.getByTestId("custom-icon")).toBeInTheDocument();
  });
});
