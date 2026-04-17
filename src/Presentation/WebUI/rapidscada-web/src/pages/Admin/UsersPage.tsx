import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { UserPlus, Edit, Trash2, Shield } from 'lucide-react';
import { userService } from '../../services/api';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';

export default function UsersPage() {
  const { t } = useTranslation();
  const [showAddModal, setShowAddModal] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: () => userService.getAll(),
  });

  if (isLoading) return <LoadingSpinner />;

  const users = data?.data || [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">{t('admin.users')}</h1>
          <p className="text-gray-400 mt-1">Manage user accounts and permissions</p>
        </div>
        <button 
          onClick={() => setShowAddModal(true)}
          className="btn btn-primary"
        >
          <UserPlus className="w-5 h-5 mr-2" />
          Add User
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Total Users</p>
          <p className="text-3xl font-bold text-gray-100 mt-2">{users.length}</p>
        </div>
        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Administrators</p>
          <p className="text-3xl font-bold text-gray-100 mt-2">
            {users.filter((u: any) => u.roles?.includes('Admin')).length}
          </p>
        </div>
        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Operators</p>
          <p className="text-3xl font-bold text-gray-100 mt-2">
            {users.filter((u: any) => u.roles?.includes('Operator')).length}
          </p>
        </div>
        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Active Today</p>
          <p className="text-3xl font-bold text-gray-100 mt-2">
            {users.filter((u: any) => u.isActive).length}
          </p>
        </div>
      </div>

      {/* Users Table */}
      <div className="glass-card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Email</th>
              <th>Role</th>
              <th>Status</th>
              <th>Last Login</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user: any) => (
              <tr key={user.id}>
                <td>
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 bg-sky-500 rounded-full flex items-center justify-center text-white font-semibold">
                      {user.userName?.[0]?.toUpperCase()}
                    </div>
                    <span className="font-medium">{user.userName}</span>
                  </div>
                </td>
                <td className="text-gray-400">{user.email}</td>
                <td>
                  <div className="flex gap-1">
                    {(user.roles || ['User']).map((role: string) => (
                      <span 
                        key={role}
                        className={`px-2 py-1 text-xs rounded ${
                          role === 'Admin' ? 'bg-red-500/20 text-red-400' :
                          role === 'Operator' ? 'bg-blue-500/20 text-blue-400' :
                          'bg-gray-500/20 text-gray-400'
                        }`}
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </td>
                <td>
                  <span className={`status-badge ${
                    user.isActive ? 'status-online' : 'status-offline'
                  }`}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="text-sm text-gray-400">
                  {user.lastLogin 
                    ? new Date(user.lastLogin).toLocaleString()
                    : 'Never'}
                </td>
                <td>
                  <div className="flex gap-2">
                    <button className="btn btn-secondary text-sm px-2 py-1">
                      <Shield className="w-4 h-4" />
                    </button>
                    <button className="btn btn-secondary text-sm px-2 py-1">
                      <Edit className="w-4 h-4" />
                    </button>
                    <button className="btn btn-danger text-sm px-2 py-1">
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Add User Modal */}
      {showAddModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="glass-card p-6 w-full max-w-md">
            <h2 className="text-xl font-bold text-gray-100 mb-4">Add New User</h2>
            
            <form className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Username
                </label>
                <input type="text" className="input" placeholder="john.doe" />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Email
                </label>
                <input type="email" className="input" placeholder="john@example.com" />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Password
                </label>
                <input type="password" className="input" />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Role
                </label>
                <select className="input">
                  <option>User</option>
                  <option>Operator</option>
                  <option>Admin</option>
                </select>
              </div>

              <div className="flex gap-2 pt-4">
                <button type="submit" className="btn btn-primary flex-1">
                  Create User
                </button>
                <button 
                  type="button"
                  onClick={() => setShowAddModal(false)}
                  className="btn btn-secondary flex-1"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
