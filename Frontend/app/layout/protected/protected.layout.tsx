import { Outlet, redirect } from "react-router";

export async function loader() {
  const user = await getUser(); // jouw auth check
  if (!user) {
    throw redirect("/login");
  }
  return null;
}

export default function ProtectedLayout() {
  return <Outlet />;
}
