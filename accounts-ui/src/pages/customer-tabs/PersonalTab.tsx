import { format } from 'date-fns';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateCustomer } from '../../api/accountsApi';
import { useAuthStore } from '../../stores/authStore';
import type { Customer } from '../../types/accounts';

const personalSchema = z.object({
  alternateLoginName: z.string().optional(),
  email: z.string().email().optional().or(z.literal('')),
  phone: z.string().optional(),
  oddsFormat: z.enum(['American', 'Decimal', 'Fractional']),
  instantActionEnabled: z.boolean(),
});
type PersonalFields = z.infer<typeof personalSchema>;

interface PersonalTabProps {
  customer: Customer;
}

export default function PersonalTab({ customer }: PersonalTabProps) {
  const qc        = useQueryClient();
  const loginName = useAuthStore((s) => s.loginName) ?? 'system';

  const { register, handleSubmit, formState: { errors, isDirty } } = useForm<PersonalFields>({
    resolver: zodResolver(personalSchema),
    defaultValues: {
      alternateLoginName: customer.alternateLoginName ?? '',
      email: customer.email ?? '',
      phone: customer.phone ?? '',
      oddsFormat: customer.oddsFormat,
      instantActionEnabled: customer.instantActionEnabled,
    },
  });

  const mutation = useMutation({
    mutationFn: (d: PersonalFields) =>
      updateCustomer(customer.id, { ...d, updatedBy: loginName }),
    onSuccess: (updated) => qc.setQueryData(['customer', customer.id], updated),
  });

  return (
    <div className="bg-white rounded-lg shadow p-6 max-w-lg">
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4">
        <Row label="Login Name" value={customer.loginName} />

        <Field label="Alternate Login">
          <input
            {...register('alternateLoginName')}
            className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none"
          />
        </Field>

        <Field label="Email" error={errors.email?.message}>
          <input
            {...register('email')}
            type="email"
            className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none"
          />
        </Field>

        <Field label="Phone">
          <input
            {...register('phone')}
            className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none"
          />
        </Field>

        <Field label="Odds Format">
          <select
            {...register('oddsFormat')}
            className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none"
          >
            <option value="American">American</option>
            <option value="Decimal">Decimal</option>
            <option value="Fractional">Fractional</option>
          </select>
        </Field>

        <div className="flex items-center gap-2">
          <input
            {...register('instantActionEnabled')}
            type="checkbox"
            id="instantActionEnabled"
            className="rounded border-gray-300"
          />
          <label htmlFor="instantActionEnabled" className="text-sm text-gray-700">
            Instant Action Enabled
          </label>
        </div>

        <Row label="Member Since" value={format(new Date(customer.createdAt), 'PP')} />

        {mutation.isError && <p className="text-xs text-red-500">{String(mutation.error)}</p>}
        {mutation.isSuccess && <p className="text-xs text-green-600">Saved.</p>}
        <button
          type="submit"
          disabled={!isDirty || mutation.isPending}
          className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {mutation.isPending ? 'Saving…' : 'Save Changes'}
        </button>
      </form>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between text-sm">
      <dt className="text-gray-500 font-medium">{label}</dt>
      <dd className="text-gray-800">{value}</dd>
    </div>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
      {children}
      {error && <p className="text-xs text-red-500 mt-0.5">{error}</p>}
    </div>
  );
}
