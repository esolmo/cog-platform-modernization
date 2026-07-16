'use strict';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { awardFreePlay, getCustomerFreePlay } from '../../api/accountsApi';
import { useAuthStore } from '../../stores/authStore';
import type { Customer } from '../../types/accounts';

const awardSchema = z.object({
  amount:      z.coerce.number().positive('Amount must be positive'),
  description: z.string().min(1, 'Description is required'),
  expiresAt:   z.string().optional(),
});
type AwardFields = z.infer<typeof awardSchema>;

const fmtMoney = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

const fmtDate = (s: string) =>
  new Date(s).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit' });

export default function FreePlayTab({ customer }: { customer: Customer }) {
  const loginName = useAuthStore((s) => s.loginName) ?? 'system';
  const qc = useQueryClient();

  const { data: history, isLoading } = useQuery({
    queryKey: ['customer-free-play', customer.id],
    queryFn:  () => getCustomerFreePlay(customer.id),
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<AwardFields>({
    resolver: zodResolver(awardSchema),
    defaultValues: { amount: 0, description: '', expiresAt: '' },
  });

  const mutation = useMutation({
    mutationFn: (d: AwardFields) =>
      awardFreePlay(customer.id, {
        amount:      d.amount,
        description: d.description,
        expiresAt:   d.expiresAt || undefined,
        issuedBy:    loginName,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customer-free-play', customer.id] });
      qc.invalidateQueries({ queryKey: ['customer', customer.id] });
      reset({ amount: 0, description: '', expiresAt: '' });
    },
  });

  const totalActive = (history ?? [])
    .filter((fp) => !fp.isRedeemed)
    .reduce((sum, fp) => sum + fp.amount, 0);

  return (
    <div className="space-y-6">
      {/* Summary */}
      <div className="grid grid-cols-2 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-xs text-gray-500 mb-1">Free Play Balance</p>
          <p className="text-xl font-semibold text-gray-800">{fmtMoney(customer.balance.freePlayBalance)}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-xs text-gray-500 mb-1">Active Awards</p>
          <p className="text-xl font-semibold text-gray-800">{fmtMoney(totalActive)}</p>
        </div>
      </div>

      {/* Award form */}
      <div className="bg-white rounded-lg shadow p-5">
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Award Free Play</h3>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Amount</label>
              <input {...register('amount')} type="number" step="0.01" min="0.01"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
              {errors.amount && <p className="text-xs text-red-500 mt-0.5">{errors.amount.message}</p>}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Description</label>
              <input {...register('description')} placeholder="Promo, bonus, etc."
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
              {errors.description && <p className="text-xs text-red-500 mt-0.5">{errors.description.message}</p>}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Expires At (optional)</label>
              <input {...register('expiresAt')} type="datetime-local"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
            </div>
          </div>
          {mutation.isError   && <p className="text-xs text-red-500">{String(mutation.error)}</p>}
          {mutation.isSuccess && <p className="text-xs text-green-600">Free play awarded.</p>}
          <button type="submit" disabled={mutation.isPending}
            className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
            {mutation.isPending ? 'Awarding…' : 'Award Free Play'}
          </button>
        </form>
      </div>

      {/* History */}
      <div>
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Award History</h3>
        {isLoading && <p className="text-sm text-gray-500">Loading…</p>}
        {!isLoading && (!history || history.length === 0) && (
          <p className="text-sm text-gray-500 text-center py-6">No free play awards found.</p>
        )}
        {history && history.length > 0 && (
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Amount</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Issued</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Expires</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {history.map((fp) => (
                  <tr key={fp.id}>
                    <td className="px-4 py-3 text-sm text-gray-700">{fp.description}</td>
                    <td className="px-4 py-3 text-sm text-right font-medium text-gray-800">{fmtMoney(fp.amount)}</td>
                    <td className="px-4 py-3 text-sm text-gray-500">{fmtDate(fp.issuedAt)} by {fp.issuedBy}</td>
                    <td className="px-4 py-3 text-sm text-gray-500">{fp.expiresAt ? fmtDate(fp.expiresAt) : '—'}</td>
                    <td className="px-4 py-3 text-center">
                      {fp.isRedeemed ? (
                        <span className="px-2 py-0.5 rounded-full text-xs bg-gray-100 text-gray-500">
                          Redeemed {fmtMoney(fp.redeemedAmount)}
                        </span>
                      ) : (
                        <span className="px-2 py-0.5 rounded-full text-xs bg-green-100 text-green-700">Active</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
