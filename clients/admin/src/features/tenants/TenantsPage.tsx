import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router';
import { useAuth } from '@/auth/AuthProvider';
import { ErrorState } from '@/components/ErrorState';
import { Pager } from '@/components/Pager';
import { text } from '@/lib/forms';
import { createTenant, searchTenants } from './api';

const plans = ['solo', 'practice', 'group'] as const;

/** Every practice on the platform, and the form that adds one. */
export function TenantsPage() {
  const { can } = useAuth();
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [showForm, setShowForm] = useState(false);

  const tenants = useQuery({
    queryKey: ['tenants', searchTerm, pageNumber],
    queryFn: () => searchTenants(searchTerm, pageNumber),
  });

  const create = useMutation({
    mutationFn: (form: FormData) =>
      createTenant(
        {
          identifier: text(form, 'identifier').trim(),
          name: text(form, 'name').trim(),
          adminEmail: text(form, 'adminEmail').trim(),
          plan: text(form, 'plan', 'solo'),
          timeZone: text(form, 'timeZone', 'Europe/London'),
          validUntil: null,
        },
        // One key per submitted form, so a retry of THIS submission replays rather than repeats.
        crypto.randomUUID(),
      ),
    onSuccess: async () => {
      setShowForm(false);
      await queryClient.invalidateQueries({ queryKey: ['tenants'] });
    },
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    create.mutate(new FormData(event.currentTarget));
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold">Practices</h1>

        <input
          className="field max-w-xs"
          placeholder="Search by name or identifier"
          aria-label="Search practices"
          value={searchTerm}
          onChange={(event) => {
            setSearchTerm(event.target.value);
            setPageNumber(1);
          }}
        />

        {can('Permissions.Tenants.Create') && (
          <button type="button" className="btn-primary ml-auto" onClick={() => setShowForm((open) => !open)}>
            {showForm ? 'Cancel' : 'Add a practice'}
          </button>
        )}
      </div>

      {showForm && (
        <form className="card grid gap-3 p-4 sm:grid-cols-2" onSubmit={submit}>
          <div>
            <label className="label" htmlFor="identifier">
              Identifier
            </label>
            <input id="identifier" name="identifier" className="field" required placeholder="smile-dental" />
          </div>

          <div>
            <label className="label" htmlFor="name">
              Name
            </label>
            <input id="name" name="name" className="field" required placeholder="Smile Dental" />
          </div>

          <div>
            <label className="label" htmlFor="adminEmail">
              Administrator email
            </label>
            <input id="adminEmail" name="adminEmail" type="email" className="field" required />
          </div>

          <div>
            <label className="label" htmlFor="plan">
              Plan
            </label>
            <select id="plan" name="plan" className="field" defaultValue="solo">
              {plans.map((plan) => (
                <option key={plan} value={plan}>
                  {plan}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="label" htmlFor="timeZone">
              Time zone
            </label>
            <input id="timeZone" name="timeZone" className="field" defaultValue="Europe/London" />
          </div>

          {create.error !== null && (
            <div className="sm:col-span-2">
              <ErrorState error={create.error} />
            </div>
          )}

          <div className="sm:col-span-2">
            <button className="btn-primary" type="submit" disabled={create.isPending}>
              {create.isPending ? 'Provisioning…' : 'Provision practice'}
            </button>
            <p className="mt-2 text-xs text-muted">
              Roles and the first administrator are seeded in the background, moments after the
              practice appears here.
            </p>
          </div>
        </form>
      )}

      {tenants.error !== null && <ErrorState error={tenants.error} />}

      {tenants.data && (
        <>
          <table className="card w-full text-sm">
            <thead className="text-left text-muted">
              <tr>
                <th className="p-3 font-medium">Practice</th>
                <th className="p-3 font-medium">Identifier</th>
                <th className="p-3 font-medium">Plan</th>
                <th className="p-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {tenants.data.items.map((tenant) => (
                <tr key={tenant.id} className="border-t border-border">
                  <td className="p-3">
                    <Link className="underline underline-offset-2" to={`/tenants/${tenant.id}`}>
                      {tenant.name}
                    </Link>
                  </td>
                  <td className="p-3 text-muted">{tenant.identifier}</td>
                  <td className="p-3">{tenant.plan}</td>
                  <td className="p-3">
                    <span className={tenant.isActive ? 'text-success' : 'text-danger'}>
                      {tenant.isActive ? 'Active' : 'Suspended'}
                    </span>
                  </td>
                </tr>
              ))}

              {tenants.data.items.length === 0 && (
                <tr className="border-t border-border">
                  <td className="p-6 text-center text-muted" colSpan={4}>
                    No practice matches that search.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <Pager page={tenants.data} onChange={setPageNumber} />
        </>
      )}
    </section>
  );
}
