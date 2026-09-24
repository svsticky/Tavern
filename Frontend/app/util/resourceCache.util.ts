/**
 * A small in-memory cache for data that several routes fetch identically
 * (e.g. the upcoming activities shown on both the home dashboard and
 * /activities), so moving between those pages doesn't refetch it.
 *
 * Entries expire after `DEFAULT_TTL_MS`, which bounds how stale the data can
 * get from changes made by *other* users. Changes made by the current user
 * are handled explicitly instead: `invalidateCacheForMutation` is wired into
 * the axios client, so any successful create/edit/delete drops the entries it
 * affects - see `MUTATION_INVALIDATIONS`.
 */
import type { AxiosInstance } from "axios";

type Entry = { value: unknown; expiresAt: number };

const DEFAULT_TTL_MS = 60_000;

const cache = new Map<string, Entry>();
/** Bumped on every invalidation, so a fetch that was in flight across one can't write stale data back. */
const versions = new Map<string, number>();

export const ACTIVITIES_CACHE_KEY = "upcoming-activities";
export const ANNOUNCEMENTS_CACHE_KEY = "announcements";
/** Per-user - the full key is `${ENROLLED_ACTIVITIES_CACHE_KEY}:${userId}`. */
export const ENROLLED_ACTIVITIES_CACHE_KEY = "enrolled-activities";
/** Per-user - the full key is `${GROUP_MEMBERSHIPS_CACHE_KEY}:${userId}`. */
export const GROUP_MEMBERSHIPS_CACHE_KEY = "group-memberships";

/** Whether `key` is `base` itself or one of its per-user variants (`base:<id>`). */
const belongsTo = (key: string, base: string) =>
  key === base || key.startsWith(`${base}:`);

/** Total invalidations seen so far for `key` and every base it belongs to. */
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

/** Drops each given key along with its per-user variants. */
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

/**
 * Returns the cached value for `key` if there is a fresh one, otherwise runs
 * `fetcher` and caches its result. A miss always falls through to a real
 * fetch - never to "nothing".
 */
export async function cachedResource<T>(
  key: string,
  fetcher: () => Promise<T>,
): Promise<T> {
  const hit = getCachedResource<T>(key);
  if (hit !== undefined) return hit;

  const versionBefore = versionOf(key);

  const value = await fetcher();

  // If this key (or the base it's a per-user variant of) was invalidated while
  // the request was in flight - say, the user enrolled in something - what
  // came back may predate that change, so don't cache it.
  if (versionOf(key) === versionBefore) {
    setCachedResource(key, value);
  }
  return value;
}

/** Which cache entries a successful mutation under an API path prefix makes stale. */
const MUTATION_INVALIDATIONS: { prefix: string; keys: string[] }[] = [
  // Enrollments change the participant counts shown on every activity tile.
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

/**
 * Drops whatever a successful request made stale. Called for every response
 * by the axios client, so it covers every current and future call site
 * without each one having to remember to invalidate.
 */
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

/**
 * Registers `invalidateCacheForMutation` on every successful response of the
 * given axios instance. Failed requests aren't matched, so a rejected
 * mutation leaves the cache alone.
 */
export function installCacheInvalidation(instance: AxiosInstance) {
  return instance.interceptors.response.use((response) => {
    invalidateCacheForMutation(response.config.method, response.config.url);
    return response;
  });
}
