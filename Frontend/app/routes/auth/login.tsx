import { useKeycloak } from "@react-keycloak/web";
import type { Route } from "./+types/login";

export default function Login({ loaderData }: Route.ComponentProps) {
  const { keycloak } = useKeycloak();
  return (
    <div>
      <div>
        <button
          type="button"
          onClick={() => keycloak.login({redirectUri: window.location.origin + "/"})}
          className="mt-2 rounded-lg bg-blue-600 px-4 py-2 font-semibold text-white shadow-md shadow-blue-500/30 transition hover:bg-blue-700"
        >
          Sign in
        </button>
      </div>
    </div>
  );
}
