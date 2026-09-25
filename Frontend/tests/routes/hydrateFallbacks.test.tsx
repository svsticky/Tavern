import { render, screen } from "@testing-library/react";
import type { ComponentType } from "react";
import { describe, expect, it } from "vitest";
import { HydrateFallback as activities } from "~/routes/activities/activities";
import { HydrateFallback as activity } from "~/routes/activity/activity";
import { HydrateFallback as adminActivities } from "~/routes/admin/activities/activities";
import { HydrateFallback as editGroup } from "~/routes/admin/edit-group/edit-group";
import { HydrateFallback as editMember } from "~/routes/admin/edit-member/edit-member";
import { HydrateFallback as finances } from "~/routes/admin/finances/finances";
import { HydrateFallback as groups } from "~/routes/admin/groups";
import { HydrateFallback as members } from "~/routes/admin/members";
import { HydrateFallback as settings } from "~/routes/admin/settings/settings";
import { HydrateFallback as announcements } from "~/routes/announcements/announcements";
import { HydrateFallback as editActivity } from "~/routes/edit-activity/edit-activity";
import { HydrateFallback as editAnnouncement } from "~/routes/edit-announcement/edit-announcement";
import { HydrateFallback as externalLinks } from "~/routes/external-links";
import { HydrateFallback as home } from "~/routes/home/home";

const routes: [string, ComponentType][] = [
  ["home", home],
  ["activities", activities],
  ["activity", activity],
  ["announcements", announcements],
  ["external links", externalLinks],
  ["edit activity", editActivity],
  ["edit announcement", editAnnouncement],
  ["admin activities", adminActivities],
  ["admin members", members],
  ["admin groups", groups],
  ["admin finances", finances],
  ["admin settings", settings],
  ["edit member", editMember],
  ["edit group", editGroup],
];

describe("route HydrateFallbacks", () => {
  for (const [name, Fallback] of routes) {
    it(`${name} shows the loading logo while its loader runs`, () => {
      render(<Fallback />);

      expect(screen.getByRole("img", { name: "Loading" })).toBeInTheDocument();
    });
  }
});
