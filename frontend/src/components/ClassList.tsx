import React, { useState, useEffect, useCallback } from 'react';
import { ClassItem, StudentHold } from '../types';
import { api } from '../services/api';
import { useStudent } from '../context/StudentContext';
import { ClassRow } from './ClassRow';
import { RefreshCw, BookOpen, ChevronLeft, ChevronRight, CheckCircle2 } from 'lucide-react';

interface ClassListProps {
  refreshTrigger: number;
}

export const ClassList: React.FC<ClassListProps> = ({ refreshTrigger }) => {
  const { studentId, isValid } = useStudent();
  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [myHolds, setMyHolds] = useState<StudentHold[]>([]);
  const [loading, setLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const loadData = useCallback(async (background = false) => {
    try {
      if (!background) setIsRefreshing(true);
      const [classesData, holdsData] = await Promise.all([
        api.getClasses(page, pageSize),
        isValid && studentId ? api.getMyHolds(studentId).catch(() => []) : Promise.resolve([]),
      ]);
      setClasses(classesData);
      setMyHolds(holdsData);
    } catch (err) {
      console.error('Error fetching data:', err);
    } finally {
      setLoading(false);
      setIsRefreshing(false);
    }
  }, [page, pageSize, studentId, isValid]);

  useEffect(() => {
    loadData();
  }, [loadData, refreshTrigger]);

  // Mandatory 3-second background polling for real-time seat availability
  useEffect(() => {
    const pollInterval = setInterval(() => {
      loadData(true);
    }, 3000);

    return () => clearInterval(pollInterval);
  }, [loadData]);

  // Map holds by classId for instant lookup
  const holdsByClass = myHolds.reduce<Record<string, StudentHold>>((acc, hold) => {
    if (!acc[hold.classId] || new Date(hold.expiresAt) > new Date(acc[hold.classId].expiresAt)) {
      acc[hold.classId] = hold;
    }
    return acc;
  }, {});

  const confirmedCount = myHolds.filter(h => h.status === 'CONFIRMED').length;
  const activeHeldCount = myHolds.filter(h => h.status === 'HELD' && new Date(h.expiresAt) > new Date()).length;

  return (
    <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
      <div className="p-5 border-b border-gray-200 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-blue-50 text-blue-600 rounded-lg">
            <BookOpen className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900">Available Classes & Admission</h2>
            <p className="text-xs text-gray-500">Live seat availability polling every 3 seconds</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {(confirmedCount > 0 || activeHeldCount > 0) && (
            <div className="flex items-center gap-2 text-xs">
              {activeHeldCount > 0 && (
                <span className="px-2.5 py-1 bg-amber-50 text-amber-700 font-semibold rounded-full border border-amber-200">
                  {activeHeldCount} Active Hold
                </span>
              )}
              {confirmedCount > 0 && (
                <span className="px-2.5 py-1 bg-emerald-50 text-emerald-700 font-semibold rounded-full border border-emerald-200 flex items-center gap-1">
                  <CheckCircle2 className="w-3 h-3" /> {confirmedCount} Confirmed
                </span>
              )}
            </div>
          )}

          <button
            onClick={() => loadData(false)}
            className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition"
            title="Refresh classes now"
            aria-label="Refresh classes"
          >
            <RefreshCw className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-indigo-600' : ''}`} />
          </button>
        </div>
      </div>

      {loading ? (
        <div className="p-12 text-center text-gray-400">
          <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-2 text-indigo-500" />
          <p className="text-sm">Loading classes...</p>
        </div>
      ) : classes.length === 0 ? (
        <div className="p-12 text-center text-gray-500">
          <p className="font-medium">No classes configured yet.</p>
          <p className="text-xs text-gray-400 mt-1">Use the Admin form above to create the first class.</p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-50/75 border-b border-gray-200 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                <th className="py-3 px-4">Class</th>
                <th className="py-3 px-4 text-center">Total</th>
                <th className="py-3 px-4 text-center">Held</th>
                <th className="py-3 px-4 text-center">Confirmed</th>
                <th className="py-3 px-4 text-center">Available</th>
                <th className="py-3 px-4 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {classes.map((c) => (
                <ClassRow
                  key={c.id}
                  classItem={c}
                  hold={holdsByClass[c.id]}
                  onHoldUpdated={() => loadData(true)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination Controls */}
      <div className="p-4 bg-gray-50 border-t border-gray-200 flex items-center justify-between text-xs text-gray-500">
        <div>
          Showing page <span className="font-semibold text-gray-700">{page}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-3 py-1.5 bg-white border border-gray-300 rounded-md font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
          >
            <ChevronLeft className="w-3.5 h-3.5" /> Previous
          </button>
          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={classes.length < pageSize}
            className="px-3 py-1.5 bg-white border border-gray-300 rounded-md font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
          >
            Next <ChevronRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
};
