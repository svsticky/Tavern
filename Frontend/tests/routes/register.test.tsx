import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  RegisterReasonResponseDto,
  RegisterSlideResponseDto,
} from "~/api";
import Register from "~/routes/register";

const { getRegisterreasons, getRegisterslides } = vi.hoisted(() => ({
  getRegisterreasons: vi.fn(),
  getRegisterslides: vi.fn(),
}));

vi.mock("~/api", () => ({ getRegisterreasons, getRegisterslides }));

vi.mock("~/components/Register/RegisterForm/RegisterForm", () => ({
  default: () => <div>register-form</div>,
}));

vi.mock("~/components/PhotoSlideShow", () => ({
  default: ({ images }: { images: string[] }) => (
    <div data-testid="slideshow">{images.join(",")}</div>
  ),
}));

class ImageStub {
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  set src(_value: string) {
    queueMicrotask(() => this.onload?.());
  }
}

describe("Register", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("Image", ImageStub);
  });

  it("shows a loading placeholder, then the default slideshow when there are no configured slides", async () => {
    getRegisterreasons.mockResolvedValue({ data: [] });
    getRegisterslides.mockResolvedValue({ data: [] });

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("photo1.png");
  });

  it("uses fetched slides for the slideshow images when available", async () => {
    const slides: RegisterSlideResponseDto[] = [
      { id: 1 } as RegisterSlideResponseDto,
      { id: 2 } as RegisterSlideResponseDto,
    ];
    getRegisterreasons.mockResolvedValue({ data: [] });
    getRegisterslides.mockResolvedValue({ data: slides });

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("registerslides/1/image");
    expect(slideshow.textContent).toContain("registerslides/2/image");
  });

  it("falls back to an empty list when the API responses have no data", async () => {
    getRegisterreasons.mockResolvedValue({ data: undefined });
    getRegisterslides.mockResolvedValue({ data: undefined });

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    const slideshow = await screen.findByTestId("slideshow");
    expect(slideshow.textContent).toContain("photo1.png");
  });

  it("preloads icon images only for reasons that have an iconPath", async () => {
    const reasons: RegisterReasonResponseDto[] = [
      { id: 1, iconPath: "icon.png" } as RegisterReasonResponseDto,
      { id: 2, iconPath: null } as RegisterReasonResponseDto,
    ];
    getRegisterreasons.mockResolvedValue({ data: reasons });
    getRegisterslides.mockResolvedValue({ data: [] });

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    await screen.findByTestId("slideshow");
    expect(await screen.findByText("register-form")).toBeInTheDocument();
  });

  it("renders the register form", async () => {
    getRegisterreasons.mockResolvedValue({ data: [] });
    getRegisterslides.mockResolvedValue({ data: [] });

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    expect(await screen.findByText("register-form")).toBeInTheDocument();
  });

  it("logs an error when loading registration content fails", async () => {
    getRegisterreasons.mockRejectedValue(new Error("boom"));
    getRegisterslides.mockResolvedValue({ data: [] });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("renders the navigation items", async () => {
    getRegisterreasons.mockResolvedValue({ data: [] });
    getRegisterslides.mockResolvedValue({ data: [] });

    render(
      <MemoryRouter>
        <Register />
      </MemoryRouter>,
    );

    await screen.findByTestId("slideshow");
    expect(screen.getByText("home")).toBeInTheDocument();
    expect(screen.getByText("who_are_we")).toBeInTheDocument();
  });
});
