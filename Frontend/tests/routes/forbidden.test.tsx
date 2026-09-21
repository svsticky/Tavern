import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import ForbiddenPage from "~/routes/forbidden";

describe("ForbiddenPage", () => {
  it("shows the 403 error page", () => {
    render(
      <MemoryRouter>
        <ForbiddenPage />
      </MemoryRouter>,
    );
    expect(screen.getByText("403")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "forbidden_headline" }),
    ).toBeInTheDocument();
    expect(screen.getByText("forbidden_description")).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "back_to_home" }),
    ).toBeInTheDocument();
  });
});
