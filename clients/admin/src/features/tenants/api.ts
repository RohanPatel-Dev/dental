import { apiFetch } from '@/lib/apiFetch';
import { queryString, type PagedResponse } from '@/lib/paging';

export interface TenantSummary {
  readonly id: string;
  readonly identifier: string;
  readonly name: string;
  readonly plan: string;
  readonly isActive: boolean;
  readonly timeZone: string;
  readonly validUntil: string | null;
  readonly createdAt: string;
}

export interface CreateTenantRequest {
  readonly identifier: string;
  readonly name: string;
  readonly adminEmail: string;
  readonly plan: string;
  readonly timeZone: string;
  readonly validUntil: string | null;
}

export function searchTenants(
  searchTerm: string,
  pageNumber: number,
): Promise<PagedResponse<TenantSummary>> {
  return apiFetch<PagedResponse<TenantSummary>>(
    `/tenants${queryString({ searchTerm, pageNumber, pageSize: 20 })}`,
  );
}

export function getTenant(tenantId: string): Promise<TenantSummary> {
  return apiFetch<TenantSummary>(`/tenants/${tenantId}`);
}

/**
 * Provisioning is idempotent on the client's key.
 *
 * Creating a practice fans out through the outbox into role and administrator seeding, so a
 * double-submitted form would provision the same practice twice under two identifiers.
 */
export function createTenant(
  request: CreateTenantRequest,
  idempotencyKey: string,
): Promise<TenantSummary> {
  return apiFetch<TenantSummary>('/tenants', {
    method: 'POST',
    body: request,
    idempotencyKey,
  });
}

export function setTenantStatus(tenantId: string, isActive: boolean): Promise<TenantSummary> {
  return apiFetch<TenantSummary>(`/tenants/${tenantId}/status`, {
    method: 'PUT',
    body: { isActive },
  });
}

export function changeTenantPlan(tenantId: string, plan: string): Promise<TenantSummary> {
  return apiFetch<TenantSummary>(`/tenants/${tenantId}/plan`, {
    method: 'PUT',
    body: { plan },
  });
}
