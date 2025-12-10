import { redirect } from "react-router";
import { getSession } from "../sessions.server";
import type { Route } from "../+types/root";

type LoaderArgs = Route.LoaderArgs | Request;

export async function requireAuth(args: LoaderArgs) {
	const request = args instanceof Request ? args : args.request;

	const session = await getSession(request.headers.get("Cookie"));

	if (!session.has("userId")) {
		throw redirect("/login");
	}

	return { userId: session.get("userId") };
}
