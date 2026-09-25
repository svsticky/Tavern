/** Entries expire so other users' changes show up; the user's own changes invalidate explicitly. */
import type { AxiosInstance } from "axios";

type Entry = { value: unknown; expiresAt: number };

const DEFAULT_TTL_MS = 60_000;

const cache = new Map<string, Entry>();
/** Bumped on invalidation so an in-flight fetch can't write stale data back. */
const versions = new Map<string, number>();

export const ACTIVITIES_CACHE_KEY = "upcoming-activities";
export const ANNOUNCEMENTS_CACHE_KEY = "announcements";
/** Stored per user as `${key}:${userId}`. */
export const ENROLLED_ACTIVITIES_CACHE_KEY = "enrolled-activities";
/** Stored per user as `${key}:${userId}`. */
export const GROUP_MEMBERSHIPS_CACHE_KEY = "group-memberships";

const belongsTo = (key: string, base: string) =>
  key === base || key.startsWith(`${base}:`);

const versionOf = (key: string) => {
  let total = 0;
  for (const [base, count] of versions) {
    if (belongsTo(key, base)) total += count;
  }
  return total;
};

export function getCachedResource<T>(key: string): T | undefined {
  const entry = cache.get(key);
  if (!entry) return undefined;
  if (entry.expiresAt <= Date.now()) {
    cache.delete(key);
    return undefined;
  }
  return entry.value as T;
}

export function setCachedResource<T>(
  key: string,
  value: T,
  ttlMs = DEFAULT_TTL_MS,
) {
  cache.set(key, { value, expiresAt: Date.now() + ttlMs });
}

export function invalidateCachedResource(...bases: string[]) {
  for (const base of bases) {
    versions.set(base, (versions.get(base) ?? 0) + 1);
    for (const key of [...cache.keys()]) {
      if (belongsTo(key, base)) cache.delete(key);
    }
  }
}

export function clearResourceCache() {
  cache.clear();
  versions.clear();
}

export async function cachedResource<T>(
  key: string,
  fetcher: () => Promise<T>,
): Promise<T> {
  const hit = getCachedResource<T>(key);
  if (hit !== undefined) return hit;

  const versionBefore = versionOf(key);

  const value = await fetcher();

  // Skip caching if the key was invalidated mid-flight (e.g. the user just enrolled).
  if (versionOf(key) === versionBefore) {
    setCachedResource(key, value);
  }
  return value;
}

const MUTATION_INVALIDATIONS: { prefix: string; keys: string[] }[] = [
  // Enrollments change the participant counts on activity tiles.
  {
    prefix: "activities",
    keys: [ACTIVITIES_CACHE_KEY, ENROLLED_ACTIVITIES_CACHE_KEY],
  },
  {
    prefix: "enrollments",
    keys: [ACTIVITIES_CACHE_KEY, ENROLLED_ACTIVITIES_CACHE_KEY],
  },
  { prefix: "announcements", keys: [ANNOUNCEMENTS_CACHE_KEY] },
  { prefix: "groups", keys: [GROUP_MEMBERSHIPS_CACHE_KEY] },
  { prefix: "groupmemberships", keys: [GROUP_MEMBERSHIPS_CACHE_KEY] },
];

const MUTATING_METHODS = new Set(["post", "put", "patch", "delete"]);

/** Called for every response by the axios client, so no call site has to remember to invalidate. */
export function invalidateCacheForMutation(
  method: string | undefined,
  url: string | undefined,
) {
  if (!method || !url || !MUTATING_METHODS.has(method.toLowerCase())) return;

  const segment = url.replace(/^\/+/, "").split(/[/?#]/)[0].toLowerCase();
  for (const { prefix, keys } of MUTATION_INVALIDATIONS) {
    if (segment === prefix) invalidateCachedResource(...keys);
  }
}

/** Failed requests don't match, so a rejected mutation leaves the cache alone. */
export function installCacheInvalidation(instance: AxiosInstance) {
  return instance.interceptors.response.use((response) => {
    invalidateCacheForMutation(response.config.method, response.config.url);
    return response;
  });
}
