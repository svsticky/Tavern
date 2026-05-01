import { useEffect, useState } from "react";
import { type Study } from "~/api";
import DataTableTile from "../../Tiles/DataTableTile";
import { t } from "i18next";
import Button from "../../UI/Button";
import Modal from "../../UI/Modal/Modal";
import BorderedTile from "../../Tiles/BorderedTile";
import EditStudyOverlay from "./../EditStudyOverlay/EditStudyOverlay";
import { fetchStudies, handleStudyEdited } from "./ManageStudiesDatatable.handlers";

/**
 * A management dashboard component for viewing and modifying study programs.
 * 
 * This component renders a data table listing all studies, providing functionality 
 * to add new studies or edit existing ones via a modal interface. It handles the 
 * integration between the data display layer and the study modification overlays.
 * 
 * @component
 */
export default function ManageStudiesDatatable() {
    const [studies, setStudies] = useState<Study[]>([]);
    const [loading, setLoading] = useState(true);
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [editedStudy, setEditedStudy] = useState<Study | undefined>(undefined);

    const columns = [
        {
            header: t("study_name"),
            render: (item: Study) => item.title,
        },
        {
            header: t("type"),
            render: (item: Study) => item.type,
        },
        {
            header: t("nominal_duration"),
            render: (item: Study) => `${item.nominalDurationYears} ${t("years")}`,
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
        fetchStudies(setLoading, setStudies);
    }, []);
    return (
        <BorderedTile>
            <DataTableTile columns={columns} data={studies} emptyText={loading ? t("loading") : t("no_studies")} />
            <Modal isOpen={isEditModalOpen} onClose={() => { setIsEditModalOpen(false); setEditedStudy(undefined); }} title={editedStudy ? t("edit_study") : t("add_study")}>
                <EditStudyOverlay
                  onStudyAdded={(study) => handleStudyEdited({ study, editedStudy, setStudies, setIsEditModalOpen, setEditedStudy })}
                  study={editedStudy}
                />
            </Modal>
        </BorderedTile>
    );
}
