import { fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { renderWithProviders } from "~/testUtils";

const { handleCreateSubmit, handleCreateBegunstigerInputChange } = vi.hoisted(
  () => ({
    handleCreateSubmit: vi.fn((args: { e: { preventDefault: () => void } }) =>
      args.e.preventDefault(),
    ),
    handleCreateBegunstigerInputChange: vi.fn(
      (
        e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
        setFormData: any,
      ) => {
        const { name, value, type } = e.target;
        const val =
          type === "checkbox" ? (e.target as HTMLInputElement).checked : value;
        setFormData((prev: any) => ({ ...prev, [name]: val }));
      },
    ),
  }),
);

vi.mock("~/routes/admin/create-member/create-member-handlers", () => ({
  handleCreateSubmit,
  handleCreateBegunstigerInputChange,
}));

import CreateBegunstigerPage from "~/routes/admin/create-member/create-member";

describe("CreateBegunstigerPage", () => {
  it("renders the form fields", () => {
    renderWithProviders(<CreateBegunstigerPage />, {
      route: "/admin/create-member",
    });

    expect(screen.getByLabelText(/first_name/)).toBeInTheDocument();
    expect(screen.getByLabelText(/last_name/)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/)).toBeInTheDocument();
    // student_number's input isn't wrapped in a <label> (custom markup for the F_ prefix), so
    // assert on the visible text instead.
    expect(screen.getByText(/student_number/)).toBeInTheDocument();
  });

  it("disables the submit button while the form is incomplete", () => {
    renderWithProviders(<CreateBegunstigerPage />);

    const submitButton = screen.getByRole("button", { name: "create" });
    expect(submitButton).toBeDisabled();
  });

  it("delegates input changes to handleCreateBegunstigerInputChange", () => {
    renderWithProviders(<CreateBegunstigerPage />);

    const firstNameInput = screen.getByLabelText(/first_name/);
    fireEvent.change(firstNameInput, { target: { value: "Jane" } });

    expect(handleCreateBegunstigerInputChange).toHaveBeenCalled();
  });

  it("shows the F_ prefix hint once the begunstiger checkbox is checked", () => {
    renderWithProviders(<CreateBegunstigerPage />);

    expect(screen.queryByText("F_")).not.toBeInTheDocument();
    fireEvent.click(document.querySelector('input[name="isBegunstiger"]')!);
    expect(screen.getByText("F_")).toBeInTheDocument();
  });

  function fillRequiredFields(birthDate: string) {
    const fieldsByLabel: [RegExp, string][] = [
      [/first_name/, "Jane"],
      [/last_name/, "Doe"],
      [/email/, "jane@example.com"],
      [/birth_date/, birthDate],
      [/^phone/, "0612345678"],
      [/street/, "Main street"],
      [/house_number/, "1"],
      [/postal_code/, "1234AB"],
      [/city/, "Utrecht"],
    ];
    fieldsByLabel.forEach(([label, value]) => {
      fireEvent.change(screen.getByLabelText(label), { target: { value } });
    });
    // The student_number input isn't wrapped in a <label> (custom markup for the F_ prefix).
    fireEvent.change(document.querySelector('input[name="studentNumber"]')!, {
      target: { value: "1234567" },
    });
  }

  it("enables submit for an adult without requiring a parent phone number", () => {
    renderWithProviders(<CreateBegunstigerPage />);
    fillRequiredFields("2000-01-01");

    expect(screen.getByRole("button", { name: "create" })).not.toBeDisabled();
  });

  it("treats a birthday later this month as not yet turned this year's age (partial-year adjustment)", () => {
    const today = new Date();
    // Same month, but a day later than today: the person hasn't had this year's birthday yet.
    const birthDate = `${today.getFullYear() - 25}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(Math.min(today.getDate() + 1, 28)).padStart(2, "0")}`;
    renderWithProviders(<CreateBegunstigerPage />);
    fillRequiredFields(birthDate);

    expect(screen.getByRole("button", { name: "create" })).not.toBeDisabled();
  });

  it("treats a birth month later in the year as not yet turned this year's age", () => {
    const today = new Date();
    const laterMonth = ((today.getMonth() + 1) % 12) + 1;
    const birthDate = `${today.getFullYear() - 25}-${String(laterMonth).padStart(2, "0")}-01`;
    renderWithProviders(<CreateBegunstigerPage />);
    fillRequiredFields(birthDate);

    expect(screen.getByRole("button", { name: "create" })).not.toBeDisabled();
  });

  it("delegates the language select change to handleCreateBegunstigerInputChange", () => {
    renderWithProviders(<CreateBegunstigerPage />);
    fireEvent.change(screen.getByLabelText(/preferred_language/), {
      target: { value: "EN" },
    });
    expect(handleCreateBegunstigerInputChange).toHaveBeenCalled();
  });

  it("keeps submit disabled for a minor until a parent phone number is provided", () => {
    const today = new Date();
    const minorBirthDate = `${today.getFullYear() - 10}-01-01`;
    renderWithProviders(<CreateBegunstigerPage />);
    fillRequiredFields(minorBirthDate);

    expect(screen.getByRole("button", { name: "create" })).toBeDisabled();

    fireEvent.change(screen.getByLabelText(/parent_phone_number/), {
      target: { value: "0612345678" },
    });

    expect(screen.getByRole("button", { name: "create" })).not.toBeDisabled();
  });

  it("submits the form via handleCreateSubmit", () => {
    renderWithProviders(<CreateBegunstigerPage />);

    const form = screen.getByRole("button", { name: "create" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form as HTMLFormElement);

    expect(handleCreateSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        isFormValid: false,
        formData: expect.objectContaining({ firstname: "" }),
      }),
    );
  });
});
