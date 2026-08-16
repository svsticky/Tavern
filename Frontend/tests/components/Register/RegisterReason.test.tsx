import { screen } from "@testing-library/react";
import { Book } from "lucide-react";
import { describe, expect, it } from "vitest";
import RegisterReason from "~/components/Register/RegisterReason";
import { render } from "~/testUtils";

describe("RegisterReason", () => {
  it("renders the title and description", () => {
    render(<RegisterReason title="Book discounts" description="Save money" />);

    expect(screen.getByText("Book discounts")).toBeInTheDocument();
    expect(screen.getByText("Save money")).toBeInTheDocument();
  });

  it("renders the provided Lucide icon when no iconUrl is given", () => {
    const { container } = render(
      <RegisterReason title="T" description="D" icon={Book} />,
    );

    expect(container.querySelector(".lucide-book")).toBeInTheDocument();
    expect(container.querySelector("img")).not.toBeInTheDocument();
  });

  it("falls back to the UsersRound icon when neither icon nor iconUrl is given", () => {
    const { container } = render(<RegisterReason title="T" description="D" />);

    expect(container.querySelector(".lucide-users-round")).toBeInTheDocument();
  });

  it("renders an image and prefers it over the icon when iconUrl is given", () => {
    const { container } = render(
      <RegisterReason
        title="T"
        description="D"
        icon={Book}
        iconUrl="https://example.com/icon.png"
      />,
    );

    const img = container.querySelector("img");
    expect(img).toHaveAttribute("src", "https://example.com/icon.png");
    expect(container.querySelector(".lucide-book")).not.toBeInTheDocument();
  });
});
