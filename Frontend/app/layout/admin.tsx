import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState, type ReactNode } from "react";
import { Outlet, useNavigate } from "react-router";
import { useApp } from "~/context/AppContext";
import { isBoardOrCandidateBoard, isInGroupWithId } from "~/util/group.util";

interface Props {
  children: ReactNode;
}

export default function AdminLayout() {
  const { boardGroupId, candidateBoardGroupId } = useApp();
  const { keycloak } = useKeycloak();
  const navigate = useNavigate();
  const [isLoading, setLoading] = useState(true);

  useEffect(() => {
    if (boardGroupId === null || candidateBoardGroupId === null || !keycloak.tokenParsed) {
      return;
    }
    if (!isInGroupWithId(keycloak.tokenParsed, boardGroupId) && !isInGroupWithId(keycloak.tokenParsed, candidateBoardGroupId)) {
      navigate("/");
      return;
    }

    setLoading(false);
  }, [boardGroupId, candidateBoardGroupId, navigate, keycloak]);

  if (isLoading) {
    return `${t("loading")}`;
  }

  return <Outlet />;;
};
