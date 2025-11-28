import { apiClient } from '@/lib/api';

// User domain specific lightweight DTOs (expand if needed)
export interface UserSummary {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  department?: string;
  role?: string;
  phoneNumber?: string;
}

// Standardized option used by dropdowns
export interface UserOption {
  id: string;
  name: string;
  username?: string;
  email?: string;
}

export class UserService {
  // GET /api/User/users-by-department/{departmentName}
  getUsersByDepartment(departmentName: string) {
    return apiClient.get<UserSummary[]>(`/User/users-by-department/${encodeURIComponent(departmentName)}`);
  }

  // GET /api/User/users-by-manager/{manager}
  getUsersByManager(manager: string) {
    return apiClient.get<UserSummary[]>(`/User/users-by-manager/${encodeURIComponent(manager)}`);
  }

  // GET /api/User - Get all users (if this endpoint exists)
  getAllUsers() {
    return apiClient.get<UserSummary[]>('/User');
  }

  // GET /api/User/me - Get current user
  getCurrentUser() {
    return apiClient.get<UserSummary>('/User/me');
  }

  // Helper: Get all users as options for selects (robust to varying backend casing)
  async getAllOptions(): Promise<UserOption[]> {
    const res = await apiClient.get<any>('/User');
    if (!res?.success) {
      throw new Error(res?.message || 'Failed to load users');
    }
    const raw = Array.isArray(res.data) ? res.data : (res.data?.users || []);
    if (!Array.isArray(raw)) return [];
    return raw.map((u: any) => {
      const id = String(u.id ?? u.Id ?? u.userId ?? u.UserId ?? u.UserID ?? '');
      const fullName = u.fullName ?? u.FullName ?? '';
      const userName = u.userName ?? u.UserName ?? u.EmployeeId ?? u.employeeId ?? '';
      const email = u.email ?? u.Email ?? undefined;
      const name = String(fullName || userName || email || id);
      return { id, name, username: userName ? String(userName) : undefined, email: email ? String(email) : undefined } as UserOption;
    }).filter((o: UserOption) => !!o.id);
  }
}

export const userService = new UserService();
