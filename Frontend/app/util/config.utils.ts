export const getEnv = (key: string): string | undefined => {
  if (import.meta.env[key]) return import.meta.env[key];

  const envKey = `VITE_${key}`;

  if (typeof window !== "undefined") {
    // @ts-expect-error
    return window._env_?.[envKey] || import.meta.env[envKey];
  }

  return process.env[envKey] || import.meta.env[envKey];
};
