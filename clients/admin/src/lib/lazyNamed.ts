import { lazy } from 'react';
import type { ComponentType } from 'react';

/**
 * Code splits a component exported by NAME.
 *
 * React.lazy only understands a default export, so a route pointing at a named export either fails
 * at runtime or forces every page to carry a default export it does not want. This adapts one to
 * the other, and keeps the route table readable.
 */
export function lazyNamed<TModule extends Record<string, unknown>, TKey extends keyof TModule>(
  load: () => Promise<TModule>,
  name: TKey,
): ComponentType<Record<string, never>> {
  return lazy(async () => {
    const module = await load();
    return { default: module[name] as ComponentType<Record<string, never>> };
  });
}
