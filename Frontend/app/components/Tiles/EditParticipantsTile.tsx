import { t } from "i18next";
import toast from "react-hot-toast";
import { deleteApiEnrollmentsByActivityIdByMemberId, getApiActivitiesByIdEnrollmentsExport, type ActivityResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Input from "~/components/UI/Input";

export default function editParticipantsTile({activity}: {activity: ActivityResponseDto}) {
    const enrollments = activity?.enrollments.filter(e => !e.isOnWaitingList) ?? [];
    const waitingList = activity?.enrollments.filter(e => e.isOnWaitingList) ?? [];

    const handleDownloadEnrollments = () => {
      const handleDownloadAction = async () => {
        try {
          const response = await getApiActivitiesByIdEnrollmentsExport({ 
              path: { id: activity.id },
              responseType: 'blob' 
          });

          const blob = new Blob([response.data as any], { type: "text/csv" });
          const url = window.URL.createObjectURL(blob);
          
          const link = document.createElement("a");
          link.href = url;
          link.setAttribute("download", `enrollments_${activity.name}.csv`);
          document.body.appendChild(link);
          link.click();

          link.parentNode?.removeChild(link);
          window.URL.revokeObjectURL(url);
        } catch (error) {
          console.error("Error downloading enrollments:", error);
          throw error; 
        }
      };

      toast.promise(handleDownloadAction(), {
        loading: t("downloading"),
        success: t("download_success"),
        error: t("download_failed"),
      });
    }

    const handleUnenroll = (memberId: string) => {
      const handleUnenrollAction = async () => {
        try {
          const response = await deleteApiEnrollmentsByActivityIdByMemberId({
            path: { activityId: activity.id, memberId: memberId },
          });
        } catch (error) {
          console.error("Error unenrolling participant:", error);
        }
      }
    }

    return (
        <div className="lg:col-span-1 space-y-6">
            <BorderedTile>
              <div className="flex flex-col gap-4">
                <FormHeader title={`${t("participants")} (${enrollments.length}${activity.participantLimit ? `/${activity.participantLimit}` : ""})`} border />
                <Button variant="primary" onClick={handleDownloadEnrollments}>{t("download_enrollments")}</Button>
                <div className="space-y-4 pr-2">
                  {
                    enrollments.length > 0 ? (
                    enrollments.map((e, index) => (
                      <BorderedTile key={index} className="bg-gray-50 p-2" noPadding>
                          
                          <p className="font-semibold text-sm truncate">
                            {e.member.firstName} {e.member.lastName}
                          </p>

                          <div className="flex flex-col sm:flex-row sm:items-center gap-2 w-full justify-between">
                            
                            <div className="flex items-center gap-2 flex-1 min-w-0"> 
                              <span className="text-m text-gray-500 shrink-0">€</span>
                              
                              <div className="flex-1 min-w-0">
                                <Input 
                                  type="number"
                                  step="0.01" 
                                  defaultValue={e.price} 
                                  className="h-8 text-sm text-right px-2 w-full" 
                                />
                              </div>
                            </div>
                            
                            <Button variant="danger" className="shrink-0 whitespace-nowrap">
                              {t("unenroll")}
                            </Button>
                          </div>
                      </BorderedTile>
                    ))
                  ) : (
                    <p className="text-sm text-gray-400 italic">{t("no_participants_yet")}</p>
                  )}
                  {
                    waitingList.length > 0 && (
                      <>
                        <FormHeader title={`${t("waiting_list")} (${waitingList.length})`} border/>

                        {waitingList.map((e, index) => (
                          <BorderedTile key={index} className="bg-gray-50 p-2" noPadding>
                            <p className="font-semibold text-sm truncate">
                              {e.member.firstName} {e.member.lastName}
                            </p>

                            <div className="flex flex-col sm:flex-row items-center gap-2 w-full mt-2">
    
                              <Button variant="secondary" className="flex-1 w-full overflow-hidden">
                                <span className="truncate block w-full px-1">
                                  {t("move_to_participants")}
                                </span>
                              </Button>
                              
                              <Button variant="danger" className="flex-1 w-full overflow-hidden">
                                <span className="truncate block w-full px-1">
                                  {t("unenroll")}
                                </span>
                              </Button>
                              
                            </div>
                          </BorderedTile>
                        ))}
                      </>
                    )
                  }
                </div>

                <Button variant="secondary" className="mt-4">
                  {t("enroll_member")}
                </Button>
              </div>
            </BorderedTile>
          </div>
    );
}