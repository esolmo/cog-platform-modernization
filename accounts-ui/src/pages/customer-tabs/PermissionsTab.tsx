'use strict';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCustomerPermissions, updateCustomerPermissions } from '../../api/accountsApi';
import { useAuthStore } from '../../stores/authStore';
import type { Customer, UpdateCustomerPermissionsRequest } from '../../types/accounts';

type PermissionKey = keyof Omit<UpdateCustomerPermissionsRequest, 'updatedBy'>;

const SPORT_FLAGS: { key: PermissionKey; label: string }[] = [
  { key: 'webSportsEnabled',  label: 'Web Sports' },
  { key: 'internetEnabled',   label: 'Internet' },
  { key: 'callInEnabled',     label: 'Call-In' },
  { key: 'racebookEnabled',   label: 'Racebook' },
  { key: 'casinoEnabled',     label: 'Casino' },
  { key: 'liveDealerEnabled', label: 'Live Dealer' },
  { key: 'lotteryEnabled',    label: 'Lottery' },
  { key: 'horseEnabled',      label: 'Horse Racing' },
];

const WAGER_FLAGS: { key: PermissionKey; label: string }[] = [
  { key: 'parlayEnabled',   label: 'Parlay' },
  { key: 'teaserEnabled',   label: 'Teaser' },
  { key: 'ifBetEnabled',    label: 'If-Bet' },
  { key: 'reverseEnabled',  label: 'Reverse' },
];

const ACCOUNT_FLAGS: { key: PermissionKey; label: string }[] = [
  { key: 'accountLocked',   label: 'Account Locked' },
  { key: 'receiveAlerts',   label: 'Receive Alerts' },
];

// ─── Toggle component ─────────────────────────────────────────────────────────

function Toggle({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (val: boolean) => void;
}) {
  return (
    <label className="flex items-center justify-between py-2 cursor-pointer">
      <span className="text-sm text-gray-700">{label}</span>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        onClick={() => onChange(!checked)}
        className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors ${
          checked ? 'bg-blue-600' : 'bg-gray-300'
        }`}
      >
        <span
          className={`inline-block h-3.5 w-3.5 rounded-full bg-white shadow transition-transform ${
            checked ? 'translate-x-4' : 'translate-x-1'
          }`}
        />
      </button>
    </label>
  );
}

// ─── Flag group ───────────────────────────────────────────────────────────────

function FlagGroup({
  title,
  flags,
  values,
  onChange,
}: {
  title: string;
  flags: { key: PermissionKey; label: string }[];
  values: Record<string, boolean>;
  onChange: (key: PermissionKey, val: boolean) => void;
}) {
  return (
    <div className="bg-white rounded-lg shadow p-5">
      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">{title}</h3>
      <div className="divide-y divide-gray-100">
        {flags.map(({ key, label }) => (
          <Toggle
            key={key}
            label={label}
            checked={values[key] ?? false}
            onChange={(val) => onChange(key, val)}
          />
        ))}
      </div>
    </div>
  );
}

// ─── Permissions tab ──────────────────────────────────────────────────────────

export default function PermissionsTab({ customer }: { customer: Customer }) {
  const loginName = useAuthStore((s) => s.loginName) ?? 'system';
  const qc = useQueryClient();

  const { data: perms, isLoading } = useQuery({
    queryKey: ['customer-permissions', customer.id],
    queryFn:  () => getCustomerPermissions(customer.id),
  });

  const { register, handleSubmit, watch, setValue, reset, formState: { isDirty } } =
    useForm<Record<PermissionKey, boolean>>({
      defaultValues: {
        webSportsEnabled:  true,
        callInEnabled:     true,
        internetEnabled:   true,
        racebookEnabled:   false,
        casinoEnabled:     true,
        lotteryEnabled:    false,
        liveDealerEnabled: false,
        horseEnabled:      false,
        parlayEnabled:     true,
        teaserEnabled:     true,
        ifBetEnabled:      true,
        reverseEnabled:    true,
        accountLocked:     false,
        receiveAlerts:     true,
      },
    });

  useEffect(() => {
    if (!perms) return;
    reset({
      webSportsEnabled:  perms.webSportsEnabled,
      callInEnabled:     perms.callInEnabled,
      internetEnabled:   perms.internetEnabled,
      racebookEnabled:   perms.racebookEnabled,
      casinoEnabled:     perms.casinoEnabled,
      lotteryEnabled:    perms.lotteryEnabled,
      liveDealerEnabled: perms.liveDealerEnabled,
      horseEnabled:      perms.horseEnabled,
      parlayEnabled:     perms.parlayEnabled,
      teaserEnabled:     perms.teaserEnabled,
      ifBetEnabled:      perms.ifBetEnabled,
      reverseEnabled:    perms.reverseEnabled,
      accountLocked:     perms.accountLocked,
      receiveAlerts:     perms.receiveAlerts,
    });
  }, [perms, reset]);

  // Register all fields so we can use watch() on them
  Object.keys(watch()).forEach((k) => { register(k as PermissionKey); });

  const values = watch();

  const mutation = useMutation({
    mutationFn: (data: Record<PermissionKey, boolean>) => {
      const req: UpdateCustomerPermissionsRequest = { ...data, updatedBy: loginName };
      return updateCustomerPermissions(customer.id, req);
    },
    onSuccess: (updated) => {
      qc.setQueryData(['customer-permissions', customer.id], updated);
      reset({ ...values });
    },
  });

  if (isLoading) return <p className="text-sm text-gray-500">Loading permissions…</p>;

  return (
    <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
        <FlagGroup
          title="Product Access"
          flags={SPORT_FLAGS}
          values={values}
          onChange={(key, val) => setValue(key, val, { shouldDirty: true })}
        />
        <FlagGroup
          title="Wager Types"
          flags={WAGER_FLAGS}
          values={values}
          onChange={(key, val) => setValue(key, val, { shouldDirty: true })}
        />
        <FlagGroup
          title="Account"
          flags={ACCOUNT_FLAGS}
          values={values}
          onChange={(key, val) => setValue(key, val, { shouldDirty: true })}
        />
      </div>

      {perms && (
        <p className="text-xs text-gray-400 mb-3">
          Last updated {new Date(perms.updatedAt).toLocaleString()} by {perms.updatedBy}
        </p>
      )}

      {mutation.isError && (
        <p className="text-xs text-red-500 mb-2">{String(mutation.error)}</p>
      )}
      {mutation.isSuccess && (
        <p className="text-xs text-green-600 mb-2">Permissions saved.</p>
      )}

      <button
        type="submit"
        disabled={!isDirty || mutation.isPending}
        className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {mutation.isPending ? 'Saving…' : 'Save Permissions'}
      </button>
    </form>
  );
}
