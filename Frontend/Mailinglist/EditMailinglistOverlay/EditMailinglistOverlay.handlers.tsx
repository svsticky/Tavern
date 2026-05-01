import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { 
    deleteApiMailinglistsById,
    postApiMailinglists, 
    putApiMailinglistsById, 
    type Mailinglist, 
    type PostMailinglistDto
} from "~/api";

type HandleSubmitArgs = {
    e: React.FormEvent;
    formData: Omit<Mailinglist, 'id'>;
    mailingList?: Mailinglist;
    setLoading: (loading: boolean) => void;
    onComplete: (list?: Mailinglist) => void;
};
export const handleMailingListSubmit = async ({ 
    e, formData, mailingList, setLoading, onComplete 
}: HandleSubmitArgs) => {
    e.preventDefault();
    setLoading(true);

    try {
        if (mailingList?.id) {
            const response = await putApiMailinglistsById({ 
                path: { id: mailingList.id }, 
                body: formData as PostMailinglistDto 
            });

            if (response.error) throw new Error();

            const updatedList: Mailinglist = {
                ...formData,
                id: mailingList.id,
            };

            toast.success(t("mailing_list_updated"));
            onComplete(updatedList); 
        } else {
            const response = await postApiMailinglists({ 
                body: formData as PostMailinglistDto 
            });

            if (response.error || !response.data) throw new Error();

            const data = response.data as any;
            const newList: Mailinglist = {
                ...formData,
                id: data.id,
                bitValue: data.bitValue,
            };

            console.log("Created mailing list:", newList);

            toast.success(t("mailing_list_created"));
            onComplete(newList);
        }
    } catch (error) {
        toast.error(t("error_saving_mailing_list"));
    } finally {
        setLoading(false);
    }
};

type HandleDeleteArgs = {
    mailingList: Mailinglist;
    setLoading: (loading: boolean) => void;
    onComplete: (list?: Mailinglist) => void;
};

export const handleMailingListDelete = async ({ mailingList, setLoading, onComplete }: HandleDeleteArgs) => {
    setLoading(true);
    try {
        const response = await deleteApiMailinglistsById({ path: { id: mailingList.id! } });
        
        if (response.error) throw new Error();

        toast.success(t("mailing_list_deleted"));
        onComplete(undefined);
    } catch (error) {
        toast.error(t("error_deleting_mailing_list"));
    } finally {
        setLoading(false);
    }
};