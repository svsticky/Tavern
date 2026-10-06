import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import NotFoundPage from "~/routes/not-found";

describe("NotFoundPage", () => {
  it("shows the 404 error page", () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );
    expect(screen.getByText("404")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "not_found_headline" }),
    ).toBeInTheDocument();
    expect(screen.getByText("not_found_description")).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "back_to_home" }),
    ).toBeInTheDocument();
  });
});
