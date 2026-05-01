import type { NavigateFunction } from "react-router";

/**
 * Orchestrates back-navigation logic based on provided overrides or fallback routes.
 * 
 * This utility follows a priority order:
 * 1. If a custom `onBack` function is provided, it executes that logic exclusively.
 * 2. Otherwise, if a `backTo` string is provided, it uses the router's `navigate` function to redirect.
 * 
 * @param {(() => void) | undefined} onBack - An optional custom callback to execute instead of navigation.
 * @param {string} [backTo] - An optional route path to navigate to if no custom callback is provided.
 * @param {NavigateFunction} navigate - The react-router navigation function instance.
 */
export const handleBack = (onBack: (() => void) | undefined, backTo: string | undefined, navigate: NavigateFunction) => {
  if (onBack) {
    onBack();
    return;
  }

  if (backTo) {
    navigate(backTo);
  }
};
