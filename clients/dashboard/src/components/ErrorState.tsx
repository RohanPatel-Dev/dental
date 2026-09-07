import { ApiError } from '@/lib/problem';

/**
 * One way to show a failure.
 *
 * The correlation identifier is shown deliberately: it is what support asks for, and it is already
 * in the ProblemDetails body, so hiding it just means the user pastes a screenshot instead.
 */
export function ErrorState({ error }: { error: unknown }) {
  const problem = error instanceof ApiError ? error : null;

  return (
    <div role="alert" className="card border-danger/40 p-4">
      <p className="font-medium text-danger">
        {problem?.problem.title ?? 'Something went wrong.'}
      </p>

      {problem?.problem.detail && <p className="mt-1 text-sm text-muted">{problem.problem.detail}</p>}

      {Object.entries(problem?.fieldErrors ?? {}).length > 0 && (
        <ul className="mt-2 list-inside list-disc text-sm text-muted">
          {Object.entries(problem?.fieldErrors ?? {}).map(([field, messages]) => (
            <li key={field}>
              <span className="font-medium">{field}:</span> {messages.join(' ')}
            </li>
          ))}
        </ul>
      )}

      {problem?.correlationId && (
        <p className="mt-2 text-xs text-muted">Reference: {problem.correlationId}</p>
      )}
    </div>
  );
}
