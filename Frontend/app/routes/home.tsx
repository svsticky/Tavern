import { useKeycloak } from "@react-keycloak/web";
import { useEffect } from "react";
import { getApiActivities } from "~/api";

export function meta() {
  return [
    { title: "New React Router App" },
    { name: "description", content: "Welcome to React Router!" },
  ];
}

function getActivities(token: string) {
  return getApiActivities({
      baseUrl: "https://localhost:8080",
      headers: {
          Authorization: `Bearer ${token}`
      }
  });
}

export default function Home() {
  const { keycloak } = useKeycloak();
  
  return (
    <div className="secure-page">
      <div>{`User is ${
        !keycloak.authenticated ? "NOT " : ""
      }authenticated`}</div>
    </div>
  );
}
