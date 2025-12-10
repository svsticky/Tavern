import { requireAuth } from "../middleware/auth";
import type { Route } from "./+types/test";

export async function loader({request}: Route.LoaderArgs) {
  return requireAuth(request);
}

export function meta() {
  return [
    { title: "New React Router App" },
    { name: "description", content: "Welcome to React Router!" },
  ];
}

export default function Home() {
  return <p>Test page</p>;
}
