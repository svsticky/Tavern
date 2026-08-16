import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import InfoItem from "~/components/Activity/ActivityDetailsTile/InfoItem";

describe("InfoItem", () => {
  it("renders the icon, label, and value", () => {
    render(
      <InfoItem
        icon={<span data-testid="icon" />}
        label="Location"
        value="Enschede"
      />,
    );

    expect(screen.getByTestId("icon")).toBeInTheDocument();
    expect(screen.getByText("Location")).toBeInTheDocument();
    expect(screen.getByText("Enschede")).toBeInTheDocument();
  });
});
