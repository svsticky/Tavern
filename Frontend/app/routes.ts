import {
  index,
  layout,
  type RouteConfig,
  route,
} from "@react-router/dev/routes";

export default [
  layout("layout/authenticated.tsx", [index("routes/home.tsx")]),

  route("login", "routes/auth/login.tsx"),
  route("logout", "routes/auth/logout.tsx"),
] satisfies RouteConfig;
