'use strict';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  updateCreditLimits,
  getCustomerWagerLimits,
  updateCustomerWagerLimits,
  getCustomerCasinoLimits,
  updateCustomerCasinoLimits,
} from '../../api/accountsApi';
import { useAuthStore } from '../../stores/authStore';
import type { Customer } from '../../types/accounts';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const creditSchema = z.object({
  creditLimit:     z.coerce.number().min(0),
  wagerLimit:      z.coerce.number().min(0),
  hardCreditLimit: z.coerce.number().min(0),
});
type CreditFields = z.infer<typeof creditSchema>;

const wagerSchema = z.object({
  maxStraightWager: z.coerce.number().min(0),
  maxParlayWager:   z.coerce.number().min(0),
  maxParlayPayout:  z.coerce.number().min(0),
  maxTeaserWager:   z.coerce.number().min(0),
  maxIfBetWager:    z.coerce.number().min(0),
  maxParlayLegs:    z.coerce.number().int().min(0),
  minimumWager:     z.coerce.number().int().min(0),
});
type WagerFields = z.infer<typeof wagerSchema>;

const casinoSchema = z.object({
  casinoWagerLimit:  z.coerce.number().min(0),
  casinoCreditLimit: z.coerce.number().min(0),
});
type CasinoFields = z.infer<typeof casinoSchema>;

// ─── Shared field component ───────────────────────────────────────────────────

function NumField({
  label,
  name,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  register,
  error,
  step = '0.01',
}: {
  label: string;
  name: string;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  register: any;
  error?: string;
  step?: string;
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
      <input
        {...register(name)}
        type="number"
        step={step}
        className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none"
      />
      {error && <p className="text-xs text-red-500 mt-0.5">{error}</p>}
    </div>
  );
}

// ─── Credit limits section ────────────────────────────────────────────────────

function CreditLimitsSection({ customer, loginName }: { customer: Customer; loginName: string }) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors, isDirty } } = useForm<CreditFields>({
    resolver: zodResolver(creditSchema),
    defaultValues: {
      creditLimit:     customer.balance.creditLimit,
      wagerLimit:      customer.balance.wagerLimit,
      hardCreditLimit: 0,
    },
  });

  const mutation = useMutation({
    mutationFn: (d: CreditFields) =>
      updateCreditLimits(customer.id, { ...d, updatedBy: loginName }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['customer', customer.id] }),
  });

  return (
    <div className="bg-white rounded-lg shadow p-5">
      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Credit Limits</h3>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-3">
        <div className="grid grid-cols-3 gap-3">
          <NumField label="Credit Limit"      name="creditLimit"     register={register} error={errors.creditLimit?.message} />
          <NumField label="Wager Limit"       name="wagerLimit"      register={register} error={errors.wagerLimit?.message} />
          <NumField label="Hard Credit Limit" name="hardCreditLimit" register={register} error={errors.hardCreditLimit?.message} />
        </div>
        {mutation.isError   && <p className="text-xs text-red-500">{String(mutation.error)}</p>}
        {mutation.isSuccess && <p className="text-xs text-green-600">Saved.</p>}
        <button type="submit" disabled={!isDirty || mutation.isPending}
          className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
          {mutation.isPending ? 'Saving…' : 'Save Credit Limits'}
        </button>
      </form>
    </div>
  );
}

// ─── Wager limits section ─────────────────────────────────────────────────────

function WagerLimitsSection({ customerId, loginName }: { customerId: number; loginName: string }) {
  const qc = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['customer-wager-limits', customerId],
    queryFn:  () => getCustomerWagerLimits(customerId),
  });

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<WagerFields>({
    resolver: zodResolver(wagerSchema),
    defaultValues: { maxStraightWager: 0, maxParlayWager: 0, maxParlayPayout: 0, maxTeaserWager: 0, maxIfBetWager: 0, maxParlayLegs: 10, minimumWager: 1 },
  });

  useEffect(() => {
    if (!data) return;
    reset({
      maxStraightWager: data.maxStraightWager,
      maxParlayWager:   data.maxParlayWager,
      maxParlayPayout:  data.maxParlayPayout,
      maxTeaserWager:   data.maxTeaserWager,
      maxIfBetWager:    data.maxIfBetWager,
      maxParlayLegs:    data.maxParlayLegs,
      minimumWager:     data.minimumWager,
    });
  }, [data, reset]);

  const mutation = useMutation({
    mutationFn: (d: WagerFields) =>
      updateCustomerWagerLimits(customerId, { ...d, updatedBy: loginName }),
    onSuccess: (updated) => {
      qc.setQueryData(['customer-wager-limits', customerId], updated);
      reset({ ...updated });
    },
  });

  if (isLoading) return <p className="text-sm text-gray-400">Loading wager limits…</p>;

  return (
    <div className="bg-white rounded-lg shadow p-5">
      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Wager Limits</h3>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-3">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <NumField label="Max Straight"  name="maxStraightWager" register={register} error={errors.maxStraightWager?.message} />
          <NumField label="Max Parlay"    name="maxParlayWager"   register={register} error={errors.maxParlayWager?.message} />
          <NumField label="Max Parlay Payout" name="maxParlayPayout" register={register} error={errors.maxParlayPayout?.message} />
          <NumField label="Max Teaser"    name="maxTeaserWager"   register={register} error={errors.maxTeaserWager?.message} />
          <NumField label="Max If-Bet"    name="maxIfBetWager"    register={register} error={errors.maxIfBetWager?.message} />
          <NumField label="Max Parlay Legs" name="maxParlayLegs" register={register} error={errors.maxParlayLegs?.message} step="1" />
          <NumField label="Minimum Wager" name="minimumWager"    register={register} error={errors.minimumWager?.message} step="1" />
        </div>
        {mutation.isError   && <p className="text-xs text-red-500">{String(mutation.error)}</p>}
        {mutation.isSuccess && <p className="text-xs text-green-600">Saved.</p>}
        <button type="submit" disabled={!isDirty || mutation.isPending}
          className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
          {mutation.isPending ? 'Saving…' : 'Save Wager Limits'}
        </button>
      </form>
    </div>
  );
}

// ─── Casino limits section ────────────────────────────────────────────────────

function CasinoLimitsSection({ customerId, loginName }: { customerId: number; loginName: string }) {
  const qc = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['customer-casino-limits', customerId],
    queryFn:  () => getCustomerCasinoLimits(customerId),
  });

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<CasinoFields>({
    resolver: zodResolver(casinoSchema),
    defaultValues: { casinoWagerLimit: 0, casinoCreditLimit: 0 },
  });

  useEffect(() => {
    if (!data) return;
    reset({ casinoWagerLimit: data.casinoWagerLimit, casinoCreditLimit: data.casinoCreditLimit });
  }, [data, reset]);

  const mutation = useMutation({
    mutationFn: (d: CasinoFields) =>
      updateCustomerCasinoLimits(customerId, { ...d, updatedBy: loginName }),
    onSuccess: (updated) => {
      qc.setQueryData(['customer-casino-limits', customerId], updated);
      reset({ casinoWagerLimit: updated.casinoWagerLimit, casinoCreditLimit: updated.casinoCreditLimit });
    },
  });

  if (isLoading) return <p className="text-sm text-gray-400">Loading casino limits…</p>;

  return (
    <div className="bg-white rounded-lg shadow p-5">
      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Casino Limits</h3>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <NumField label="Casino Wager Limit"  name="casinoWagerLimit"  register={register} error={errors.casinoWagerLimit?.message} />
          <NumField label="Casino Credit Limit" name="casinoCreditLimit" register={register} error={errors.casinoCreditLimit?.message} />
        </div>
        {mutation.isError   && <p className="text-xs text-red-500">{String(mutation.error)}</p>}
        {mutation.isSuccess && <p className="text-xs text-green-600">Saved.</p>}
        <button type="submit" disabled={!isDirty || mutation.isPending}
          className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
          {mutation.isPending ? 'Saving…' : 'Save Casino Limits'}
        </button>
      </form>
    </div>
  );
}

// ─── Limits tab ───────────────────────────────────────────────────────────────

export default function LimitsTab({ customer }: { customer: Customer }) {
  const loginName = useAuthStore((s) => s.loginName) ?? 'system';

  return (
    <div className="space-y-4">
      <CreditLimitsSection customer={customer} loginName={loginName} />
      <WagerLimitsSection  customerId={customer.id} loginName={loginName} />
      <CasinoLimitsSection customerId={customer.id} loginName={loginName} />
    </div>
  );
}
