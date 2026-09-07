import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router';
import { useAuth } from '@/auth/AuthProvider';
import { ErrorState } from '@/components/ErrorState';
import { Pager } from '@/components/Pager';
import { checked, text } from '@/lib/forms';
import { registerPatient, searchPatients, type PatientSex } from './api';

const sexes: readonly PatientSex[] = ['Unknown', 'Female', 'Male', 'Other'];

/** The practice's list of patients, and the form that registers one. */
export function PatientsPage() {
  const { can } = useAuth();
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [showForm, setShowForm] = useState(false);

  const patients = useQuery({
    queryKey: ['patients', searchTerm, pageNumber],
    queryFn: () => searchPatients(searchTerm, pageNumber),
  });

  const register = useMutation({
    mutationFn: (form: FormData) =>
      registerPatient(
        {
          firstName: text(form, 'firstName').trim(),
          lastName: text(form, 'lastName').trim(),
          dateOfBirth: text(form, 'dateOfBirth'),
          sex: text(form, 'sex', 'Unknown') as PatientSex,
          email: text(form, 'email').trim() || null,
          phoneNumber: text(form, 'phoneNumber').trim() || null,
          preferredProviderId: null,
          allergies: [],
          hasMarketingConsent: checked(form, 'hasMarketingConsent'),
          hasReminderConsent: checked(form, 'hasReminderConsent'),
        },
        crypto.randomUUID(),
      ),
    onSuccess: async () => {
      setShowForm(false);
      await queryClient.invalidateQueries({ queryKey: ['patients'] });
    },
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    register.mutate(new FormData(event.currentTarget));
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold">Patients</h1>

        <input
          className="field max-w-xs"
          aria-label="Search patients"
          placeholder="Name or chart number"
          value={searchTerm}
          onChange={(event) => {
            setSearchTerm(event.target.value);
            setPageNumber(1);
          }}
        />

        {can('Permissions.Patients.Create') && (
          <button type="button" className="btn-primary ml-auto" onClick={() => setShowForm((open) => !open)}>
            {showForm ? 'Cancel' : 'Register a patient'}
          </button>
        )}
      </div>

      {showForm && (
        <form className="card grid gap-3 p-4 sm:grid-cols-2" onSubmit={submit}>
          <div>
            <label className="label" htmlFor="firstName">
              First name
            </label>
            <input id="firstName" name="firstName" className="field" required />
          </div>

          <div>
            <label className="label" htmlFor="lastName">
              Last name
            </label>
            <input id="lastName" name="lastName" className="field" required />
          </div>

          <div>
            <label className="label" htmlFor="dateOfBirth">
              Date of birth
            </label>
            <input id="dateOfBirth" name="dateOfBirth" type="date" className="field" required />
          </div>

          <div>
            <label className="label" htmlFor="sex">
              Sex
            </label>
            <select id="sex" name="sex" className="field" defaultValue="Unknown">
              {sexes.map((sex) => (
                <option key={sex} value={sex}>
                  {sex}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="label" htmlFor="email">
              Email
            </label>
            <input id="email" name="email" type="email" className="field" />
          </div>

          <div>
            <label className="label" htmlFor="phoneNumber">
              Phone
            </label>
            <input id="phoneNumber" name="phoneNumber" className="field" placeholder="+15551234567" />
          </div>

          <p className="text-xs text-muted sm:col-span-2">
            One of email or phone is required: a practice has to be able to reach the patient.
          </p>

          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" name="hasReminderConsent" defaultChecked />
            Appointment reminders
          </label>

          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" name="hasMarketingConsent" />
            Marketing contact
          </label>

          {register.error !== null && (
            <div className="sm:col-span-2">
              <ErrorState error={register.error} />
            </div>
          )}

          <div className="sm:col-span-2">
            <button className="btn-primary" type="submit" disabled={register.isPending}>
              {register.isPending ? 'Registering…' : 'Register patient'}
            </button>
          </div>
        </form>
      )}

      {patients.error !== null && <ErrorState error={patients.error} />}

      {patients.data && (
        <>
          <table className="card w-full text-sm">
            <thead className="text-left text-muted">
              <tr>
                <th className="p-3 font-medium">Chart</th>
                <th className="p-3 font-medium">Patient</th>
                <th className="p-3 font-medium">Date of birth</th>
                <th className="p-3 font-medium">Contact</th>
              </tr>
            </thead>
            <tbody>
              {patients.data.items.map((patient) => (
                <tr key={patient.id} className="border-t border-border">
                  <td className="p-3 font-mono text-xs text-muted">{patient.chartNumber}</td>
                  <td className="p-3">
                    <Link className="underline underline-offset-2" to={`/patients/${patient.id}`}>
                      {patient.fullName}
                    </Link>
                  </td>
                  <td className="p-3 text-muted">{patient.dateOfBirth}</td>
                  <td className="p-3 text-muted">{patient.email ?? patient.phoneNumber ?? '—'}</td>
                </tr>
              ))}

              {patients.data.items.length === 0 && (
                <tr className="border-t border-border">
                  <td className="p-6 text-center text-muted" colSpan={4}>
                    No patient matches that search.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <Pager page={patients.data} onChange={setPageNumber} />
        </>
      )}
    </section>
  );
}
