import { Outlet } from "react-router";

export function meta() {
  return [
    { title: "Auth" },
    {
      name: "description",
      content: "Authenticate to access the app.",
    },
  ];
}

export default function AuthLayout() {
  return (
    <div className="flex min-h-full flex-col justify-center px-6 py-12 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-sm">
        <img
          alt="Sticky logo"
          src="https://public.svsticky.nl/logos/logo_compact_outline_wit_kleur.svg"
          className="mx-auto h-10 w-auto"
        />
        <h2 className="mt-10 text-center text-2xl/9 font-bold tracking-tight text-white">
          Authentificatie
        </h2>
      </div>

      <div className="mt-10 sm:mx-auto sm:w-full sm:max-w-sm">
        <Outlet />
      </div>
    </div>
  );
}
