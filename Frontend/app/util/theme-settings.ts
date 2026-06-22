import { getSettingsById } from "~/api";

export const BOARD_THEME_SETTINGS = [
  {
    name: "BoardPrimaryLight",
    cssVar: "--board-primary-light",
    fallback: "#f98f55ff",
  },
  {
    name: "BoardPrimary",
    cssVar: "--board-primary",
    fallback: "#fa6b20",
  },
  {
    name: "BoardPrimaryDark",
    cssVar: "--board-primary-dark",
    fallback: "#ca5617",
  },
] as const;

export const BOARD_THEME_SETTINGS_UPDATED_EVENT =
  "board-theme-settings-updated";

export const loadBoardThemeSettings = async () => {
  const responses = await Promise.allSettled(
    BOARD_THEME_SETTINGS.map(({ name }) =>
      getSettingsById({ path: { id: name } }),
    ),
  );

  let loadedSettingCount = 0;

  responses.forEach((response, index) => {
    const setting = BOARD_THEME_SETTINGS[index];

    if (
      response.status === "fulfilled" &&
      !response.value.error &&
      response.value.data?.value
    ) {
      loadedSettingCount += 1;
      document.documentElement.style.setProperty(
        setting.cssVar,
        response.value.data.value,
      );
      return;
    }

    document.documentElement.style.setProperty(
      setting.cssVar,
      setting.fallback,
    );

    if (response.status === "rejected") {
      console.warn(
        `Failed to load theme setting ${setting.name}.`,
        response.reason,
      );
    }
  });

  return loadedSettingCount;
};
