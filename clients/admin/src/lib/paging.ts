/** The API's paged envelope, as PagedResponse<T> serialises it. */
export interface PagedResponse<T> {
  readonly items: readonly T[];
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

/** Builds a query string, leaving out anything empty so the API sees its own defaults. */
export function queryString(parameters: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(parameters)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value));
    }
  }

  const rendered = search.toString();
  return rendered.length > 0 ? `?${rendered}` : '';
}
