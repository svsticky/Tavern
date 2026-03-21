import { useKeycloak } from "@react-keycloak/web";

export function meta() {
  return [
    { title: "New React Router App" },
    { name: "description", content: "Welcome to React Router!" },
  ];
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
