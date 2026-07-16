'use strict'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '@/api/client'
import { useAuthStore } from '@/stores/authStore'

const schema = z.object({
  loginName: z.string().min(1, 'Username is required'),
  password: z.string().min(1, 'Password is required'),
})

type FormValues = z.infer<typeof schema>

interface LoginResponse {
  accessToken: string
  refreshToken: string
  userId: number
  domainEntityId: number | null
  loginName: string
  roles: string[]
}

export function LoginPage(): React.ReactElement {
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      apiClient.post<LoginResponse>('/auth/login', values).then((r) => r.data),
    onSuccess: (data) => {
      login(data.accessToken, data.refreshToken, data.domainEntityId ?? data.userId, data.loginName, data.roles)
      navigate('/sports', { replace: true })
    },
  })

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100 px-4">
      <div className="w-full max-w-sm rounded-xl bg-white p-8 shadow-md">
        <h1 className="mb-8 text-center text-2xl font-bold text-gray-900">COG Betting</h1>
        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-4">
          <div>
            <label htmlFor="loginName" className="block text-sm font-medium text-gray-700">Username</label>
            <input
              id="loginName"
              type="text"
              autoComplete="username"
              {...register('loginName')}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
            {errors.loginName && <p className="mt-1 text-xs text-red-600">{errors.loginName.message}</p>}
          </div>
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700">Password</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              {...register('password')}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
            {errors.password && <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>}
          </div>

          {mutation.isError && (
            <p className="text-xs text-red-600 text-center">Invalid credentials. Please try again.</p>
          )}

          <button
            type="submit"
            disabled={mutation.isPending}
            className="w-full rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {mutation.isPending ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  )
}
