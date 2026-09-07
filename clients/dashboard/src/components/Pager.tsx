import type { PagedResponse } from '@/lib/paging';

/** Page controls driven by the envelope the API already returns. */
export function Pager<T>({
  page,
  onChange,
}: {
  page: PagedResponse<T>;
  onChange: (pageNumber: number) => void;
}) {
  return (
    <div className="flex items-center gap-3 text-sm text-muted">
      <button
        type="button"
        className="btn-ghost"
        disabled={!page.hasPreviousPage}
        onClick={() => onChange(page.pageNumber - 1)}
      >
        Previous
      </button>

      <span>
        Page {page.pageNumber} of {Math.max(page.totalPages, 1)} · {page.totalCount} in total
      </span>

      <button
        type="button"
        className="btn-ghost"
        disabled={!page.hasNextPage}
        onClick={() => onChange(page.pageNumber + 1)}
      >
        Next
      </button>
    </div>
  );
}
