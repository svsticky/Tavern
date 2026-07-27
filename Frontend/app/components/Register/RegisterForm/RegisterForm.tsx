import { useTranslation } from "react-i18next";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router";
import type { Mailinglist, RegistrationDocumentResponseDto, Study } from "~/api";
import { cn } from "~/util/tailwind.util";
import Tile from "../../Tiles/Tile";
import Button from "../../UI/Button";
import Checkbox from "../../UI/Checkbox";
import Form from "../../UI/Form/Form";
import { FormSection } from "../../UI/Form/FormSection";
import Input from "../../UI/Input";
import Select from "../../UI/Select";
import {
  handleRegisterInputChange,
  handleRegisterSubmit,
  handleStudyToggle,
  loadMailingLists,
  loadMastersMustPay,
  loadPrice,
  loadRegistrationDocuments,
  loadStudies,
  loadStudyStartDates,
} from "./RegisterForm.handlers";

export default function RegisterForm({ className }: { className?: string }) {
  const { t, i18n } = useTranslation();
  const isDutch = i18n.language.startsWith("nl");
  const [loading, setLoading] = useState(true);
  const [studies, setStudies] = useState<Study[]>([]);
  const [mailingLists, setMailingLists] = useState<Mailinglist[]>([]);
  const [documents, setDocuments] = useState<RegistrationDocumentResponseDto[]>([]);
  const [agreedDocumentIds, setAgreedDocumentIds] = useState<number[]>([]);
  const [mastersMustPay, setMastersMustPay] = useState<boolean | null>(null);
  const [price, setPrice] = useState<number | null>(null);
  const [membershipPaymentExpirationTime, setMembershipPaymentExpirationTime] =
    useState<number | null>(null);
  const [startDatesRaw, setStartDatesRaw] = useState<string>("09-01,02-01");
  const [selectedStartDate, setSelectedStartDate] = useState<string>("");

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
  });

  const [selectedStudies, setSelectedStudies] = useState<number[]>([]);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      await loadStudies(setStudies);
      await loadMastersMustPay(setMastersMustPay);
      await loadPrice(setPrice, setMembershipPaymentExpirationTime);
      await loadMailingLists(setMailingLists);
      await loadRegistrationDocuments(setDocuments);
      await loadStudyStartDates(setStartDatesRaw);
      setLoading(false);
    };
    loadData();
  }, []);

  const startDateOptions = useMemo(() => {
    const configured = startDatesRaw
      .split(",")
      .map((s) => s.trim())
      .filter((s) => s.includes("-"));

    if (configured.length === 0) return [];

    const now = new Date();
    const currentYear = now.getFullYear();
    const yearsBack = 6;
    const generatedDates: Date[] = [];

    configured.forEach((md) => {
      const [monthStr, dayStr] = md.split("-");
      const month = Number.parseInt(monthStr, 10) - 1;
      const day = Number.parseInt(dayStr, 10);

      for (let y = currentYear - yearsBack; y <= currentYear + 1; y++) {
        const d = new Date(Date.UTC(y, month, day));
        if (!Number.isNaN(d.getTime())) {
          generatedDates.push(d);
        }
      }
    });

    generatedDates.sort((a, b) => a.getTime() - b.getTime());

    // Filter past dates (<= now) + the next single future date
    const pastDates = generatedDates.filter((d) => d <= now);
    const nextFutureDate = generatedDates.find((d) => d > now);

    const validDates = [...pastDates];
    if (nextFutureDate) {
      validDates.push(nextFutureDate);
    }

    return validDates.map((d) => {
      const isoDate = d.toISOString().split("T")[0];
      return {
        value: isoDate,
        label: isoDate,
      };
    });
  }, [startDatesRaw]);

  useEffect(() => {
    if (startDateOptions.length > 0 && !selectedStartDate) {
      const nowTime = new Date().getTime();
      let closestOption = startDateOptions[0];
      let minDiff = Math.abs(
        new Date(startDateOptions[0].value).getTime() - nowTime,
      );

      startDateOptions.forEach((opt) => {
        const diff = Math.abs(new Date(opt.value).getTime() - nowTime);
        if (diff < minDiff) {
          minDiff = diff;
          closestOption = opt;
        }
      });

      setSelectedStartDate(String(closestOption.value));
    }
  }, [startDateOptions, selectedStartDate]);

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
      if (key === "parentPhone" && isAdult) {
        return true;
      }
      return value.trim() !== "";
    });

    const hasAtLeastOneStudy = selectedStudies.length > 0;
    const hasStartDate = selectedStartDate.trim() !== "";

    const allDocumentsAgreed =
      documents.length === 0 ||
      documents.every((doc) => agreedDocumentIds.includes(doc.id));

    return allFieldsFilled && hasAtLeastOneStudy && hasStartDate && allDocumentsAgreed;
  }, [formData, selectedStudies, selectedStartDate, documents, agreedDocumentIds]);

  const [subscriptions, setSubscriptions] = useState<number>(0);

  if (!loading && mastersMustPay === null) {
    return t("error_loading_page");
  }

  if (!loading && mastersMustPay === null) {
    return t("error_loading_page");
  }

  return (
    <Tile
      className={cn("border border-gray-200 bg-white", className)}
    >
      <Form
        onSubmit={(e) =>
          handleRegisterSubmit({
            e,
            isFormValid,
            setLoading,
            formData,
            selectedStudies,
            selectedStartDate,
            subscriptions,
            studies,
            navigate,
            mastersMustPay,
          })
        }
      >
        <FormSection title={t("personal_information")}>
          <Input
            label={t("first_name")}
            name="firstname"
            value={formData.firstname}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("last_name")}
            name="lastname"
            value={formData.lastname}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
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
              handleRegisterInputChange(e, setFormData)
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
              handleRegisterInputChange(e, setFormData)
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
              handleRegisterInputChange(e, setFormData)
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
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
          />
          <Input
            label={t("street")}
            name="street"
            value={formData.street}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("house_number")}
            name="houseNumber"
            value={formData.houseNumber}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("postal_code")}
            name="postalCode"
            value={formData.postalCode}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <Input
            label={t("city")}
            name="city"
            value={formData.city}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
        </FormSection>

        <FormSection title={t("study_information")}>
          <Input
            label={t("student_number")}
            name="studentNumber"
            value={formData.studentNumber}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              handleRegisterInputChange(e, setFormData)
            }
            disabled={loading}
            required
          />
          <div>
            <span className="text-sm font-medium text-gray-700 block mb-2">
              {t("studies")}
            </span>
            {loading ? (
              t("loading")
            ) : (
              <div className="space-y-2">
                {studies.map((study) => (
                  <Checkbox
                    key={study.id}
                    label={study.title}
                    checked={selectedStudies.includes(study.id!)}
                    onChange={() =>
                      handleStudyToggle(study.id!, setSelectedStudies)
                    }
                    disabled={loading}
                  />
                ))}
              </div>
            )}
          </div>
          {startDateOptions.length > 0 && (
            <Select
              label={t("study_start_date")}
              value={selectedStartDate}
              onChange={(e) => setSelectedStartDate(e.target.value)}
              options={startDateOptions}
              disabled={loading}
              required
            />
          )}
        </FormSection>

        {mailingLists.length > 0 && (
          <FormSection title={t("mail_subscriptions")}>
            {loading ? (
              t("loading")
            ) : (
              <div className="space-y-2">
                {mailingLists.map((list) => (
                <Checkbox
                  key={list.id}
                  label={list.name}
                  disabled={loading}
                  checked={(subscriptions & list.bitValue!) !== 0}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                    const newValue = e.target.checked
                      ? subscriptions | list.bitValue!
                      : subscriptions & ~list.bitValue!;
                    setSubscriptions(newValue);
                  }}
                />
              ))}
            </div>
          )}
        </FormSection>
        )}

        {documents.length > 0 && (
          <FormSection title={t("terms_and_conditions")}>
            <div className="space-y-3">
              <span className="text-sm font-medium text-gray-700 block">
                {t("agree_to_documents")}
              </span>
              <div className="flex flex-wrap gap-2 text-sm">
                {documents.map((doc, idx) => {
                  const isChecked = agreedDocumentIds.includes(doc.id);
                  const name = isDutch ? doc.nameDutch : doc.nameEnglish;
                  return (
                    <div key={doc.id} className="flex items-center gap-2">
                      <Checkbox
                        label=""
                        checked={isChecked}
                        disabled={loading}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                          if (e.target.checked) {
                            setAgreedDocumentIds((prev) => [...prev, doc.id]);
                          } else {
                            setAgreedDocumentIds((prev) =>
                              prev.filter((id) => id !== doc.id),
                            );
                          }
                        }}
                      />
                      <a
                        href={doc.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-blue-600 hover:underline font-medium"
                      >
                        {name}
                      </a>
                      {idx < documents.length - 1 && <span className="text-gray-400">,</span>}
                    </div>
                  );
                })}
              </div>
            </div>
          </FormSection>
        )}

        <p className="text-gray-500">
          {price != null &&
            `${t("membershipcosts").replace("<price>", `€${price?.toFixed(2)}`)}${membershipPaymentExpirationTime != null ? (membershipPaymentExpirationTime === 1 ? t("for_1_year") : t("for_x_years").replace("<years>", membershipPaymentExpirationTime.toString())) : ""}${mastersMustPay === false ? t("membership_free_for_masters") : "."}`}
        </p>

        <Button
          type="submit"
          className={cn(
            "w-full",
            !isFormValid && "opacity-50 cursor-not-allowed",
          )}
          disabled={!isFormValid || loading}
        >
          {t("become_member")}
        </Button>
      </Form>
    </Tile>
  );
}
