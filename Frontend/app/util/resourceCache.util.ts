/**
 * A tiny cross-page cache for data multiple routes fetch identically (e.g.
 * upcoming activities shown on both the home page and /activities). Seed
 * state from `getCachedResource`, write results with `setCachedResource`,
 * and invalidate on create/edit/delete with `invalidateCachedResource`.
 */
const resourceCache = new Map<string, unknown>();

export const ACTIVITIES_CACHE_KEY = "upcoming-activities";
export const ANNOUNCEMENTS_CACHE_KEY = "announcements";
export const HOME_ENROLLED_ACTIVITIES_CACHE_KEY = "home-enrolled-activities";
export const HOME_GROUP_MEMBERSHIPS_CACHE_KEY = "home-group-memberships";

export function getCachedResource<T>(key: string): T | undefined {
  return resourceCache.get(key) as T | undefined;
}

export function setCachedResource<T>(key: string, value: T) {
  resourceCache.set(key, value);
}

export function invalidateCachedResource(key: string) {
  resourceCache.delete(key);
}
