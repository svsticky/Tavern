import type React from "react";
import type { NavigateFunction } from "react-router";
import type { ActivityResponseDto } from "~/api";

export const handleEditClick = (e: React.MouseEvent, navigate: NavigateFunction, activity: ActivityResponseDto) => {
  e.preventDefault();
  e.stopPropagation();
  navigate(`/activities/edit/${activity.id}`);
};
