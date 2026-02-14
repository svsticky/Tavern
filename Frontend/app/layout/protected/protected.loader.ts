import { requireAuth } from "~/middleware/auth";
import type { Route } from "../../+types/root";

export async function loader({ request }: Route.LoaderArgs) {
  return requireAuth(request);
}