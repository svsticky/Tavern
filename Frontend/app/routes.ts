import {
  index,
  layout,
  type RouteConfig,
  route,
} from "@react-router/dev/routes";

export default [
  layout("layout/keycloak.tsx", [
    layout("layout/authenticated.tsx", [
      layout("layout/navbar.tsx", [
        index("routes/home.tsx"),
        route("announcements", "routes/announcements.tsx"),
        route("activities", "routes/activities.tsx"),
        route("activities/create", "routes/edit-activity.tsx", { id: "create-activity" }),
        route("activities/edit/:id", "routes/edit-activity.tsx", { id: "edit-activity" }),
        route("activities/:id", "routes/activity.tsx"),
        route("settings", "routes/settings.tsx"),
        route("external-links", "routes/external-links.tsx"),
        route("announcements/create", "routes/edit-announcement.tsx", { id: "create-announcement" }),
        route("announcements/edit/:id", "routes/edit-announcement.tsx", { id: "edit-announcement" }),

        route("admin/finances", "routes/admin/finances.tsx"),
        route("admin/activities", "routes/admin/activities.tsx"),
        route("admin/members", "routes/admin/members.tsx"),
        route("admin/members/:id", "routes/admin/edit-member.tsx"),
        route("admin/groups", "routes/admin/groups.tsx"),
        route("admin/groups/:id", "routes/admin/edit-group.tsx"),
      ]),
    ]),
    

    route("login", "routes/auth/login.tsx"),
    route("logout", "routes/auth/logout.tsx"),
  ]),
  route("register", "routes/register.tsx"),
] satisfies RouteConfig;

