import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import ErrorPage from "~/components/ErrorPage/ErrorPage";

const renderPage = (mood: "lost" | "forbidden" = "lost") =>
  render(
    <MemoryRouter>
      <ErrorPage
        code="418"
        headline="Short and stout"
        description="I am a teapot."
        mood={mood}
      />
    </MemoryRouter>,
  );

describe("ErrorPage", () => {
  it("shows the code, headline and description", () => {
    renderPage();
    expect(screen.getByText("418")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Short and stout" }),
    ).toBeInTheDocument();
    expect(screen.getByText("I am a teapot.")).toBeInTheDocument();
  });

  it("links back to the home page", () => {
    renderPage();
    expect(screen.getByRole("link", { name: "back_to_home" })).toHaveAttribute(
      "href",
      "/",
    );
  });

  it("passes the mood on to the koala", () => {
    const { container } = renderPage("forbidden");
    expect(container.querySelector("circle")).toBeInTheDocument();
  });
});
