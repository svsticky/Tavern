import { describe, expect, it } from "vitest";
import RequiredAsterisk from "~/components/UI/RequiredAstrix";
import { render, screen } from "~/testUtils";

describe("RequiredAsterisk", () => {
  it("renders an asterisk when required is true", () => {
    render(<RequiredAsterisk required />);
    expect(screen.getByText("*")).toBeInTheDocument();
  });

  it("renders nothing when required is false", () => {
    const { container } = render(<RequiredAsterisk required={false} />);
    expect(container).toBeEmptyDOMElement();
  });
});
