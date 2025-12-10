import { data, redirect } from "react-router";
import { commitSession, getSession } from "../../sessions.server";
import type { Route } from "./+types/login";

export async function loader({ request }: Route.LoaderArgs) {
  const session = await getSession(request.headers.get("Cookie"));

  if (session.has("userId")) {
    // Redirect to the home page if they are already signed in.
    return redirect("/");
  }

  return data(
    { error: session.get("error") },
    {
      headers: {
        "Set-Cookie": await commitSession(session),
      },
    },
  );
}

export default function Register() {
  return (
    <div>
      <div className="flex flex-col gap-2">
        <label className="text-sm font-medium text-slate-700" htmlFor="email">
          Email
        </label>
        <input
          id="email"
          name="email"
          type="email"
          // required
          className="rounded-lg border border-slate-200 px-3 py-2 shadow-sm outline-none ring-blue-500 focus:border-blue-400 focus:ring"
        />
      </div>
      <div className="flex flex-col gap-2">
        <label
          className="text-sm font-medium text-slate-700"
          htmlFor="password"
        >
          Password
        </label>
        <input
          id="password"
          name="password"
          type="password"
          // required
          className="rounded-lg border border-slate-200 px-3 py-2 shadow-sm outline-none ring-blue-500 focus:border-blue-400 focus:ring"
        />
      </div>
      <button
        disabled
        type="submit"
        className="mt-2 rounded-lg bg-blue-600 px-4 py-2 font-semibold text-white shadow-md shadow-blue-500/30 transition hover:bg-blue-700"
      >
        Registeren
      </button>
    </div>
  );
}
