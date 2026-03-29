import {
  index,
  layout,
  type RouteConfig,
  route,
} from "@react-router/dev/routes";

export default [
  layout("layout/authenticated.tsx", [
    layout("layout/navbar.tsx", [
      index("routes/home.tsx"),
      route("announcements", "routes/announcements.tsx"),
      route("activities", "routes/activities.tsx"),
      route("activities/create", "routes/create-activity.tsx"),
      route("activities/:id", "routes/activity.tsx"),
      route("settings", "routes/settings.tsx"),
      route("external-links", "routes/external-links.tsx"),
    ]),
  ]),
  

  route("login", "routes/auth/login.tsx"),
  route("logout", "routes/auth/logout.tsx"),
] satisfies RouteConfig;

