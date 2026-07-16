import { format } from 'date-fns';
import type { Customer } from '../../types/accounts';

interface PersonalTabProps {
  customer: Customer;
}

export default function PersonalTab({ customer }: PersonalTabProps) {
  return (
    <div className="bg-white rounded-lg shadow p-6 max-w-lg">
      <dl className="space-y-4">
        <Row label="Login Name"    value={customer.loginName} />
        <Row label="Alternate"     value={customer.alternateLoginName ?? '—'} />
        <Row label="Email"         value={customer.email ?? '—'} />
        <Row label="Phone"         value={customer.phone ?? '—'} />
        <Row label="Odds Format"   value={customer.oddsFormat} />
        <Row label="Instant Action" value={customer.instantActionEnabled ? 'Enabled' : 'Disabled'} />
        <Row label="Member Since"  value={format(new Date(customer.createdAt), 'PP')} />
      </dl>
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
