import { getApiActivities } from "~/api";
import { requireAuth } from "~/middleware/auth";
import type { Route } from "./+types/home";

export async function loader({ request }: Route.LoaderArgs) {
  return requireAuth(request);
}

export function meta() {
  return [
    { title: "New React Router App" },
    { name: "description", content: "Welcome to React Router!" },
  ];
}

export default function Home() {
  /**
   * TODO: Swap this out with tanstack query
   */
  const getActivities = () => {
    getApiActivities().then((r) => console.log(r));
  };

  return (
    <div>
      <p>Home</p>
      <button type="button" onClick={() => getActivities()}>
        Klik mij
      </button>
    </div>
  );
}
