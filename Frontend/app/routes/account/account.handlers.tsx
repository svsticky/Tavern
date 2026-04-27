import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiMembersById, type MemberResponseDto } from "~/api";

type LoadAccountArgs = {
  userId: string | undefined;
  setMember: (member: MemberResponseDto) => void;
  setLoading: (loading: boolean) => void;
};

export const loadAccountData = async ({ userId, setMember, setLoading }: LoadAccountArgs) => {
  if (!userId) return;
  try {
    const res = await getApiMembersById({ path: { id: userId } });
    if (res.data) {
      setMember(res.data);
    }
  } catch (err) {
    console.error("Error while loading user data:", err);
    toast.error(t("loading_profile_failed"));
  } finally {
    setLoading(false);
  }
};
