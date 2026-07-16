'use strict';
import { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useForm, useFieldArray, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { createBatchTransactions } from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import type { BatchTransactionLineResult } from '../types/accounts';

const TRANSACTION_TYPES = [
  'Wire', 'Cash', 'Check', 'BankTransfer', 'FreePlay',
  'CreditAdjustment', 'WagerSettlement', 'Reversal', 'CasinoAdjustment', 'ManualCorrection',
] as const;

const rowSchema = z.object({
  customerId:    z.coerce.number().min(1, 'Required'),
  code:          z.enum(['Credit', 'Debit']),
  type:          z.string().min(1, 'Required'),
  amount:        z.coerce.number().positive('Must be > 0'),
  description:   z.string().optional(),
  reference:     z.string().optional(),
  paymentMethod: z.string().optional(),
});

const formSchema = z.object({
  stopOnFirstError: z.boolean(),
  rows: z.array(rowSchema).min(1),
});

type FormValues = z.infer<typeof formSchema>;

const emptyRow = (): FormValues['rows'][number] => ({
  customerId:    0,
  code:          'Credit',
  type:          'Cash',
  amount:        0,
  description:   '',
  reference:     '',
  paymentMethod: '',
});

interface RowResult {
  index: number;
  result: BatchTransactionLineResult;
}

export default function BatchTransactionsPage() {
  const loginName = useAuthStore((s) => s.loginName);
  const [submitted, setSubmitted] = useState(false);
  const [rowResults, setRowResults] = useState<RowResult[]>([]);
  const [summary, setSummary] = useState<{ succeeded: number; failed: number } | null>(null);

  const {
    register,
    control,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { stopOnFirstError: false, rows: [emptyRow()] },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'rows' });
  const rows = watch('rows');

  const mutation = useMutation({
    mutationFn: createBatchTransactions,
    onSuccess: (data) => {
      setSubmitted(true);
      setSummary({ succeeded: data.succeeded, failed: data.failed });
      setRowResults(data.results.map((r) => ({ index: r.index, result: r })));
    },
  });

  const onSubmit = useCallback(
    (values: FormValues) => {
      setSubmitted(false);
      setRowResults([]);
      mutation.mutate({
        stopOnFirstError: values.stopOnFirstError,
        transactions: values.rows.map((r) => ({
          customerId:    r.customerId,
          code:          r.code,
          type:          r.type,
          amount:        r.amount,
          description:   r.description || undefined,
          reference:     r.reference || undefined,
          paymentMethod: r.paymentMethod || undefined,
          enteredBy:     loginName ?? 'unknown',
        })),
      });
    },
    [mutation, loginName],
  );

  const getRowResult = (i: number) => rowResults.find((r) => r.index === i)?.result;

  const totalDebit  = rows.reduce((s, r) => s + (r.code === 'Debit'  ? (Number(r.amount) || 0) : 0), 0);
  const totalCredit = rows.reduce((s, r) => s + (r.code === 'Credit' ? (Number(r.amount) || 0) : 0), 0);

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Batch Transaction Entry</h1>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-green-400">
            Total Credits: <span className="font-mono font-semibold">${totalCredit.toFixed(2)}</span>
          </span>
          <span className="text-red-400">
            Total Debits: <span className="font-mono font-semibold">${totalDebit.toFixed(2)}</span>
          </span>
        </div>
      </div>

      {submitted && summary && (
        <div className={`rounded-lg px-4 py-3 text-sm font-medium ${
          summary.failed === 0
            ? 'bg-green-900/40 border border-green-700 text-green-300'
            : summary.succeeded === 0
            ? 'bg-red-900/40 border border-red-700 text-red-300'
            : 'bg-yellow-900/40 border border-yellow-700 text-yellow-300'
        }`}>
          Batch complete — {summary.succeeded} succeeded, {summary.failed} failed
          {summary.failed > 0 && (
            <span className="ml-2 text-xs opacity-75">
              (failed rows shown in red below)
            </span>
          )}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="overflow-x-auto rounded-lg border border-gray-700">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-800 text-gray-400 text-left">
                <th className="px-3 py-2 w-8">#</th>
                <th className="px-3 py-2">Customer ID</th>
                <th className="px-3 py-2">Code</th>
                <th className="px-3 py-2">Type</th>
                <th className="px-3 py-2">Amount</th>
                <th className="px-3 py-2">Description</th>
                <th className="px-3 py-2">Reference</th>
                <th className="px-3 py-2">Payment Method</th>
                <th className="px-3 py-2 w-8">Status</th>
                <th className="px-3 py-2 w-8"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-700">
              {fields.map((field, i) => {
                const rowResult = getRowResult(i);
                const rowError  = errors.rows?.[i];
                const rowBg = rowResult
                  ? rowResult.isSuccess
                    ? 'bg-green-950/40'
                    : 'bg-red-950/40'
                  : 'bg-gray-900/60';

                return (
                  <tr key={field.id} className={rowBg}>
                    <td className="px-3 py-1 text-gray-500 text-xs">{i + 1}</td>

                    <td className="px-2 py-1">
                      <input
                        type="number"
                        {...register(`rows.${i}.customerId`)}
                        placeholder="ID"
                        className={`w-20 bg-gray-800 border rounded px-2 py-1 text-white text-xs focus:outline-none focus:ring-1 focus:ring-indigo-500 ${
                          rowError?.customerId ? 'border-red-500' : 'border-gray-600'
                        }`}
                      />
                    </td>

                    <td className="px-2 py-1">
                      <Controller
                        name={`rows.${i}.code`}
                        control={control}
                        render={({ field: f }) => (
                          <select
                            {...f}
                            className="bg-gray-800 border border-gray-600 rounded px-2 py-1 text-xs text-white focus:outline-none focus:ring-1 focus:ring-indigo-500"
                          >
                            <option value="Credit">Credit</option>
                            <option value="Debit">Debit</option>
                          </select>
                        )}
                      />
                    </td>

                    <td className="px-2 py-1">
                      <select
                        {...register(`rows.${i}.type`)}
                        className={`bg-gray-800 border rounded px-2 py-1 text-xs text-white focus:outline-none focus:ring-1 focus:ring-indigo-500 ${
                          rowError?.type ? 'border-red-500' : 'border-gray-600'
                        }`}
                      >
                        {TRANSACTION_TYPES.map((t) => (
                          <option key={t} value={t}>{t}</option>
                        ))}
                      </select>
                    </td>

                    <td className="px-2 py-1">
                      <input
                        type="number"
                        step="0.01"
                        min="0.01"
                        {...register(`rows.${i}.amount`)}
                        placeholder="0.00"
                        className={`w-24 bg-gray-800 border rounded px-2 py-1 text-white text-xs font-mono focus:outline-none focus:ring-1 focus:ring-indigo-500 ${
                          rowError?.amount ? 'border-red-500' : 'border-gray-600'
                        }`}
                      />
                    </td>

                    <td className="px-2 py-1">
                      <input
                        type="text"
                        {...register(`rows.${i}.description`)}
                        placeholder="Description"
                        className="w-36 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-white text-xs focus:outline-none focus:ring-1 focus:ring-indigo-500"
                      />
                    </td>

                    <td className="px-2 py-1">
                      <input
                        type="text"
                        {...register(`rows.${i}.reference`)}
                        placeholder="Ref #"
                        className="w-24 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-white text-xs focus:outline-none focus:ring-1 focus:ring-indigo-500"
                      />
                    </td>

                    <td className="px-2 py-1">
                      <input
                        type="text"
                        {...register(`rows.${i}.paymentMethod`)}
                        placeholder="e.g. Wire"
                        className="w-24 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-white text-xs focus:outline-none focus:ring-1 focus:ring-indigo-500"
                      />
                    </td>

                    <td className="px-2 py-1 text-center">
                      {rowResult ? (
                        rowResult.isSuccess ? (
                          <span className="text-green-400 text-xs">✓</span>
                        ) : (
                          <span
                            className="text-red-400 text-xs cursor-help"
                            title={rowResult.error ?? ''}
                          >
                            ✗
                          </span>
                        )
                      ) : null}
                    </td>

                    <td className="px-2 py-1">
                      {fields.length > 1 && (
                        <button
                          type="button"
                          onClick={() => remove(i)}
                          className="text-gray-500 hover:text-red-400 text-lg leading-none"
                        >
                          ×
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Failed row error details */}
        {submitted && rowResults.some((r) => !r.result.isSuccess) && (
          <div className="space-y-1">
            {rowResults
              .filter((r) => !r.result.isSuccess)
              .map((r) => (
                <div key={r.index} className="text-xs text-red-400">
                  Row {r.index + 1}: {r.result.error} ({r.result.errorCode})
                </div>
              ))}
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <button
              type="button"
              onClick={() => append(emptyRow())}
              className="text-sm text-indigo-400 hover:text-indigo-300 font-medium"
            >
              + Add Row
            </button>
            <button
              type="button"
              onClick={() => {
                for (let i = 0; i < 5; i++) append(emptyRow());
              }}
              className="text-sm text-gray-400 hover:text-gray-300"
            >
              + Add 5 Rows
            </button>
            <label className="flex items-center gap-2 text-sm text-gray-400 cursor-pointer select-none">
              <input
                type="checkbox"
                {...register('stopOnFirstError')}
                className="accent-indigo-500"
              />
              Stop on first error
            </label>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => {
                reset({ stopOnFirstError: false, rows: [emptyRow()] });
                setSubmitted(false);
                setRowResults([]);
                setSummary(null);
              }}
              className="px-4 py-2 text-sm rounded bg-gray-700 hover:bg-gray-600 text-white"
            >
              Clear
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="px-5 py-2 text-sm rounded bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white font-medium"
            >
              {mutation.isPending ? 'Submitting…' : `Submit ${fields.length} Transaction${fields.length !== 1 ? 's' : ''}`}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}
