import React, { useState, useEffect } from 'react';
import { StudentHold } from '../types';
import { api } from '../services/api';
import { useStudent } from '../context/StudentContext';
import { History, CheckCircle2, Clock, XCircle, AlertCircle, RefreshCw } from 'lucide-react';

export const MyApplications: React.FC = () => {
  const { studentId, isValid } = useStudent();
  const [holds, setHolds] = useState<StudentHold[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchHolds = async () => {
    if (!isValid || !studentId) {
      setHolds([]);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      const data = await api.getMyHolds(studentId);
      setHolds(data);
    } catch (err) {
      console.error('Failed to load student applications:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHolds();
  }, [studentId, isValid]);

  const getStatusBadge = (status: string, expiresAt: string) => {
    const isExpired = status === 'EXPIRED' || (status === 'HELD' && new Date(expiresAt) <= new Date());

    if (status === 'CONFIRMED') {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800">
          <CheckCircle2 className="w-3.5 h-3.5" /> CONFIRMED
        </span>
      );
    }

    if (isExpired) {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-gray-100 text-gray-600">
          <Clock className="w-3.5 h-3.5" /> EXPIRED
        </span>
      );
    }

    if (status === 'CANCELLED') {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-red-100 text-red-700">
          <XCircle className="w-3.5 h-3.5" /> CANCELLED
        </span>
      );
    }

    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-amber-100 text-amber-800 animate-pulse">
        <Clock className="w-3.5 h-3.5" /> ACTIVE HOLD
      </span>
    );
  };

  return (
    <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
      <div className="p-5 border-b border-gray-200 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-purple-50 text-purple-600 rounded-lg">
            <History className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900">Student Application History</h2>
            <p className="text-xs text-gray-500">
              Audit log of all seat reservations for ID <span className="font-mono text-gray-700 font-semibold">{studentId || '(not set)'}</span>
            </p>
          </div>
        </div>

        <button
          onClick={fetchHolds}
          disabled={loading}
          className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition"
          title="Refresh application history"
          aria-label="Refresh history"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin text-purple-600' : ''}`} />
        </button>
      </div>

      {!isValid ? (
        <div className="p-8 text-center text-amber-600 flex flex-col items-center gap-2">
          <AlertCircle className="w-8 h-8" />
          <p className="text-sm font-medium">Please enter a valid 17-digit Student ID in the bar above.</p>
        </div>
      ) : loading ? (
        <div className="p-12 text-center text-gray-400">
          <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-2 text-purple-500" />
          <p className="text-sm">Loading applications...</p>
        </div>
      ) : holds.length === 0 ? (
        <div className="p-12 text-center text-gray-500">
          <p className="font-medium">No application records found for this student ID.</p>
          <p className="text-xs text-gray-400 mt-1">Navigate to the Admission Portal to apply for an available class.</p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-50/75 border-b border-gray-200 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                <th className="py-3 px-4">Hold ID</th>
                <th className="py-3 px-4">Class</th>
                <th className="py-3 px-4">Status</th>
                <th className="py-3 px-4">Expiration / Timestamp</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {holds.map((h) => (
                <tr key={h.holdId} className="hover:bg-gray-50/50 transition">
                  <td className="py-3.5 px-4 font-mono text-xs font-semibold text-gray-700">{h.holdId}</td>
                  <td className="py-3.5 px-4 text-sm font-medium text-gray-900">{h.className || h.classId}</td>
                  <td className="py-3.5 px-4">{getStatusBadge(h.status, h.expiresAt)}</td>
                  <td className="py-3.5 px-4 text-xs text-gray-500 font-mono">
                    {new Date(h.expiresAt).toLocaleString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
