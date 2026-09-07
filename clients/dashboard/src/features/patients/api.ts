import { apiFetch } from '@/lib/apiFetch';
import { queryString, type PagedResponse } from '@/lib/paging';

export type PatientSex = 'Unknown' | 'Female' | 'Male' | 'Other';

export interface Patient {
  readonly id: string;
  readonly chartNumber: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly fullName: string;
  readonly dateOfBirth: string;
  readonly sex: PatientSex;
  readonly email: string | null;
  readonly phoneNumber: string | null;
  readonly allergies: readonly string[];
  readonly hasMarketingConsent: boolean;
  readonly hasReminderConsent: boolean;
  readonly isActive: boolean;
}

export interface RegisterPatientRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly dateOfBirth: string;
  readonly sex: PatientSex;
  readonly email: string | null;
  readonly phoneNumber: string | null;
  readonly preferredProviderId: string | null;
  readonly allergies: readonly string[];
  readonly hasMarketingConsent: boolean;
  readonly hasReminderConsent: boolean;
}

export function searchPatients(
  searchTerm: string,
  pageNumber: number,
): Promise<PagedResponse<Patient>> {
  return apiFetch<PagedResponse<Patient>>(
    `/patients${queryString({ searchTerm, pageNumber, pageSize: 20 })}`,
  );
}

export function getPatient(patientId: string): Promise<Patient> {
  return apiFetch<Patient>(`/patients/${patientId}`);
}

/** Registration is idempotent: a double-tapped button must not create the patient twice. */
export function registerPatient(
  request: RegisterPatientRequest,
  idempotencyKey: string,
): Promise<Patient> {
  return apiFetch<Patient>('/patients', { method: 'POST', body: request, idempotencyKey });
}

export function updateConsent(
  patientId: string,
  consent: { hasMarketingConsent: boolean; hasReminderConsent: boolean },
): Promise<Patient> {
  return apiFetch<Patient>(`/patients/${patientId}/consent`, { method: 'PUT', body: consent });
}
