import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { getApiStudies, type Study } from "~/api";
import DataTableTile from "../Tiles/DataTableTile";
import { t } from "i18next";
import Button from "../UI/Button";
import Modal from "../UI/Modal";
import BorderedTile from "../Tiles/BorderedTile";
import EditStudyOverlay from "./EditStudyOverlay";

export default function ManageStudiesDatatable() {
    const [studies, setStudies] = useState<Study[]>([]);
    const [loading, setLoading] = useState(true);
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [editedStudy, setEditedStudy] = useState<Study | undefined>(undefined);

    const columns = [
        {
            header: "Study Name",
            render: (item: Study) => item.title,
        },
        {
            header: "Type",
            render: (item: Study) => item.type,
        },
        {
            header: "Nominal duration",
            render: (item: Study) => `${item.nominalDurationYears} years`,
        },
        {
            header: (
                <div className="flex justify-end">
                    <Button type="button" variant="primary" onClick={() => setIsEditModalOpen(true)}>
                        {t("add_study")}
                    </Button>
                </div>
            ),
            render: (item: Study) => (
                <div className="flex justify-end">
                    <Button type="button" variant="secondary" onClick={() => { setEditedStudy(item); setIsEditModalOpen(true); }}>
                        {t("edit")}
                    </Button>
                </div>
            ),
            className: "w-px whitespace-nowrap text-right",
        },
    ];

    useEffect(() => {
        const fetchStudies = async () => {
            try {
                setLoading(true);
                const response = await getApiStudies();

                if(response.data) {
                    setStudies(response.data);
                }

            } catch (error) {
                console.error("Error fetching studies:", error);
            } finally {
                setLoading(false);
            }
        };
        toast.promise(fetchStudies(), {
            loading: t("fetching_studies"),
            success: t("studies_fetched_successfully"),
            error: t("failed_to_fetch_studies")
        });
    }, []);

    const handleStudyEdited = (study?: Study) => {
        if(!study) {
            if(editedStudy) setStudies(prev => prev.filter(s => s.id !== editedStudy.id));
            setIsEditModalOpen(false);
            setEditedStudy(undefined);
            return;
        }

        if(study.id){
            setStudies(prev => prev.map(s => s.id === study.id ? study : s));
            setIsEditModalOpen(false);
            setEditedStudy(undefined);
            return;
        }

        setStudies(prev => [...prev, { ...study }]);
        setIsEditModalOpen(false);
    }
    return (
        <BorderedTile>
            <DataTableTile columns={columns} data={studies} emptyText={loading ? `${t("loading")}...` : t("no_studies")} />
            <Modal isOpen={isEditModalOpen} onClose={() => { setIsEditModalOpen(false); setEditedStudy(undefined); }} title={editedStudy ? t("edit_study") : t("add_study")}>
                <EditStudyOverlay onStudyAdded={handleStudyEdited} study={editedStudy} />
            </Modal>
        </BorderedTile>
    );
}