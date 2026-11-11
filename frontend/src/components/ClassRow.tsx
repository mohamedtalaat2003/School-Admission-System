import React, { useState, useEffect } from 'react';
import { ClassItem, StudentHold, ApiError } from '../types';
import { api } from '../services/api';
import { useStudent } from '../context/StudentContext';
import { Clock, Check, XCircle, AlertCircle, Loader2 } from 'lucide-react';

interface ClassRowProps {
  classItem: ClassItem;
  hold?: StudentHold;
  onHoldUpdated: () => void;
}

export const ClassRow: React.FC<ClassRowProps> = ({ classItem, hold, onHoldUpdated }) => {
  const { studentId, isValid } = useStudent();
  const [loading, setLoading] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [remainingSeconds, setRemainingSeconds] = useState<number>(() => {
    if (!hold || hold.status !== 'HELD') return 0;
    const diff = Math.floor((new Date(hold.expiresAt).getTime() - Date.now()) / 1000);
    return Math.max(0, diff);
  });

  // Countdown timer for active hold
  useEffect(() => {
    if (!hold || hold.status !== 'HELD') {
      setRemainingSeconds(0);
      return;
    }

    const calculateRemaining = () => {
      const diff = Math.floor((new Date(hold.expiresAt).getTime() - Date.now()) / 1000);
      return Math.max(0, diff);
    };

    setRemainingSeconds(calculateRemaining());

    const timer = setInterval(() => {
      const remaining = calculateRemaining();
      setRemainingSeconds(remaining);
      if (remaining === 0) {
        clearInterval(timer);
        onHoldUpdated(); // Refresh seat status with zero API calls required from user
      }
    }, 1000);

    return () => clearInterval(timer);
  }, [hold, onHoldUpdated]);

  const handleApply = async () => {
    if (!isValid) {
      setActionError('Valid 17-digit Student ID required to apply.');
      return;
    }

    try {
      setLoading(true);
      setActionError(null);
      await api.createHold(classItem.id, studentId);
      onHoldUpdated();
    } catch (err) {
      const apiErr = err as ApiError;
      setActionError(apiErr.message || 'Failed to hold seat.');
    } finally {
      setLoading(false);
    }
  };

  const handleConfirm = async () => {
    if (!hold) return;
    try {
      setLoading(true);
      setActionError(null);
      await api.confirmHold(hold.holdId, studentId);
      onHoldUpdated();
    } catch (err) {
      const apiErr = err as ApiError;
      setActionError(apiErr.message || 'Payment confirmation failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!hold) return;
    try {
      setLoading(true);
      setActionError(null);
      await api.cancelHold(hold.holdId, studentId);
      onHoldUpdated();
    } catch (err) {
      const apiErr = err as ApiError;
      setActionError(apiErr.message || 'Cancellation failed.');
    } finally {
      setLoading(false);
    }
  };

  const formatMMSS = (sec: number) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  };

  const renderDynamicButton = () => {
    if (loading) {
      return (
        <button disabled className="px-4 py-2 bg-gray-100 text-gray-400 rounded-lg text-sm font-medium flex items-center gap-2">
          <Loader2 className="w-4 h-4 animate-spin" /> Processing...
        </button>
      );
    }

    // 1. Confirmed state
    if (hold?.status === 'CONFIRMED') {
      return (
        <button
          disabled
          className="px-4 py-2 bg-emerald-100 text-emerald-800 rounded-lg text-sm font-semibold flex items-center gap-1.5 cursor-default"
          aria-label="Confirmed Admission"
        >
          <Check className="w-4 h-4" /> CONFIRMED
        </button>
      );
    }

    // 2. Active HELD state with live countdown
    if (hold?.status === 'HELD' && remainingSeconds > 0) {
      return (
        <div className="flex items-center gap-2">
          <button
            onClick={handleConfirm}
            className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-sm font-semibold flex items-center gap-2 shadow-sm transition animate-pulse"
            aria-label={`Pay Fee for Hold in ${classItem.name}`}
          >
            <Clock className="w-4 h-4" /> HELD ({formatMMSS(remainingSeconds)}) — Pay Fee
          </button>
          <button
            onClick={handleCancel}
            className="px-3 py-2 bg-gray-100 hover:bg-red-50 text-gray-600 hover:text-red-600 border border-gray-200 rounded-lg text-sm font-medium transition"
            title="Cancel Seat Hold"
            aria-label="Cancel Hold"
          >
            <XCircle className="w-4 h-4" />
          </button>
        </div>
      );
    }

    // 3. Expired state
    if (hold?.status === 'EXPIRED' || (hold?.status === 'HELD' && remainingSeconds === 0)) {
      return (
        <button
          disabled
          className="px-4 py-2 bg-gray-100 text-gray-500 rounded-lg text-sm font-medium cursor-not-allowed border border-gray-200"
          aria-label="Hold Expired"
        >
          EXPIRED
        </button>
      );
    }

    // 4. No hold: Available seats check
    if (classItem.availableSeats > 0) {
      return (
        <button
          onClick={handleApply}
          disabled={!isValid}
          className={`px-5 py-2 rounded-lg text-sm font-semibold transition shadow-sm ${
            isValid
              ? 'bg-indigo-600 hover:bg-indigo-700 text-white'
              : 'bg-gray-100 text-gray-400 cursor-not-allowed'
          }`}
          aria-label={`Apply for ${classItem.name}`}
        >
          Apply
        </button>
      );
    }

    // 5. Class Full state
    return (
      <button
        disabled
        className="px-4 py-2 bg-gray-100 text-gray-400 rounded-lg text-sm font-medium cursor-not-allowed"
        aria-label="Class Full"
      >
        Class Full
      </button>
    );
  };

  return (
    <tr className="hover:bg-gray-50/75 transition border-b border-gray-100">
      <td className="py-4 px-4">
        <div className="font-semibold text-gray-900">{classItem.name}</div>
        <div className="text-xs text-gray-400 font-mono">ID: {classItem.id}</div>
        {actionError && (
          <div className="mt-1 flex items-center gap-1 text-xs text-red-600">
            <AlertCircle className="w-3.5 h-3.5 shrink-0" />
            <span>{actionError}</span>
          </div>
        )}
      </td>
      <td className="py-4 px-4 text-center">
        <span className="font-medium text-gray-700">{classItem.totalSeats}</span>
      </td>
      <td className="py-4 px-4 text-center">
        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-amber-50 text-amber-700">
          {classItem.heldSeats}
        </span>
      </td>
      <td className="py-4 px-4 text-center">
        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-emerald-50 text-emerald-700">
          {classItem.confirmedSeats}
        </span>
      </td>
      <td className="py-4 px-4 text-center">
        <span
          className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold ${
            classItem.availableSeats > 0 ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-700'
          }`}
        >
          {classItem.availableSeats}
        </span>
      </td>
      <td className="py-4 px-4 text-right">
        <div className="flex justify-end">{renderDynamicButton()}</div>
      </td>
    </tr>
  );
};
