import {
  index,
  layout,
  type RouteConfig,
  route,
} from "@react-router/dev/routes";

export default [
  layout("layout/protected/index.ts", [
    layout("layout/navbar.tsx", [
      index("routes/home.tsx"),
      route("announcements", "routes/announcements.tsx"),
      route("activities", "routes/activities.tsx"),
    ]),

    route("admin", "layout/admindashboard.tsx", [
      index("routes/admin/dashboard.tsx"),
      route("activities", "routes/admin/activities.tsx"),
      route("members", "routes/admin/members.tsx"),
      route("groups", "routes/admin/groups.tsx"),
      route("payments", "routes/admin/payments.tsx"),
      route("announcements", "routes/admin/announcements.tsx"),
      route("settings", "routes/admin/settings.tsx"),
    ]),
  ]),

  layout("layout/auth.tsx", [
    route("login", "routes/auth/login.tsx"),
    route("register", "routes/auth/register.tsx"),
  ]),
  route("logout", "routes/auth/logout.tsx"),
] satisfies RouteConfig;

