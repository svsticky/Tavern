import {
  index,
  layout,
  type RouteConfig,
  route,
} from "@react-router/dev/routes";

export default [
  layout("layout/auth-service.tsx", [
    layout("layout/authenticated.tsx", [
      layout("layout/navbar.tsx", [
        layout("layout/paywall.tsx", [
          index("routes/home/home.tsx"),
          route("sign_in", "routes/home/home.tsx", { id: "sign-in" }),
          route("announcements", "routes/announcements/announcements.tsx"),
          route("activities", "routes/activities/activities.tsx"),
          route("activities/create", "routes/edit-activity/edit-activity.tsx", {
            id: "create-activity",
          }),
          route(
            "activities/edit/:id",
            "routes/edit-activity/edit-activity.tsx",
            {
              id: "edit-activity",
            },
          ),
          route("activities/:id", "routes/activity/activity.tsx", {
            id: "activity-details",
          }),
          route("account", "routes/account/account.tsx"),
          route("external-links", "routes/external-links.tsx"),
          route(
            "announcements/create",
            "routes/edit-announcement/edit-announcement.tsx",
            { id: "create-announcement" },
          ),
          route(
            "announcements/edit/:id",
            "routes/edit-announcement/edit-announcement.tsx",
            { id: "edit-announcement" },
          ),

          layout("layout/admin.tsx", [
            route("admin/finances", "routes/admin/finances/finances.tsx"),
            route("admin/activities", "routes/admin/activities/activities.tsx"),
            route("admin/activities/:id", "routes/activity/activity.tsx", {
              id: "activity-details-admin",
            }),
            route(
              "admin/activities/create",
              "routes/edit-activity/edit-activity.tsx",
              { id: "create-activity-admin" },
            ),
            route(
              "admin/activities/edit/:id",
              "routes/edit-activity/edit-activity.tsx",
              { id: "edit-activity-admin" },
            ),
            route("admin/members", "routes/admin/members.tsx"),
            route(
              "admin/members/create-member",
              "routes/admin/create-member/create-member.tsx",
            ),
            route(
              "admin/members/:id",
              "routes/admin/edit-member/edit-member.tsx",
            ),
            route("admin/groups", "routes/admin/groups.tsx"),
            route("admin/groups/:id", "routes/admin/edit-group/edit-group.tsx"),
            route("admin/roles", "routes/admin/roles/roles.tsx"),
            route("admin/settings", "routes/admin/settings/settings.tsx"),
          ]),
        ]),
        route("update-account-status", "routes/update-account-status.tsx"),
      ]),
    ]),

    route("confirm-mail", "routes/confirm-mail.tsx"),

    route("login", "routes/auth/login.tsx"),
    route("logout", "routes/auth/logout.tsx"),
  ]),
  route("register", "routes/register.tsx"),
] satisfies RouteConfig;
