import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import { type MemberResponseDto } from "~/api";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import ChangeProfilePicture from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";
import ChangeAccountForm from "~/components/Account/ChangeProfileForm/ChangeAccountForm";
import { useApp } from "~/context/AppContext";

export default function AccountPage() {
  const { keycloak } = useKeycloak();
  const userId = keycloak.tokenParsed?.UserId;

  const { member } = useApp();

  return (
    <>
      <PageHeader title={t("account")} />
      
      <div className="flex flex-col lg:flex-row gap-12">
        {/* Left: Profile Picture */}
        <ChangeProfilePicture userId={userId}>
          <h2 className="font-bold text-l">{member?.firstName} {member?.lastName}</h2>
          <p className="text-gray-500 font-mono text-sm">{member?.studentNumber}</p>
        </ChangeProfilePicture>

        {/* Right: Forms */}
        {member && <ChangeAccountForm member={member} />}
      </div>
    </>
  );
}
