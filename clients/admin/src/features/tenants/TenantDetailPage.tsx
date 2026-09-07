import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { useAuth } from '@/auth/AuthProvider';
import { ErrorState } from '@/components/ErrorState';
import { changeTenantPlan, getTenant, setTenantStatus } from './api';

const plans = ['solo', 'practice', 'group'] as const;

/** One practice: its plan, and whether it is still allowed to sign in. */
export function TenantDetailPage() {
  const { tenantId = '' } = useParams();
  const { can } = useAuth();
  const queryClient = useQueryClient();

  const tenant = useQuery({
    queryKey: ['tenants', tenantId],
    queryFn: () => getTenant(tenantId),
    enabled: tenantId.length > 0,
  });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['tenants'] });
  };

  const status = useMutation({
    mutationFn: (isActive: boolean) => setTenantStatus(tenantId, isActive),
    onSuccess: refresh,
  });

  const plan = useMutation({
    mutationFn: (next: string) => changeTenantPlan(tenantId, next),
    onSuccess: refresh,
  });

  if (tenant.error !== null) {
    return <ErrorState error={tenant.error} />;
  }

  if (!tenant.data) {
    return <p className="text-muted">Loading…</p>;
  }

  return (
    <section className="flex flex-col gap-4">
      <header>
        <h1 className="text-lg font-semibold">{tenant.data.name}</h1>
        <p className="text-sm text-muted">
          {tenant.data.identifier} · {tenant.data.timeZone}
        </p>
      </header>

      <div className="card flex flex-col gap-4 p-4">
        <div className="flex items-center gap-3">
          <label className="label mb-0" htmlFor="plan">
            Plan
          </label>
          <select
            id="plan"
            className="field max-w-40"
            value={tenant.data.plan}
            disabled={!can('Permissions.Tenants.Update') || plan.isPending}
            onChange={(event) => plan.mutate(event.target.value)}
          >
            {plans.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
          <p className="text-xs text-muted">Quotas follow the plan from the next request onwards.</p>
        </div>

        <div className="flex items-center gap-3">
          <span className="label mb-0">Status</span>
          <span className={tenant.data.isActive ? 'text-success' : 'text-danger'}>
            {tenant.data.isActive ? 'Active' : 'Suspended'}
          </span>

          {can('Permissions.Tenants.Update') && (
            <button
              type="button"
              className="btn-ghost"
              disabled={status.isPending}
              onClick={() => status.mutate(!tenant.data.isActive)}
            >
              {tenant.data.isActive ? 'Suspend' : 'Reactivate'}
            </button>
          )}
        </div>

        {status.error !== null && <ErrorState error={status.error} />}
        {plan.error !== null && <ErrorState error={plan.error} />}
      </div>
    </section>
  );
}
