import { createCookieSessionStorage } from "react-router";

type SessionData = {
  auth_token: string;
};

type SessionFlashData = {
  error: string;
};

/**
 * TODO: Below is the temporary implementation of the Cookie session. Eventually secret has to come from the .env
 */
const { getSession, commitSession, destroySession } =
  createCookieSessionStorage<SessionData, SessionFlashData>({
    // a Cookie from `createCookie` or the CookieOptions to create one
    cookie: {
      name: "__session",

      // all of these are optional
      // domain: "reactrouter.com",
      // Expires can also be set (although maxAge overrides it when used in combination).
      // Note that this method is NOT recommended as `new Date` creates only one date on each server deployment, not a dynamic date in the future!
      //
      // expires: new Date(Date.now() + 60_000),
      // httpOnly: true,
      maxAge: 60,
      path: "/",
      // sameSite: "lax",
      secrets: ["s3cret1"],
      // secure: true,
    },
  });

export { getSession, commitSession, destroySession };
