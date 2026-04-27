import type { NavigateFunction } from "react-router";

export const handleBack = (onBack: (() => void) | undefined, backTo: string | undefined, navigate: NavigateFunction) => {
  if (onBack) {
    onBack();
    return;
  }

  if (backTo) {
    navigate(backTo);
  }
};
