import { t } from "i18next";
import { useMemo, useState } from "react";
import { useNavigate } from "react-router";
import type { Language } from "~/api";
import Button from "~/components/UI/Button";
import Form from "~/components/UI/Form/Form";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import RequiredAsterisk from "~/components/UI/RequiredAstrix";
import Select from "~/components/UI/Select";
import { cn } from "~/util/tailwind.util";
import {
  handleCreateBegunstigerInputChange,
  handleCreateSubmit,
} from "./create-begunstiger.handlers";

/**
 * The page for creating a begunstiger.
 *
 * This component manages the state for personal details. It includes
 * complex client-side validation, such as checkingthe user's age to
 * determine if parent contact information is required.
 *
 * @component
 * @page
 */
export default function CreateBegunstigerPage() {
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    firstname: "",
    lastname: "",
    email: "",
    birthDate: "",
    phone: "",
    parentPhone: "",
    street: "",
    houseNumber: "",
    postalCode: "",
    city: "",
    studentNumber: "",
    language: "NL" as Language,
    isBegunstiger: false,
  });

  const languageOptions = [
    { value: "NL", label: t("dutch") },
    { value: "EN", label: t("english") },
  ];

  const isFormValid = useMemo(() => {
    const birthDateValue = new Date(formData.birthDate);
    let isAdult = false;

    if (birthDateValue && !Number.isNaN(birthDateValue.getTime())) {
      const today = new Date();
      let age = today.getFullYear() - birthDateValue.getFullYear();
      const monthDiff = today.getMonth() - birthDateValue.getMonth();

      if (
        monthDiff < 0 ||
        (monthDiff === 0 && today.getDate() < birthDateValue.getDate())
      ) {
        age--;
      }
      isAdult = age >= 18;
    }

    const allFieldsFilled = Object.entries(formData).every(([key, value]) => {
      if (typeof value === "boolean") return true;
      if (key === "parentPhone" && isAdult) {
        return true;
      }
      return value.trim() !== "";
    });

    return allFieldsFilled;
  }, [formData]);

  return (
    <>
      <PageHeader title={t("create_member")} backTo="/admin/members" />
      <Form
        onSubmit={(e) =>
          handleCreateSubmit({
            e,
            isFormValid,
            setLoading,
            formData,
            navigate,
          })
        }
      >
        <FormSection title={t("personal_information")}>
          <Input
            label={t("begunstiger")}
            name="isBegunstiger"
            type="checkbox"
            checked={formData.isBegunstiger}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
          />
          <Input
            label={t("first_name")}
            name="firstname"
            value={formData.firstname}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("last_name")}
            name="lastname"
            value={formData.lastname}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("email")}
            name="email"
            type="email"
            value={formData.email}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("birth_date")}
            name="birthDate"
            type="date"
            value={formData.birthDate}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("phone")}
            name="phone"
            type="tel"
            value={formData.phone}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("parent_phone_number")}
            name="parentPhone"
            type="tel"
            value={formData.parentPhone}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
          />
          <div className="flex flex-col gap-1">
            <span className="text-sm font-medium text-gray-700">
              {t("student_number")}
              <RequiredAsterisk required />
            </span>
            <div className="flex gap-2">
              {formData.isBegunstiger && <p className="my-auto text-m">F_</p>}
              <Input
                name="studentNumber"
                value={formData.studentNumber}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  handleCreateBegunstigerInputChange(e, setFormData)
                }
                disabled={loading}
                required
              />
            </div>
          </div>
          <Select
            label={t("preferred_language")}
            name="language"
            value={formData.language}
            options={languageOptions}
            onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("street")}
            name="street"
            value={formData.street}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("house_number")}
            name="houseNumber"
            value={formData.houseNumber}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("postal_code")}
            name="postalCode"
            value={formData.postalCode}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("city")}
            name="city"
            value={formData.city}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleCreateBegunstigerInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
        </FormSection>

        <Button
          type="submit"
          className={cn(
            "w-full",
            !isFormValid && "opacity-50 cursor-not-allowed",
          )}
          disabled={!isFormValid || loading}
        >
          {t("create")}
        </Button>
      </Form>
    </>
  );
}
