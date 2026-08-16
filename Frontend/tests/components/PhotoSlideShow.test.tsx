import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import PhotoSlideshow from "~/components/PhotoSlideShow";

describe("PhotoSlideshow", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders a dot button for each image", () => {
    render(<PhotoSlideshow images={["a.jpg", "b.jpg", "c.jpg"]} />);
    expect(screen.getAllByLabelText(/Go to slide/)).toHaveLength(3);
  });

  it("shows the next slide when the next button is clicked", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg"]} />,
    );
    const [prevButton, nextButton] = container.querySelectorAll("button");
    void prevButton;
    fireEvent.click(nextButton);
    expect(container.querySelector('[style*="b.jpg"]')).toBeInTheDocument();
  });

  it("wraps to the first slide when clicking next from the last slide", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg"]} />,
    );
    const [, nextButton] = container.querySelectorAll("button");
    fireEvent.click(nextButton);
    fireEvent.click(nextButton);
    expect(container.querySelector('[style*="a.jpg"]')).toBeInTheDocument();
  });

  it("wraps to the last slide when clicking previous from the first slide", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg"]} />,
    );
    const [prevButton] = container.querySelectorAll("button");
    fireEvent.click(prevButton);
    expect(container.querySelector('[style*="b.jpg"]')).toBeInTheDocument();
  });

  it("jumps to a specific slide when its dot is clicked", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg", "c.jpg"]} />,
    );
    fireEvent.click(screen.getByLabelText("Go to slide 3"));
    expect(container.querySelector('[style*="c.jpg"]')).toBeInTheDocument();
  });

  it("automatically advances slides after the autoplay interval", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg"]} autoPlayInterval={1000} />,
    );
    act(() => {
      vi.advanceTimersByTime(1000);
    });
    expect(container.querySelector('[style*="b.jpg"]')).toBeInTheDocument();
  });

  it("automatically wraps to the first slide after the last one", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg"]} autoPlayInterval={1000} />,
    );
    act(() => {
      vi.advanceTimersByTime(2000);
    });
    expect(container.querySelector('[style*="a.jpg"]')).toBeInTheDocument();
  });

  it("goes to the previous slide when not on the first one", () => {
    const { container } = render(
      <PhotoSlideshow images={["a.jpg", "b.jpg", "c.jpg"]} />,
    );
    const [prevButton, nextButton] = container.querySelectorAll("button");
    fireEvent.click(nextButton);
    fireEvent.click(nextButton);
    fireEvent.click(prevButton);
    expect(container.querySelector('[style*="b.jpg"]')).toBeInTheDocument();
  });
});
