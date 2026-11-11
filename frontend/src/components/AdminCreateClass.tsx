import React, { useState } from 'react';
import { api } from '../services/api';
import { ApiError } from '../types';
import { PlusCircle, School, AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';

interface AdminCreateClassProps {
  onClassCreated: () => void;
}

export const AdminCreateClass: React.FC<AdminCreateClassProps> = ({ onClassCreated }) => {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState<number | ''>(25);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('Class name is required.');
      return;
    }

    const seats = Number(totalSeats);
    if (!Number.isInteger(seats) || seats <= 0) {
      setError('Total seats must be a positive integer.');
      return;
    }

    try {
      setLoading(true);
      const created = await api.createClass({
        name: trimmedName,
        totalSeats: seats,
      });

      setSuccess(`Class "${created.name}" created successfully with ${created.totalSeats} seats.`);
      setName('');
      setTotalSeats(25);
      onClassCreated();
    } catch (err) {
      const apiErr = err as ApiError;
      setError(apiErr.message || 'Failed to create class.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-white border border-gray-200 rounded-xl shadow-sm p-6 mb-8">
      <div className="flex items-center gap-3 mb-5 border-b border-gray-100 pb-4">
        <div className="p-2 bg-indigo-50 text-indigo-600 rounded-lg">
          <School className="w-5 h-5" />
        </div>
        <div>
          <h2 className="text-lg font-bold text-gray-900">School Admin — Class Management</h2>
          <p className="text-xs text-gray-500">Configure new classes and allocate initial seat capacities</p>
        </div>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg flex items-center gap-2 text-sm text-red-700">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {success && (
        <div className="mb-4 p-3 bg-emerald-50 border border-emerald-200 rounded-lg flex items-center gap-2 text-sm text-emerald-700">
          <CheckCircle2 className="w-4 h-4 shrink-0" />
          <span>{success}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-12 gap-4 items-end">
        <div className="md:col-span-7">
          <label htmlFor="className" className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-1.5">
            Class Name
          </label>
          <input
            id="className"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Grade 6 - Morning Shift"
            className="w-full text-sm px-3.5 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-200 focus:border-indigo-600 transition"
            disabled={loading}
          />
        </div>

        <div className="md:col-span-3">
          <label htmlFor="totalSeats" className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-1.5">
            Total Seats
          </label>
          <input
            id="totalSeats"
            type="number"
            min={1}
            max={1000}
            value={totalSeats}
            onChange={(e) => setTotalSeats(e.target.value === '' ? '' : Number(e.target.value))}
            className="w-full text-sm px-3.5 py-2.5 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-200 focus:border-indigo-600 transition"
            disabled={loading}
          />
        </div>

        <div className="md:col-span-2">
          <button
            type="submit"
            disabled={loading}
            className="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg font-medium text-sm transition shadow-sm disabled:opacity-50"
          >
            {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <PlusCircle className="w-4 h-4" />}
            Create
          </button>
        </div>
      </form>
    </div>
  );
};
