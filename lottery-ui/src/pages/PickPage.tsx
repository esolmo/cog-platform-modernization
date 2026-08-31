import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useForm, useFieldArray } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { lotteryApi, DrawingDetail, PurchaseRequest } from '../api/lottery';

const pickSchema = z.object({
  number1: z.coerce.number().int().min(0).max(9),
  number2: z.coerce.number().int().min(0).max(9),
  number3: z.coerce.number().int().min(0).max(9),
  number4: z.coerce.number().int().min(0).max(9).optional(),
  amount: z.coerce.number().positive(),
});

const formSchema = z.object({
  drawingDetailId: z.coerce.number(),
  dateToPlay: z.string(),
  pickType: z.enum(['1', '2']),
  picks: z.array(pickSchema).min(1),
});

type FormValues = z.infer<typeof formSchema>;

export default function PickPage() {
  const { gameId } = useParams<{ gameId: string }>();
  const navigate = useNavigate();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const { data: drawings, isLoading } = useQuery({
    queryKey: ['drawings', gameId],
    queryFn: () => lotteryApi.getDrawings(Number(gameId)),
    enabled: !!gameId,
  });

  const { register, control, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      pickType: '1',
      picks: [{ number1: 0, number2: 0, number3: 0, number4: 0, amount: 1 }],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'picks' });

  const isPick4 = Number(gameId) === 2;

  const purchaseMutation = useMutation({
    mutationFn: (request: PurchaseRequest) => lotteryApi.purchaseTicket(request),
    onSuccess: (ticket) => {
      setSuccessMessage(`Ticket #${ticket.id} purchased! Total: $${ticket.total.toFixed(2)}`);
      setTimeout(() => navigate('/history'), 2000);
    },
  });

  const onSubmit = (values: FormValues) => {
    const request: PurchaseRequest = {
      drawingDetailId: values.drawingDetailId,
      dateToPlay: values.dateToPlay,
      pickType: Number(values.pickType) as 1 | 2,
      picks: values.picks.map((p) => ({
        number1: p.number1,
        number2: p.number2,
        number3: p.number3,
        number4: p.number4 ?? 0,
        amount: p.amount,
      })),
    };
    purchaseMutation.mutate(request);
  };

  if (isLoading) return <div className="p-8 text-gray-500">Loading drawings...</div>;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-indigo-700 text-white px-8 py-4 flex items-center gap-4">
        <Link to="/games" className="text-indigo-200 hover:text-white">&larr; Games</Link>
        <h1 className="text-2xl font-bold">{isPick4 ? 'Pick 4' : 'Pick 3'}</h1>
      </header>

      <main className="max-w-2xl mx-auto p-8">
        {successMessage && (
          <div className="mb-4 p-4 bg-green-100 text-green-800 rounded-lg border border-green-300">
            {successMessage}
          </div>
        )}

        {purchaseMutation.isError && (
          <div className="mb-4 p-4 bg-red-100 text-red-800 rounded-lg border border-red-300">
            Purchase failed. Please check your balance and try again.
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Drawing &amp; Date</h2>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="drawingDetailId" className="block text-sm font-medium text-gray-700 mb-1">Drawing</label>
                <select
                  id="drawingDetailId"
                  {...register('drawingDetailId')}
                  className="w-full border border-gray-300 rounded-md p-2 text-sm"
                >
                  {drawings?.map((d: DrawingDetail) => (
                    <option key={d.id} value={d.id}>
                      {d.name} — {new Date(d.drawingDate).toLocaleString()}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="dateToPlay" className="block text-sm font-medium text-gray-700 mb-1">Play Date</label>
                <input
                  id="dateToPlay"
                  type="date"
                  {...register('dateToPlay')}
                  defaultValue={new Date().toISOString().split('T')[0]}
                  className="w-full border border-gray-300 rounded-md p-2 text-sm"
                />
              </div>
            </div>

            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">Pick Type</label>
              <div className="flex gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" {...register('pickType')} value="1" className="text-indigo-600" />
                  <span className="text-sm">Straight</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input type="radio" {...register('pickType')} value="2" className="text-indigo-600" />
                  <span className="text-sm">Boxed (all permutations)</span>
                </label>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-800">Your Numbers</h2>
              <button
                type="button"
                onClick={() => append({ number1: 0, number2: 0, number3: 0, number4: 0, amount: 1 })}
                className="text-sm text-indigo-600 hover:text-indigo-800"
              >
                + Add Line
              </button>
            </div>

            {fields.map((field, idx) => (
              <div key={field.id} className="flex items-center gap-3 mb-3">
                <div className="flex gap-2">
                  {['number1', 'number2', 'number3'].map((n) => (
                    <input
                      key={n}
                      type="number"
                      min={0}
                      max={9}
                      {...register(`picks.${idx}.${n as 'number1' | 'number2' | 'number3'}`)}
                      className="w-14 text-center border border-gray-300 rounded-md p-2 text-sm font-mono"
                    />
                  ))}
                  {isPick4 && (
                    <input
                      type="number"
                      min={0}
                      max={9}
                      {...register(`picks.${idx}.number4`)}
                      className="w-14 text-center border border-gray-300 rounded-md p-2 text-sm font-mono"
                    />
                  )}
                </div>
                <span className="text-gray-400 text-sm">$</span>
                <input
                  type="number"
                  step="0.50"
                  min="0.50"
                  {...register(`picks.${idx}.amount`)}
                  className="w-20 border border-gray-300 rounded-md p-2 text-sm"
                />
                {fields.length > 1 && (
                  <button
                    type="button"
                    onClick={() => remove(idx)}
                    className="text-red-400 hover:text-red-600 text-sm"
                  >
                    Remove
                  </button>
                )}
              </div>
            ))}
            {errors.picks && (
              <p className="text-red-500 text-sm mt-1">Please fill in all pick numbers.</p>
            )}
          </div>

          <button
            type="submit"
            disabled={purchaseMutation.isPending}
            className="w-full bg-indigo-600 text-white py-3 rounded-lg font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-colors"
          >
            {purchaseMutation.isPending ? 'Processing...' : 'Purchase Ticket'}
          </button>
        </form>
      </main>
    </div>
  );
}
