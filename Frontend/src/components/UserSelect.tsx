import React from 'react';
import { userService, type UserOption } from '@/services/userService';

interface Props {
  multiple?: boolean;
  value?: string | string[];
  onChange: (ids: string[], options: UserOption[]) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}

const baseClass = "w-full rounded-lg border border-[#CDA352]/30 focus:outline-none focus:ring-2 focus:ring-[#B351A9]/20 focus:border-[#B351A9] p-2.5 text-sm transition-all bg-white";

const UserSelect: React.FC<Props> = ({ multiple, value, onChange, placeholder = 'Select user', disabled, className }) => {
  const [options, setOptions] = React.useState<UserOption[]>([]);
  const [loading, setLoading] = React.useState<boolean>(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const opts = await userService.getAllOptions();
        if (!mounted) return;
        setOptions(opts);
      } catch (e: any) {
        if (!mounted) return;
        setError(e?.message || 'Failed to load users');
        setOptions([]);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const selectedIds = multiple
      ? Array.from(e.target.selectedOptions).map((o) => o.value).filter(Boolean)
      : [e.target.value].filter(Boolean);
    const selectedOpts = options.filter((o) => selectedIds.includes(o.id));
    onChange(selectedIds, selectedOpts);
  };

  const valueArr = Array.isArray(value) ? value : (value ? [value] : []);

  return (
    <div>
      <select
        multiple={!!multiple}
        value={multiple ? valueArr : (valueArr[0] ?? '')}
        onChange={handleChange}
        disabled={disabled || loading}
        className={`${baseClass} ${className || ''}`}
      >
        {!multiple && (
          <option value="" disabled>
            {loading ? 'Loading users...' : (error ? 'Failed to load users' : placeholder)}
          </option>
        )}
        {options.map((u) => (
          <option key={u.id} value={u.id}>
            {u.name}{u.email ? ` (${u.email})` : ''}
          </option>
        ))}
      </select>
      {error && (
        <div className="text-xs text-red-600 mt-1 font-semibold">{error}</div>
      )}
    </div>
  );
};

export default UserSelect;
