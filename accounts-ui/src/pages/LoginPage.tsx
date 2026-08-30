import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import apiClient from '../api/client';

const loginSchema = z.object({
  loginName: z.string().min(1, 'Login name is required'),
  password:  z.string().min(1, 'Password is required'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const navigate  = useNavigate();
  const location  = useLocation();
  const { setTokens, setUser } = useAuthStore();
  const from      = (location.state as { from?: Location })?.from?.pathname ?? '/customers';

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({ resolver: zodResolver(loginSchema) });

  const onSubmit = async (data: LoginFormData) => {
    try {
      const response = await apiClient.post('/auth/login', data);
      const { accessToken, refreshToken, domainEntityId, loginName, roles } = response.data;

      setTokens(accessToken, refreshToken);
      setUser(domainEntityId, loginName, roles);

      navigate(from, { replace: true });
    } catch {
      setError('root', { message: 'Invalid credentials. Please try again.' });
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-sm">
        <h1 className="text-2xl font-bold text-gray-800 mb-6">COG Accounts</h1>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label htmlFor="loginName" className="block text-sm font-medium text-gray-700 mb-1">
              Login Name
            </label>
            <input
              id="loginName"
              {...register('loginName')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              autoComplete="username"
            />
            {errors.loginName && (
              <p className="mt-1 text-xs text-red-500">{errors.loginName.message}</p>
            )}
          </div>
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              Password
            </label>
            <input
              id="password"
              {...register('password')}
              type="password"
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              autoComplete="current-password"
            />
            {errors.password && (
              <p className="mt-1 text-xs text-red-500">{errors.password.message}</p>
            )}
          </div>
          {errors.root && (
            <p className="text-sm text-red-500">{errors.root.message}</p>
          )}
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-blue-600 text-white rounded-md py-2 text-sm font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  );
}
