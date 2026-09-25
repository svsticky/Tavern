import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router";
import { describe, expect, it } from "vitest";
import {
  BackNavigationProvider,
  useBackNavigationTarget,
} from "~/context/BackNavigationContext";

function TargetDisplay() {
  const target = useBackNavigationTarget();
  return <div data-testid="target">{target ?? "none"}</div>;
}

function Page({ to }: { to: string }) {
  const navigate = useNavigate();
  return (
    <>
      <button type="button" onClick={() => navigate(to)}>
        navigate
      </button>
      <TargetDisplay />
    </>
  );
}

function Harness({ initialEntries }: { initialEntries: string[] }) {
  return (
    <MemoryRouter initialEntries={initialEntries}>
      <BackNavigationProvider>
        <Routes>
          <Route path="/" element={<Page to="/activities" />} />
          <Route path="/activities" element={<Page to="/admin/members" />} />
          <Route
            path="/admin/members"
            element={<Page to="/admin/members/create-member" />}
          />
          <Route
            path="/admin/members/create-member"
            element={<Page to="/admin/members/123" />}
          />
          <Route path="/admin/members/:id" element={<Page to="/" />} />
        </Routes>
      </BackNavigationProvider>
    </MemoryRouter>
  );
}

describe("BackNavigationProvider", () => {
  it("exposes the previous page once you've navigated within the app", async () => {
    render(<Harness initialEntries={["/"]} />);
    expect(screen.getByTestId("target")).toHaveTextContent("none");

    await userEvent.click(screen.getByRole("button", { name: "navigate" }));

    expect(screen.getByTestId("target")).toHaveTextContent("/");
  });

  it("excludes a create page from the back target, so the page after it can't accidentally re-open it", async () => {
    render(<Harness initialEntries={["/admin/members"]} />);

    // /admin/members -> /admin/members/create-member -> /admin/members/123
    await userEvent.click(screen.getByRole("button", { name: "navigate" }));
    await userEvent.click(screen.getByRole("button", { name: "navigate" }));

    // Going back from here would land on the create form - excluded.
    expect(screen.getByTestId("target")).toHaveTextContent("none");
  });

  it("does not exclude an ordinary page", async () => {
    render(<Harness initialEntries={["/"]} />);

    // / -> /activities -> /admin/members
    await userEvent.click(screen.getByRole("button", { name: "navigate" }));
    await userEvent.click(screen.getByRole("button", { name: "navigate" }));

    expect(screen.getByTestId("target")).toHaveTextContent("/activities");
  });
});
