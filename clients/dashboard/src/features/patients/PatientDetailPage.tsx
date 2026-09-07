import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { useAuth } from '@/auth/AuthProvider';
import { ErrorState } from '@/components/ErrorState';
import { getPatient, updateConsent } from './api';

/** One patient's record, and the consent switches the practice is answerable for. */
export function PatientDetailPage() {
  const { patientId = '' } = useParams();
  const { can } = useAuth();
  const queryClient = useQueryClient();

  const patient = useQuery({
    queryKey: ['patients', patientId],
    queryFn: () => getPatient(patientId),
    enabled: patientId.length > 0,
  });

  const consent = useMutation({
    mutationFn: (next: { hasMarketingConsent: boolean; hasReminderConsent: boolean }) =>
      updateConsent(patientId, next),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['patients'] });
    },
  });

  if (patient.error !== null) {
    return <ErrorState error={patient.error} />;
  }

  if (!patient.data) {
    return <p className="text-muted">Loading…</p>;
  }

  const record = patient.data;
  const editable = can('Permissions.Patients.Update');

  return (
    <section className="flex flex-col gap-4">
      <header>
        <h1 className="text-lg font-semibold">{record.fullName}</h1>
        <p className="text-sm text-muted">
          {record.chartNumber} · born {record.dateOfBirth} · {record.sex}
        </p>
      </header>

      <dl className="card grid gap-3 p-4 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-muted">Email</dt>
          <dd>{record.email ?? '—'}</dd>
        </div>
        <div>
          <dt className="text-muted">Phone</dt>
          <dd>{record.phoneNumber ?? '—'}</dd>
        </div>
        <div className="sm:col-span-2">
          <dt className="text-muted">Allergies</dt>
          <dd>{record.allergies.length > 0 ? record.allergies.join(', ') : 'None recorded'}</dd>
        </div>
      </dl>

      <div className="card flex flex-col gap-3 p-4">
        <h2 className="font-medium">Consent</h2>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={record.hasReminderConsent}
            disabled={!editable || consent.isPending}
            onChange={(event) =>
              consent.mutate({
                hasMarketingConsent: record.hasMarketingConsent,
                hasReminderConsent: event.target.checked,
              })
            }
          />
          Appointment reminders
        </label>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={record.hasMarketingConsent}
            disabled={!editable || consent.isPending}
            onChange={(event) =>
              consent.mutate({
                hasMarketingConsent: event.target.checked,
                hasReminderConsent: record.hasReminderConsent,
              })
            }
          />
          Marketing contact
        </label>

        <p className="text-xs text-muted">
          Withdrawing consent reaches the notifications service through an event; a reminder already
          queued for tonight will not be sent.
        </p>

        {consent.error !== null && <ErrorState error={consent.error} />}
      </div>
    </section>
  );
}
