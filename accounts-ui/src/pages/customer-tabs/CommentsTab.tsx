'use strict';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { addCustomerComment, getCustomerComments } from '../../api/accountsApi';
import { useAuthStore } from '../../stores/authStore';
import type { Customer } from '../../types/accounts';

const commentSchema = z.object({
  body:              z.string().min(1, 'Comment cannot be empty'),
  visibleToCustomer: z.boolean(),
  visibleToAgent:    z.boolean(),
});
type CommentFields = z.infer<typeof commentSchema>;

const fmtDate = (s: string) =>
  new Date(s).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit' });

export default function CommentsTab({ customer }: { customer: Customer }) {
  const loginName = useAuthStore((s) => s.loginName) ?? 'system';
  const [showAll, setShowAll] = useState(false);
  const qc = useQueryClient();

  const { data: comments, isLoading } = useQuery({
    queryKey: ['customer-comments', customer.id, showAll],
    queryFn:  () => getCustomerComments(customer.id, showAll),
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<CommentFields>({
    resolver: zodResolver(commentSchema),
    defaultValues: { body: '', visibleToCustomer: false, visibleToAgent: true },
  });

  const mutation = useMutation({
    mutationFn: (d: CommentFields) =>
      addCustomerComment(customer.id, { ...d, createdBy: loginName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customer-comments', customer.id] });
      reset({ body: '', visibleToCustomer: false, visibleToAgent: true });
    },
  });

  return (
    <div className="space-y-6">
      {/* Add comment form */}
      <div className="bg-white rounded-lg shadow p-5">
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Add Comment</h3>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-3">
          <div>
            <textarea
              {...register('body')}
              rows={3}
              placeholder="Enter comment…"
              className="w-full border border-gray-300 rounded px-3 py-2 text-sm resize-none focus:ring-1 focus:ring-blue-500 focus:outline-none"
            />
            {errors.body && <p className="text-xs text-red-500 mt-0.5">{errors.body.message}</p>}
          </div>
          <div className="flex items-center gap-6 text-sm">
            <label className="flex items-center gap-2 cursor-pointer">
              <input {...register('visibleToAgent')} type="checkbox" className="rounded border-gray-300" />
              <span className="text-gray-600">Visible to agent</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input {...register('visibleToCustomer')} type="checkbox" className="rounded border-gray-300" />
              <span className="text-gray-600">Visible to customer</span>
            </label>
          </div>
          {mutation.isError   && <p className="text-xs text-red-500">{String(mutation.error)}</p>}
          <button type="submit" disabled={mutation.isPending}
            className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
            {mutation.isPending ? 'Posting…' : 'Post Comment'}
          </button>
        </form>
      </div>

      {/* Filter toggle */}
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
          Comments <span className="font-normal text-gray-400">({comments?.length ?? 0})</span>
        </h3>
        <label className="flex items-center gap-2 text-xs text-gray-600 cursor-pointer">
          <input
            type="checkbox"
            checked={showAll}
            onChange={(e) => setShowAll(e.target.checked)}
            className="rounded border-gray-300"
          />
          Include customer-visible
        </label>
      </div>

      {/* Comment feed */}
      {isLoading && <p className="text-sm text-gray-500">Loading…</p>}
      {!isLoading && (!comments || comments.length === 0) && (
        <p className="text-sm text-gray-500 text-center py-6">No comments yet.</p>
      )}
      {comments && comments.length > 0 && (
        <div className="space-y-3">
          {comments.map((c) => (
            <div key={c.id} className="bg-white rounded-lg shadow p-4">
              <div className="flex items-start justify-between gap-4 mb-2">
                <p className="text-sm text-gray-800 whitespace-pre-wrap">{c.body}</p>
                <div className="flex gap-1 shrink-0">
                  {c.visibleToAgent && (
                    <span className="px-1.5 py-0.5 rounded text-xs bg-blue-50 text-blue-600">Agent</span>
                  )}
                  {c.visibleToCustomer && (
                    <span className="px-1.5 py-0.5 rounded text-xs bg-green-50 text-green-600">Customer</span>
                  )}
                </div>
              </div>
              <p className="text-xs text-gray-400">{c.createdBy} &middot; {fmtDate(c.createdAt)}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
