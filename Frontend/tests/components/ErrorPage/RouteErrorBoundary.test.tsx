import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import RouteErrorBoundary, {
  getErrorStatus,
} from "~/components/ErrorPage/RouteErrorBoundary";

const renderWithError = (thrown: unknown) => {
  const router = createMemoryRouter(
    [
      {
        path: "/",
        loader: () => {
          throw thrown;
        },
        element: <div>Never rendered</div>,
        ErrorBoundary: RouteErrorBoundary,
      },
    ],
    { initialEntries: ["/"] },
  );
  return render(<RouterProvider router={router} />);
};

describe("getErrorStatus", () => {
  it("reads the status of a router error response", () => {
    expect(getErrorStatus(new Response(null, { status: 404 }))).toBe(404);
    expect(
      getErrorStatus({ status: 404, statusText: "", internal: true, data: "" }),
    ).toBe(404);
  });

  it("reads the status of an axios error", () => {
    expect(getErrorStatus({ response: { status: 500 } })).toBe(500);
  });

  it("reads the status of a plain problem details body", () => {
    expect(getErrorStatus({ status: 403, title: "Forbidden" })).toBe(403);
  });

  it("returns undefined when there is no numeric status", () => {
    expect(getErrorStatus(new Error("boom"))).toBeUndefined();
    expect(getErrorStatus("boom")).toBeUndefined();
    expect(getErrorStatus(null)).toBeUndefined();
    expect(getErrorStatus({ status: "500" })).toBeUndefined();
  });
});

describe("RouteErrorBoundary", () => {
  afterEach(() => vi.restoreAllMocks());

  const silenceRouterLogging = () =>
    vi.spyOn(console, "error").mockImplementation(() => {});

  it("shows the not found page for a 404", async () => {
    silenceRouterLogging();
    renderWithError(new Response("", { status: 404 }));

    expect(
      await screen.findByRole("heading", { name: "not_found_headline" }),
    ).toBeInTheDocument();
    expect(screen.getByText("404")).toBeInTheDocument();
    expect(screen.queryByText("try_again")).not.toBeInTheDocument();
  });

  it("shows the forbidden page for a 403 thrown by the API client", async () => {
    silenceRouterLogging();
    renderWithError({ response: { status: 403 } });

    expect(
      await screen.findByRole("heading", { name: "forbidden_headline" }),
    ).toBeInTheDocument();
    expect(screen.getByText("403")).toBeInTheDocument();
  });

  it("shows the generic error page with the status code for other failures", async () => {
    silenceRouterLogging();
    renderWithError({ status: 500 });

    expect(
      await screen.findByRole("heading", { name: "error_headline" }),
    ).toBeInTheDocument();
    expect(screen.getByText("500")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "back_to_home" })).toHaveAttribute(
      "href",
      "/",
    );
  });

  it("falls back to a friendly code when the status is unknown", async () => {
    silenceRouterLogging();
    renderWithError(new Error("boom"));

    expect(await screen.findByText("error_code_fallback")).toBeInTheDocument();
  });

  it("reloads the page when trying again", async () => {
    silenceRouterLogging();
    const reload = vi.fn();
    const originalLocation = window.location;
    Object.defineProperty(window, "location", {
      configurable: true,
      value: { ...originalLocation, reload },
    });
    renderWithError(new Error("boom"));

    await userEvent.click(
      await screen.findByRole("button", { name: "try_again" }),
    );

    expect(reload).toHaveBeenCalledTimes(1);
    Object.defineProperty(window, "location", {
      configurable: true,
      value: originalLocation,
    });
  });

  it("shows the technical details of an Error in development", async () => {
    silenceRouterLogging();
    renderWithError(new Error("kaboom"));

    expect(
      await screen.findByText("error_technical_details"),
    ).toBeInTheDocument();
    expect(screen.getByText(/kaboom/)).toBeInTheDocument();
  });

  it("stringifies non-Error values in the technical details", async () => {
    silenceRouterLogging();
    renderWithError({ title: "Bad thing" });
    expect(await screen.findByText(/Bad thing/)).toBeInTheDocument();
  });

  it("shows a thrown string as is in the technical details", async () => {
    silenceRouterLogging();
    renderWithError("just a string");
    expect(await screen.findByText("just a string")).toBeInTheDocument();
  });

  it("copes with values that cannot be serialized", async () => {
    silenceRouterLogging();
    const circular: Record<string, unknown> = {};
    circular.self = circular;
    renderWithError(circular);
    expect(await screen.findByText("[object Object]")).toBeInTheDocument();
  });

  it("hides the technical details outside development", async () => {
    silenceRouterLogging();
    vi.stubEnv("DEV", false);
    renderWithError(new Error("kaboom"));

    await screen.findByRole("heading", { name: "error_headline" });
    expect(
      screen.queryByText("error_technical_details"),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/kaboom/)).not.toBeInTheDocument();
    vi.unstubAllEnvs();
  });
});
