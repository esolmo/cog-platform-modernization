import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { createTransaction } from '../../api/accountsApi';
import { useAuthStore } from '../../stores/authStore';
import type { PagedResult, Transaction } from '../../types/accounts';

const transactionSchema = z.object({
  code:          z.enum(['Credit', 'Debit']),
  type:          z.string().min(1, 'Type is required'),
  amount:        z.number().positive('Amount must be positive'),
  description:   z.string().optional(),
  reference:     z.string().optional(),
  paymentMethod: z.string().optional(),
});

type TransactionFormData = z.infer<typeof transactionSchema>;

interface TransactionsTabProps {
  customerId:   number;
  transactions: PagedResult<Transaction> | undefined;
}

export default function TransactionsTab({ customerId, transactions }: TransactionsTabProps) {
  const queryClient = useQueryClient();
  const loginName   = useAuthStore((s) => s.loginName) ?? 'unknown';

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: { code: 'Credit', type: 'Cash', amount: 0 },
  });

  const mutation = useMutation({
    mutationFn: (data: TransactionFormData) =>
      createTransaction({ ...data, customerId, enteredBy: loginName }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions', customerId] });
      queryClient.invalidateQueries({ queryKey: ['customer', customerId] });
      reset();
    },
  });

  return (
    <div className="space-y-6">
      {/* New transaction form */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-base font-semibold text-gray-800 mb-4">New Transaction</h2>
        <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="txn-code" className="block text-sm font-medium text-gray-700 mb-1">Code</label>
            <select
              id="txn-code"
              {...register('code')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
            >
              <option value="Credit">Credit</option>
              <option value="Debit">Debit</option>
            </select>
          </div>
          <div>
            <label htmlFor="txn-type" className="block text-sm font-medium text-gray-700 mb-1">Type</label>
            <select
              id="txn-type"
              {...register('type')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
            >
              <option value="Cash">Cash</option>
              <option value="Wire">Wire</option>
              <option value="Check">Check</option>
              <option value="BankTransfer">Bank Transfer</option>
              <option value="FreePlay">Free Play</option>
              <option value="CreditAdjustment">Credit Adjustment</option>
              <option value="ManualCorrection">Manual Correction</option>
            </select>
          </div>
          <div>
            <label htmlFor="txn-amount" className="block text-sm font-medium text-gray-700 mb-1">Amount</label>
            <input
              id="txn-amount"
              {...register('amount', { valueAsNumber: true })}
              type="number"
              step="0.01"
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
            {errors.amount && <p className="mt-1 text-xs text-red-500">{errors.amount.message}</p>}
          </div>
          <div>
            <label htmlFor="txn-reference" className="block text-sm font-medium text-gray-700 mb-1">Reference</label>
            <input
              id="txn-reference"
              {...register('reference')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
          </div>
          <div className="col-span-2">
            <label htmlFor="txn-description" className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <input
              id="txn-description"
              {...register('description')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
          </div>
          {mutation.isError && (
            <p className="col-span-2 text-xs text-red-500">
              {(mutation.error as Error)?.message ?? 'Transaction failed.'}
            </p>
          )}
          <div className="col-span-2">
            <button
              type="submit"
              disabled={mutation.isPending}
              className="bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
            >
              {mutation.isPending ? 'Processing...' : 'Post Transaction'}
            </button>
          </div>
        </form>
      </div>

      {/* Transaction history */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Date</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Code</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Type</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600">Amount</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600">Balance After</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Description</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Reference</th>
              <th className="px-4 py-3 text-center font-medium text-gray-600">Verified</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {transactions?.items.map((t) => (
              <tr key={t.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 text-gray-500">
                  {format(new Date(t.transactionDate), 'PP p')}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`px-2 py-0.5 rounded text-xs font-medium ${
                      t.code === 'Credit'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-700'
                    }`}
                  >
                    {t.code}
                  </span>
                </td>
                <td className="px-4 py-3 text-gray-700">{t.type}</td>
                <td className="px-4 py-3 text-right font-medium">${t.amount.toFixed(2)}</td>
                <td className="px-4 py-3 text-right text-gray-700">
                  ${t.balanceAfter.toFixed(2)}
                </td>
                <td className="px-4 py-3 text-gray-500">{t.description ?? '—'}</td>
                <td className="px-4 py-3 text-gray-500">{t.reference ?? '—'}</td>
                <td className="px-4 py-3 text-center">
                  {t.isVerified ? (
                    <span className="text-green-600 font-medium text-xs">Yes</span>
                  ) : (
                    <span className="text-gray-400 text-xs">No</span>
                  )}
                </td>
              </tr>
            ))}
            {!transactions?.items.length && (
              <tr>
                <td colSpan={8} className="px-4 py-6 text-center text-gray-400 text-sm">
                  No transactions found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
