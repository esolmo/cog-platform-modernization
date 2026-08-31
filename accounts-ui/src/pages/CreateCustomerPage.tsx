import { useId, cloneElement, type ReactElement } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { createCustomer, listAgents } from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';

const createSchema = z.object({
  loginName:       z.string().min(1).max(10, 'Max 10 characters'),
  agentId:         z.number({ invalid_type_error: 'Select an agent' }).min(1, 'Select an agent'),
  alternateLoginName: z.string().max(20).optional(),
  email:           z.string().email('Invalid email').optional().or(z.literal('')),
  phone:           z.string().optional(),
  oddsFormat:      z.enum(['American', 'Decimal', 'Fractional']),
  creditLimit:     z.number().min(0),
  wagerLimit:      z.number().min(0),
  maxStraightWager: z.number().min(0),
  maxParlayWager:  z.number().min(0),
  maxParlayPayout: z.number().min(0),
  maxTeaserWager:  z.number().min(0),
  maxIfBetWager:   z.number().min(0),
  hardCreditLimit: z.number().min(0),
});

type CreateFormData = z.infer<typeof createSchema>;

export default function CreateCustomerPage() {
  const navigate       = useNavigate();
  const storeAgentId   = useAuthStore((s) => s.agentId);
  const loginName      = useAuthStore((s) => s.loginName) ?? 'unknown';
  const isAdmin        = !storeAgentId; // admin users have no agentId

  const { data: agents } = useQuery({
    queryKey: ['agents'],
    queryFn: listAgents,
    enabled: isAdmin,
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateFormData>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      agentId:         storeAgentId ?? undefined,
      oddsFormat:      'American',
      creditLimit:     500,
      wagerLimit:      250,
      maxStraightWager: 200,
      maxParlayWager:  200,
      maxParlayPayout: 2000,
      maxTeaserWager:  100,
      maxIfBetWager:   100,
      hardCreditLimit: 1000,
    },
  });

  const mutation = useMutation({
    mutationFn: (data: CreateFormData) =>
      createCustomer({ ...data, createdBy: loginName }),
    onSuccess: (customer) => {
      navigate(`/customers/${customer.id}`);
    },
  });

  return (
    <div className="max-w-2xl">
      <h1 className="text-xl font-semibold text-gray-800 mb-6">New Customer</h1>
      <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="space-y-6">
        <section className="bg-white rounded-lg shadow p-6">
          <h2 className="text-sm font-semibold text-gray-700 mb-4">Personal Information</h2>
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Login Name *" error={errors.loginName?.message}>
              <input {...register('loginName')} className={inputCls} />
            </FormField>
            {isAdmin && (
              <FormField label="Agent *" error={errors.agentId?.message}>
                <select
                  {...register('agentId', { valueAsNumber: true })}
                  className={inputCls}
                  defaultValue=""
                >
                  <option value="" disabled>Select agent…</option>
                  {(agents ?? []).map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.loginName}{a.name ? ` — ${a.name}` : ''}
                    </option>
                  ))}
                </select>
              </FormField>
            )}
            <FormField label="Alternate Login" error={errors.alternateLoginName?.message}>
              <input {...register('alternateLoginName')} className={inputCls} />
            </FormField>
            <FormField label="Email" error={errors.email?.message}>
              <input {...register('email')} type="email" className={inputCls} />
            </FormField>
            <FormField label="Phone" error={errors.phone?.message}>
              <input {...register('phone')} className={inputCls} />
            </FormField>
            <FormField label="Odds Format" error={errors.oddsFormat?.message}>
              <select {...register('oddsFormat')} className={inputCls}>
                <option value="American">American</option>
                <option value="Decimal">Decimal</option>
                <option value="Fractional">Fractional</option>
              </select>
            </FormField>
          </div>
        </section>

        <section className="bg-white rounded-lg shadow p-6">
          <h2 className="text-sm font-semibold text-gray-700 mb-4">Limits</h2>
          <div className="grid grid-cols-2 gap-4">
            {[
              { label: 'Credit Limit',       name: 'creditLimit' as const },
              { label: 'Wager Limit',        name: 'wagerLimit' as const },
              { label: 'Max Straight',       name: 'maxStraightWager' as const },
              { label: 'Max Parlay',         name: 'maxParlayWager' as const },
              { label: 'Max Parlay Payout',  name: 'maxParlayPayout' as const },
              { label: 'Max Teaser',         name: 'maxTeaserWager' as const },
              { label: 'Max If-Bet',         name: 'maxIfBetWager' as const },
              { label: 'Hard Credit Limit',  name: 'hardCreditLimit' as const },
            ].map(({ label, name }) => (
              <FormField key={name} label={label} error={(errors[name] as { message?: string } | undefined)?.message}>
                <input
                  {...register(name, { valueAsNumber: true })}
                  type="number"
                  step="0.01"
                  className={inputCls}
                />
              </FormField>
            ))}
          </div>
        </section>

        {mutation.isError && (
          <p className="text-sm text-red-500">
            {(mutation.error as Error)?.message ?? 'Failed to create customer.'}
          </p>
        )}

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={mutation.isPending}
            className="bg-blue-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
          >
            {mutation.isPending ? 'Creating...' : 'Create Customer'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/customers')}
            className="px-6 py-2 rounded-md text-sm font-medium border border-gray-300 text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

const inputCls =
  'w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent';

function FormField({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: ReactElement;
}) {
  const id = useId();
  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {cloneElement(children, { id })}
      {error && <p className="mt-1 text-xs text-red-500">{error}</p>}
    </div>
  );
}
