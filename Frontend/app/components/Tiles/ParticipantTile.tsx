import { useKeycloak } from "@react-keycloak/web";
import type { MemberSummaryDto } from "~/api/types.gen";
import Tile from "./Tile";
import { useState } from "react";
import path from "path/win32";

export default function ParticipantTile({ member }: { member: MemberSummaryDto }) {
  const { keycloak, initialized } = useKeycloak();
  
  const imageUrl = `${import.meta.env.ApiUrl}/api/profilepicture/view/${member.profilePicturePath}`;
  const fallbackUrl = "/profile-picture.svg";

  const [imgError, setImgError] = useState(false);

  return (
    <Tile className="bg-slate-50 flex items-center gap-4 border border-transparent hover:border-slate-200 hover:bg-white transition-all group cursor-default">
      <div className="relative flex-shrink-0">
        <div className="w-12 h-12 rounded-full overflow-hidden flex items-center justify-center bg-(--board-primary)shadow-inner group-hover:scale-105 transition-transform duration-200">
          <img
            src={imgError || !member.profilePicturePath ? fallbackUrl : imageUrl}
            alt="Profile"
            onError={() => setImgError(true)}
            className="w-full h-full object-cover"
          />
        </div>
      </div>

      <div className="overflow-hidden flex flex-col justify-center">
        <p className="font-bold text-slate-900 truncate leading-tight group-hover:text-(--board-primary-dark) transition-colors">
          {member.firstName} {member.lastName}
        </p>
      </div>
    </Tile>
  );
}