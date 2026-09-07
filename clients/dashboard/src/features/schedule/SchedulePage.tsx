import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { ErrorState } from '@/components/ErrorState';
import { apiFetch } from '@/lib/apiFetch';
import { queryString, type PagedResponse } from '@/lib/paging';
import { onRealtime } from '@/lib/realtime';

interface Appointment {
  readonly id: string;
  readonly patientId: string;
  readonly patientName: string;
  readonly providerName: string;
  readonly operatoryName: string;
  readonly startsAt: string;
  readonly endsAt: string;
  readonly status: string;
  readonly reason: string | null;
}

function searchAppointments(day: string): Promise<PagedResponse<Appointment>> {
  return apiFetch<PagedResponse<Appointment>>(
    `/appointments${queryString({ from: `${day}T00:00:00Z`, to: `${day}T23:59:59Z`, pageSize: 100 })}`,
  );
}

function time(value: string): string {
  return new Date(value).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}

/** The day's diary. Live, because the front desk and the surgery are looking at the same list. */
export function SchedulePage() {
  const queryClient = useQueryClient();
  const [day, setDay] = useState(() => new Date().toISOString().slice(0, 10));

  const appointments = useQuery({
    queryKey: ['appointments', day],
    queryFn: () => searchAppointments(day),
  });

  // The hub client is imported here, not at module scope: a session that never opens the diary
  // should not pay for SignalR in the entry chunk.
  useEffect(() => {
    const refresh = () => {
      void queryClient.invalidateQueries({ queryKey: ['appointments'] });
    };

    const subscriptions = ['AppointmentBooked', 'AppointmentCancelled', 'AppointmentRescheduled'].map(
      (event) => onRealtime(event, refresh),
    );

    return () => subscriptions.forEach((unsubscribe) => unsubscribe());
  }, [queryClient]);

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold">Diary</h1>
        <input
          type="date"
          className="field max-w-44"
          aria-label="Day"
          value={day}
          onChange={(event) => setDay(event.target.value)}
        />
      </div>

      {appointments.error !== null && <ErrorState error={appointments.error} />}

      {appointments.data && (
        <ul className="card divide-y divide-border">
          {appointments.data.items.map((appointment) => (
            <li key={appointment.id} className="flex items-baseline gap-3 p-3 text-sm">
              <span className="font-mono text-xs text-muted">
                {time(appointment.startsAt)}–{time(appointment.endsAt)}
              </span>
              <span className="font-medium">{appointment.patientName}</span>
              <span className="text-muted">{appointment.providerName}</span>
              <span className="text-muted">{appointment.operatoryName}</span>
              <span className="ml-auto text-muted">{appointment.status}</span>
            </li>
          ))}

          {appointments.data.items.length === 0 && (
            <li className="p-6 text-center text-muted">Nothing booked for this day.</li>
          )}
        </ul>
      )}
    </section>
  );
}
