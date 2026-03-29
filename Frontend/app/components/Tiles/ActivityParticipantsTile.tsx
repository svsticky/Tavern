import type { Enrollment, MemberSummaryDto } from "~/api";
import ParticipantTile from "./ParticipantTile";
import Tile from "./Tile";
import { t } from "i18next";

export default function ActivityParticipantsTile({ title: title, members: members }: { title?: string; members: MemberSummaryDto[] }) {
  const count = members?.length ?? 0;

  if (count === 0) return null;

  return (
    <Tile>
      <h2 className="text-2xl font-extrabold text-slate-900 mb-8 flex items-center gap-3">
        {title || t("participants")} 
        <span className="bg-slate-100 text-slate-500 text-sm py-1 px-3 rounded-full font-bold">
          {count}
        </span>
      </h2>
      
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {members?.map((member, idx) => (
          <ParticipantTile key={idx} member={member} />
        ))}
      </div>
    </Tile>
  );
}