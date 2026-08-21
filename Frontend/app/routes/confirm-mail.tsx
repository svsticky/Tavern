import { t } from "i18next";
import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { postMembersByIdActivationEmail } from "~/api";
import NavBar from "~/components/Menu/NavBar/NavBar";

const RETRY_DELAY_MS = 2000;
const MAX_ATTEMPTS = 5;

/**
 * Landing page shown right after registration - whether the member paid a membership fee via
 * Mollie or was exempt - and after board-created member registration. Triggers the one-time
 * account-activation email (verify email + set password) for the given member and shows a
 * confirmation message. The `memberId` query param identifies which member to send it for; the
 * endpoint itself is idempotent (Member.ActivationEmailSentAt), so it's safe even if this page
 * is revisited or the request is retried.
 *
 * @page
 * @component
 */
export default function ConfirmMail() {
  const [searchParams] = useSearchParams();
  const memberId = searchParams.get("memberId");
  const [status, setStatus] = useState<"loading" | "done">(
    memberId ? "loading" : "done",
  );
  const attemptsRef = useRef(0);

  useEffect(() => {
    if (!memberId) return;
    let cancelled = false;

    const trySend = async () => {
      attemptsRef.current += 1;

      const response = await postMembersByIdActivationEmail({
        path: { id: memberId },
      });

      if (cancelled) return;

      if (
        response.status === 200 &&
        response.data === "Pending" &&
        attemptsRef.current < MAX_ATTEMPTS
      ) {
        // The member isn't linked to the auth system yet (still being provisioned in the
        // background right after registration). Retry briefly instead of giving up.
        setTimeout(trySend, RETRY_DELAY_MS);
        return;
      }

      setStatus("done");
    };

    trySend();

    return () => {
      cancelled = true;
    };
  }, [memberId]);

  return (
    <>
      <section id="home">
        <NavBar className="px-[5%] sm:px-[10%]" maxWidthBeforeCompact={900}>
          <NavBar.Branding title="" homepage="/register" />
        </NavBar>
      </section>

      <div className="p-4">
        {status === "loading" ? (
          <p className="text-lg">{t("loading")}...</p>
        ) : (
          <>
            <h1 className="text-2xl font-bold">{t("confirm_mail")}</h1>
            <p className="text-lg">{t("confirm_mail_description")}</p>
          </>
        )}
      </div>
    </>
  );
}
