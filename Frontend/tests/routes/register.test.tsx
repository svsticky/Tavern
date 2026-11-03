import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  RegisterReasonResponseDto,
  RegisterSlideResponseDto,
} from "~/api";
import { loadRegisterFormData } from "~/components/Register/RegisterForm/RegisterForm.handlers";
import * as route from "~/routes/register";
import { renderRoute } from "~/testUtils";

const { getRegisterreasons, getRegisterslides } = vi.hoisted(() => ({
  getRegisterreasons: vi.fn(),
  getRegisterslides: vi.fn(),
}));

vi.mock("~/api", () => ({ getRegisterreasons, getRegisterslides }));

vi.mock("~/components/Register/RegisterForm/RegisterForm.handlers", () => ({
  loadRegisterFormData: vi.fn(),
}));

vi.mock("~/components/Register/RegisterForm/RegisterForm", () => ({
  default: ({ data }: { data: { studies: unknown[] } }) => (
    <div>register-form-studies-{data.studies.length}</div>
  ),
}));

vi.mock("~/components/Register/RegisterReasons", () => ({
  default: ({ reasons }: { reasons: unknown[] }) => (
    <div>register-reasons-{reasons.length}</div>
  ),
}));

vi.mock("~/components/PhotoSlideShow", () => ({
  default: ({ images }: { images: string[] }) => (
    <div data-testid="slideshow">{images.join(",")}</div>
  ),
}));

/** The URLs of all images the loader preloaded. */
let preloaded: string[] = [];

class ImageStub {
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  set src(value: string) {
    preloaded.push(value);
    queueMicrotask(() => this.onload?.());
  }
}

describe("Register", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    preloaded = [];
    vi.stubGlobal("Image", ImageStub);
    getRegisterreasons.mockResolvedValue({ data: [] });
    getRegisterslides.mockResolvedValue({ data: [] });
    vi.mocked(loadRegisterFormData).mockResolvedValue({
      studies: [{ id: 1 }],
    } as never);
  });

  it("shows the default slideshow when there are no configured slides", async () => {
    renderRoute(route);

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("photo1.png");
  });

  it("uses the configured slides for the slideshow images when available", async () => {
    getRegisterslides.mockResolvedValue({
      data: [{ id: 1 }, { id: 2 }] as RegisterSlideResponseDto[],
    });
    renderRoute(route);

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("registerslides/1/image");
    expect(slideshow.textContent).toContain("registerslides/2/image");
    expect(slideshow.textContent).not.toContain("photo1.png");
  });

  it("falls back to the defaults when the API responses have no data", async () => {
    getRegisterreasons.mockResolvedValue({ data: undefined });
    getRegisterslides.mockResolvedValue({ data: undefined });
    renderRoute(route);

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("photo1.png");
    expect(screen.getByText("register-reasons-0")).toBeInTheDocument();
  });

  it("still renders the page when the reasons or slides fail to load", async () => {
    getRegisterreasons.mockRejectedValue(new Error("boom"));
    getRegisterslides.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    renderRoute(route);

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("photo1.png");
    expect(consoleError).toHaveBeenCalledTimes(2);
    consoleError.mockRestore();
  });

  it("preloads the slides and only the icons of reasons that have one", async () => {
    getRegisterreasons.mockResolvedValue({
      data: [
        { id: 1, iconPath: "icon.png" },
        { id: 2, iconPath: null },
      ] as RegisterReasonResponseDto[],
    });
    renderRoute(route);

    await screen.findByTestId("slideshow");
    expect(preloaded).toContain("/register_slide_photos/photo1.png");
    expect(
      preloaded.some((src) => src.endsWith("/registerreasons/1/icon")),
    ).toBe(true);
    expect(
      preloaded.some((src) => src.endsWith("/registerreasons/2/icon")),
    ).toBe(false);
    expect(screen.getByText("register-reasons-2")).toBeInTheDocument();
  });

  it("renders the register form with the loaded form data", async () => {
    renderRoute(route);

    expect(
      await screen.findByText("register-form-studies-1"),
    ).toBeInTheDocument();
  });

  it("renders the navigation items", async () => {
    renderRoute(route);

    await screen.findByTestId("slideshow");
    expect(screen.getByText("home")).toBeInTheDocument();
    expect(screen.getByText("who_are_we")).toBeInTheDocument();
  });
});
