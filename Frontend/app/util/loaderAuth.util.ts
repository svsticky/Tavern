import { data } from "react-router";
import { getSettingsById } from "~/api";
import { waitForAuthReady } from "~/layout/auth-service";
import type { TokenParsed } from "~/types/TokenParsed";
import { getCommitteeYear } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";

/**
 * Returns the parsed token of the logged-in user, for use in route loaders.
 * When the user isn't logged in, it redirects to the login page and never
 * resolves, so the loader doesn't continue without a user.
 *
 * @returns {Promise<TokenParsed>} The parsed token of the logged-in user.
 */
export async function requireTokenParsed(): Promise<TokenParsed> {
  const authService = await waitForAuthReady();

  const tokenParsed = await authService?.getTokenParsed();
  if (!tokenParsed) {
    await authService?.login(window.location.href);

    return new Promise<never>(() => {});
  }

  return tokenParsed;
}

/**
 * Returns the parsed token of the logged-in user, for use in the loaders of
 * admin routes. Redirects to the login page when the user isn't logged in, like
 * `requireTokenParsed`.
 *
 * @returns {Promise<TokenParsed>} The parsed token of the logged-in board or candidate board member.
 * @throws {Response} A 403 response when the user isn't a board or candidate board member, which shows the route's forbidden page.
 */
export async function requireBoardOrCandidateBoard(): Promise<TokenParsed> {
  const tokenParsed = await requireTokenParsed();
  if (!isBoardOrCandidateBoard(tokenParsed)) {
    throw data("Forbidden", { status: 403 });
  }
  return tokenParsed;
}

/**
 * Returns the current committee year, for use in route loaders. Loaders run
 * before the authenticated layout has loaded the `CommitteeCreationDate`
 * setting, so this fetches it itself; when it fails to load, it falls back to
 * the default creation date (see `getCommitteeYear`).
 *
 * @returns {Promise<number>} The current committee year.
 */
export async function loadCommitteeYear(): Promise<number> {
  try {
    const response = await getSettingsById({
      path: { id: "CommitteeCreationDate" },
    });
    return getCommitteeYear(response.data?.value);
  } catch (error) {
    console.error("Failed to load the committee creation date:", error);
    return getCommitteeYear();
  }
}
