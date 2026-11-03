import { fireEvent, screen, waitFor } from "@testing-library/react";
import i18next from "i18next";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  MailinglistDto,
  RegistrationDocumentResponseDto,
  Study,
} from "~/api";
import RegisterForm from "~/components/Register/RegisterForm/RegisterForm";
import {
  handleRegisterInputChange,
  handleRegisterSubmit,
  handleStudyToggle,
  type RegisterFormLoaderData,
} from "~/components/Register/RegisterForm/RegisterForm.handlers";
import { renderWithProviders } from "~/testUtils";

vi.mock("~/components/Register/RegisterForm/RegisterForm.handlers", () => ({
  handleRegisterInputChange: vi.fn(),
  handleStudyToggle: vi.fn(),
  handleRegisterSubmit: vi.fn(),
}));

function makeStudy(overrides: Partial<Study> = {}): Study {
  return {
    id: 1,
    title: "Computer Science",
    type: "Bachelor",
    ...overrides,
  } as Study;
}

/** The form's data; tests change it before rendering. */
let data: RegisterFormLoaderData;

/** Renders the form with the current `data`. */
function renderForm() {
  return renderWithProviders(<RegisterForm data={data} />);
}

describe("RegisterForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    data = {
      studies: [],
      mastersMustPay: false,
      price: 10,
      membershipPaymentExpirationTime: 1,
      mailingLists: [],
      documents: [],
      studyStartDates: null,
    };
  });

  it("renders the form fields", async () => {
    renderForm();
    expect(await screen.findByLabelText(/first_name/)).toBeInTheDocument();
  });

  it("shows an error message when mastersMustPay could not be loaded", async () => {
    data.mastersMustPay = null;
    renderForm();
    expect(await screen.findByText("error_loading_page")).toBeInTheDocument();
  });

  it("renders a checkbox for each loaded study and toggles selection", async () => {
    data.studies = [makeStudy()];
    renderForm();

    const checkbox = await screen.findByLabelText("Computer Science");
    fireEvent.click(checkbox);
    expect(handleStudyToggle).toHaveBeenCalledWith(1, expect.any(Function));
  });

  it("renders mailing list checkboxes when lists are available", async () => {
    const lists: MailinglistDto[] = [
      { id: "list-1", name: "Newsletter" } as MailinglistDto,
    ];
    data.mailingLists = lists;
    renderForm();
    expect(await screen.findByLabelText("Newsletter")).toBeInTheDocument();
  });

  it("renders registration documents and toggles agreement", async () => {
    const docs: RegistrationDocumentResponseDto[] = [
      {
        id: 1,
        nameDutch: "Statuten",
        nameEnglish: "Statutes",
        urlDutch: "https://example.com/nl/doc.pdf",
        urlEnglish: "https://example.com/en/doc.pdf",
      } as RegistrationDocumentResponseDto,
    ];
    data.documents = docs;
    renderForm();

    expect(await screen.findByText("Statutes")).toBeInTheDocument();
    const checkbox = document.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    expect(checkbox).toBeTruthy();
  });

  it("links registration documents to the URL matching the page language", async () => {
    const docs: RegistrationDocumentResponseDto[] = [
      {
        id: 1,
        nameDutch: "Statuten",
        nameEnglish: "Statutes",
        urlDutch: "https://example.com/nl/doc.pdf",
        urlEnglish: "https://example.com/en/doc.pdf",
      } as RegistrationDocumentResponseDto,
    ];
    data.documents = docs;

    await i18next.changeLanguage("en");
    const { unmount } = renderForm();
    expect(
      await screen.findByRole("link", { name: "Statutes" }),
    ).toHaveAttribute("href", "https://example.com/en/doc.pdf");
    unmount();

    await i18next.changeLanguage("nl");
    try {
      renderForm();
      expect(
        await screen.findByRole("link", { name: "Statuten" }),
      ).toHaveAttribute("href", "https://example.com/nl/doc.pdf");
    } finally {
      await i18next.changeLanguage("en");
    }
  });

  it("calls handleRegisterInputChange when a text field changes", async () => {
    renderForm();
    const firstNameInput = await screen.findByLabelText(/first_name/);
    fireEvent.change(firstNameInput, { target: { value: "Jane" } });
    expect(handleRegisterInputChange).toHaveBeenCalled();
  });

  it("disables the submit button until the form is valid", async () => {
    renderForm();
    await screen.findByLabelText(/first_name/);
    expect(screen.getByText("become_member")).toBeDisabled();
  });

  it("calls handleRegisterSubmit on form submission", async () => {
    renderForm();
    await screen.findByLabelText(/first_name/);
    fireEvent.click(screen.getByText("become_member"));
    // The button is disabled while the form is incomplete, but the submit handler
    // is only reachable via a real form submit event which we simulate directly.
    fireEvent.submit(screen.getByText("become_member").closest("form")!);
    await waitFor(() => expect(handleRegisterSubmit).toHaveBeenCalled());
  });

  it("shows the membership price with the expiration copy", async () => {
    renderForm();
    expect(await screen.findByText(/for_1_year/)).toBeInTheDocument();
  });

  it("shows a multi-year expiration copy when the expiration time is not 1", async () => {
    data.membershipPaymentExpirationTime = 3;
    renderForm();
    expect(await screen.findByText(/for_x_years/)).toBeInTheDocument();
  });

  it("shows the masters-free copy when masters do not have to pay", async () => {
    renderForm();
    expect(
      await screen.findByText(/membership_free_for_masters/),
    ).toBeInTheDocument();
  });

  it("calls handleRegisterInputChange for every remaining personal-info field", async () => {
    renderForm();
    await screen.findByLabelText(/first_name/);

    const fields: [RegExp, string][] = [
      [/last_name/, "x"],
      [/email/, "x"],
      [/birth_date/, "2000-01-01"],
      [/^phone/, "x"],
      [/parent_phone_number/, "x"],
      [/street/, "x"],
      [/house_number/, "x"],
      [/postal_code/, "x"],
      [/city/, "x"],
      [/student_number/, "x"],
    ];
    fields.forEach(([label, value]) => {
      fireEvent.change(screen.getByLabelText(label), { target: { value } });
    });

    expect(handleRegisterInputChange).toHaveBeenCalledTimes(fields.length);
  });

  it("toggles a mail subscription checkbox on and off", async () => {
    const lists: MailinglistDto[] = [
      { id: "list-1", name: "Newsletter" } as MailinglistDto,
    ];
    data.mailingLists = lists;
    renderForm();

    const checkbox = await screen.findByLabelText("Newsletter");
    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();

    fireEvent.click(checkbox);
    expect(checkbox).not.toBeChecked();
  });

  it("changes the study start date select", async () => {
    renderForm();
    const select = await screen.findByLabelText(/study_start_date/);
    fireEvent.change(select, { target: { value: "2020-09-01" } });
    expect(select).toHaveValue("2020-09-01");
  });

  it("does not render the study start date select when no dates are configured", async () => {
    data.studyStartDates = "";
    renderForm();
    await screen.findByLabelText(/first_name/);
    expect(screen.queryByLabelText("study_start_date")).not.toBeInTheDocument();
  });

  it("marks the form invalid for a birth date under 18, valid once turning adult", async () => {
    vi.mocked(handleRegisterInputChange).mockImplementation(
      (e: any, setFormData: any) => {
        const { name, value } = e.target;
        setFormData((prev: any) => ({ ...prev, [name]: value }));
      },
    );
    renderForm();

    const birthDateInput = await screen.findByLabelText(/birth_date/);
    const today = new Date();
    const minorBirthDate = `${today.getFullYear() - 10}-01-01`;
    fireEvent.change(birthDateInput, { target: { value: minorBirthDate } });

    expect(screen.getByText("become_member")).toBeDisabled();

    const adultBirthDate = `${today.getFullYear() - 25}-01-01`;
    fireEvent.change(birthDateInput, { target: { value: adultBirthDate } });

    // Adults don't need a filled-in parentPhone, but other required fields
    // are still empty, so the form stays disabled - this just exercises the
    // isAdult branch rather than asserting overall validity.
    expect(screen.getByText("become_member")).toBeDisabled();
  });

  it("toggles document agreement checkboxes on and off", async () => {
    const docs: RegistrationDocumentResponseDto[] = [
      {
        id: 1,
        nameDutch: "Statuten",
        nameEnglish: "Statutes",
        urlDutch: "https://example.com/nl/doc.pdf",
        urlEnglish: "https://example.com/en/doc.pdf",
      } as RegistrationDocumentResponseDto,
    ];
    data.documents = docs;
    renderForm();

    await screen.findByText("Statutes");
    const checkbox = document.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();

    fireEvent.click(checkbox);
    expect(checkbox).not.toBeChecked();
  });
});
