import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import Form from "~/components/UI/Form/Form";
import { render, screen } from "~/testUtils";

describe("Form", () => {
  it("renders children inside a form element", () => {
    const { container } = render(
      <Form>
        <input aria-label="name" />
      </Form>,
    );
    expect(container.querySelector("form")).toBeInTheDocument();
    expect(screen.getByLabelText("name")).toBeInTheDocument();
  });

  it("applies the default flex-column layout classes merged with a custom className", () => {
    const { container } = render(
      <Form className="custom-form">
        <input aria-label="name" />
      </Form>,
    );
    const form = container.querySelector("form");
    expect(form).toHaveClass("flex", "flex-col", "gap-4", "custom-form");
  });

  it("submits the form when Ctrl+Enter is pressed", async () => {
    const onSubmit = vi.fn((e) => e.preventDefault());
    render(
      <Form onSubmit={onSubmit}>
        <input aria-label="name" />
      </Form>,
    );

    const input = screen.getByLabelText("name");
    input.focus();
    await userEvent.keyboard("{Control>}{Enter}{/Control}");

    expect(onSubmit).toHaveBeenCalledTimes(1);
  });

  it("submits the form when Meta+Enter is pressed", async () => {
    const onSubmit = vi.fn((e) => e.preventDefault());
    render(
      <Form onSubmit={onSubmit}>
        <input aria-label="name" />
      </Form>,
    );

    const input = screen.getByLabelText("name");
    input.focus();
    await userEvent.keyboard("{Meta>}{Enter}{/Meta}");

    expect(onSubmit).toHaveBeenCalledTimes(1);
  });

  it("does not call preventDefault or requestSubmit on a plain Enter press", async () => {
    const onKeyDown = vi.fn();
    const { container } = render(
      <Form onKeyDown={onKeyDown}>
        <input aria-label="name" />
      </Form>,
    );

    const form = container.querySelector("form") as HTMLFormElement;
    const requestSubmitSpy = vi.spyOn(form, "requestSubmit");
    const input = screen.getByLabelText("name");
    input.focus();
    await userEvent.keyboard("{Enter}");

    expect(requestSubmitSpy).not.toHaveBeenCalled();
    expect(onKeyDown).toHaveBeenCalled();
  });

  it("calls a custom onKeyDown handler in addition to the built-in behavior", async () => {
    const onKeyDown = vi.fn();
    render(
      <Form onKeyDown={onKeyDown}>
        <input aria-label="name" />
      </Form>,
    );

    const input = screen.getByLabelText("name");
    input.focus();
    await userEvent.keyboard("a");

    expect(onKeyDown).toHaveBeenCalled();
  });
});
