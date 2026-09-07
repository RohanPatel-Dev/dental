import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { ErrorState } from '@/components/ErrorState';
import { Pager } from '@/components/Pager';
import { apiFetch } from '@/lib/apiFetch';
import { queryString, type PagedResponse } from '@/lib/paging';

interface Invoice {
  readonly id: string;
  readonly invoiceNumber: string;
  readonly patientName: string;
  readonly status: string;
  readonly currency: string;
  readonly total: number;
  readonly amountPaid: number;
  readonly balance: number;
  readonly issuedAt: string | null;
}

function searchInvoices(status: string, pageNumber: number): Promise<PagedResponse<Invoice>> {
  return apiFetch<PagedResponse<Invoice>>(
    `/invoices${queryString({ status, pageNumber, pageSize: 20 })}`,
  );
}

function money(amount: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount);
}

/** The day book: what has been raised, what is still owed. */
export function BillingPage() {
  const [status, setStatus] = useState('');
  const [pageNumber, setPageNumber] = useState(1);

  const invoices = useQuery({
    queryKey: ['invoices', status, pageNumber],
    queryFn: () => searchInvoices(status, pageNumber),
  });

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <h1 className="text-lg font-semibold">Billing</h1>

        <select
          className="field max-w-40"
          aria-label="Status"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value);
            setPageNumber(1);
          }}
        >
          <option value="">All</option>
          <option value="Draft">Draft</option>
          <option value="Issued">Issued</option>
          <option value="PartiallyPaid">Partially paid</option>
          <option value="Paid">Paid</option>
          <option value="Voided">Voided</option>
        </select>
      </div>

      {invoices.error !== null && <ErrorState error={invoices.error} />}

      {invoices.data && (
        <>
          <table className="card w-full text-sm">
            <thead className="text-left text-muted">
              <tr>
                <th className="p-3 font-medium">Invoice</th>
                <th className="p-3 font-medium">Patient</th>
                <th className="p-3 font-medium">Status</th>
                <th className="p-3 text-right font-medium">Total</th>
                <th className="p-3 text-right font-medium">Balance</th>
              </tr>
            </thead>
            <tbody>
              {invoices.data.items.map((invoice) => (
                <tr key={invoice.id} className="border-t border-border">
                  <td className="p-3 font-mono text-xs text-muted">{invoice.invoiceNumber}</td>
                  <td className="p-3">{invoice.patientName}</td>
                  <td className="p-3">{invoice.status}</td>
                  <td className="p-3 text-right">{money(invoice.total, invoice.currency)}</td>
                  <td className="p-3 text-right">
                    <span className={invoice.balance > 0 ? 'text-danger' : 'text-success'}>
                      {money(invoice.balance, invoice.currency)}
                    </span>
                  </td>
                </tr>
              ))}

              {invoices.data.items.length === 0 && (
                <tr className="border-t border-border">
                  <td className="p-6 text-center text-muted" colSpan={5}>
                    Nothing to show.
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          <Pager page={invoices.data} onChange={setPageNumber} />
        </>
      )}
    </section>
  );
}
