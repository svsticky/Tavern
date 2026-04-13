import { t } from "i18next";
import type { ActivityResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Input from "~/components/UI/Input";

export default function editParticipantsTile({activity}: {activity: ActivityResponseDto}) {
    const enrollments = activity?.enrollments.filter(e => !e.isOnWaitingList) ?? [];
    const waitingList = activity?.enrollments.filter(e => e.isOnWaitingList) ?? [];

    return (
        <div className="lg:col-span-1 space-y-6">
            <BorderedTile>
              <div className="flex flex-col gap-4">
                <FormHeader title={`${t("participants")} (${enrollments.length}${activity.participantLimit ? `/${activity.participantLimit}` : ""})`} border />
              </div>
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
                            Unenroll
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
                                Move to participants
                              </span>
                            </Button>
                            
                            <Button variant="danger" className="flex-1 w-full overflow-hidden">
                              <span className="truncate block w-full px-1">
                                Unenroll
                              </span>
                            </Button>
                            
                          </div>
                        </BorderedTile>
                      ))}
                    </>
                  )
                }
              </div>

              <Button variant="secondary" className="mt-4">Enroll member</Button>
            </BorderedTile>
          </div>
    );
}