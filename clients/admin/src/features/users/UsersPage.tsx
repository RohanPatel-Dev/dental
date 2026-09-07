import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { ErrorState } from '@/components/ErrorState';
import { Pager } from '@/components/Pager';
import { apiFetch } from '@/lib/apiFetch';
import { queryString, type PagedResponse } from '@/lib/paging';

interface UserSummary {
  readonly id: string;
  readonly email: string;
  readonly fullName: string;
  readonly isActive: boolean;
  readonly roles: readonly string[];
}

function searchUsers(searchTerm: string, pageNumber: number): Promise<PagedResponse<UserSummary>> {
  return apiFetch<PagedResponse<UserSummary>>(
    `/users${queryString({ searchTerm, pageNumber, pageSize: 20 })}`,
  );
}

/** The operator tenant's own staff. A practice's staff is administered from the practice app. */
export function UsersPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [pageNumber, setPageNumber] = useState(1);

  const users = useQuery({
    queryKey: ['users', searchTerm, pageNumber],
    queryFn: () => searchUsers(searchTerm, pageNumber),
  });

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold">Operators</h1>
        <input
          className="field max-w-xs"
          aria-label="Search operators"
          placeholder="Search by name or email"
          value={searchTerm}
          onChange={(event) => {
            setSearchTerm(event.target.value);
            setPageNumber(1);
          }}
        />
      </div>

      {users.error !== null && <ErrorState error={users.error} />}

      {users.data && (
        <>
          <ul className="card divide-y divide-border">
            {users.data.items.map((user) => (
              <li key={user.id} className="flex items-center gap-3 p-3 text-sm">
                <span className="font-medium">{user.fullName}</span>
                <span className="text-muted">{user.email}</span>
                <span className="ml-auto text-muted">{user.roles.join(', ')}</span>
                {!user.isActive && <span className="text-danger">Disabled</span>}
              </li>
            ))}

            {users.data.items.length === 0 && (
              <li className="p-6 text-center text-muted">Nobody matches that search.</li>
            )}
          </ul>

          <Pager page={users.data} onChange={setPageNumber} />
        </>
      )}
    </section>
  );
}
