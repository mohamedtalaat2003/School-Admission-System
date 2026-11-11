import React, { useState } from 'react';
import { useStudent } from '../context/StudentContext';
import { UserCheck, AlertTriangle, KeyRound, Sparkles } from 'lucide-react';

export const StudentIdentityBar: React.FC = () => {
  const { studentId, setStudentId, isValid } = useStudent();
  const [inputValue, setInputValue] = useState(studentId);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value.replace(/\D/g, '').slice(0, 17);
    setInputValue(val);
    setStudentId(val);
  };

  const handlePreset = (presetId: string) => {
    setInputValue(presetId);
    setStudentId(presetId);
  };

  return (
    <div className="bg-white border border-gray-200 rounded-xl shadow-sm p-4 mb-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className={`p-2.5 rounded-lg ${isValid ? 'bg-emerald-50 text-emerald-600' : 'bg-amber-50 text-amber-600'}`}>
            <KeyRound className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-semibold text-gray-900 text-sm">Student Birth Registration ID</span>
              {isValid ? (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-emerald-100 text-emerald-800">
                  <UserCheck className="w-3.5 h-3.5" /> 17-digit Valid
                </span>
              ) : (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-800">
                  <AlertTriangle className="w-3.5 h-3.5" /> {inputValue.length}/17 digits
                </span>
              )}
            </div>
            <p className="text-xs text-gray-500 mt-0.5">
              Sent in the <code className="bg-gray-100 px-1 py-0.5 rounded text-gray-700">X-Student-Id</code> HTTP header for holds and payments.
            </p>
          </div>
        </div>

        <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
          <div className="relative">
            <input
              type="text"
              value={inputValue}
              onChange={handleChange}
              placeholder="e.g. 20152691234567890"
              className={`w-full sm:w-64 font-mono text-sm px-3.5 py-2 border rounded-lg focus:outline-none focus:ring-2 transition-all ${
                isValid
                  ? 'border-gray-300 focus:border-emerald-500 focus:ring-emerald-200'
                  : 'border-amber-300 focus:border-amber-500 focus:ring-amber-200 bg-amber-50/20'
              }`}
              maxLength={17}
              aria-label="Student Birth Registration ID"
            />
          </div>

          <div className="flex items-center gap-1 text-xs">
            <span className="text-gray-400 mr-1 hidden sm:inline flex items-center gap-1">
              <Sparkles className="w-3 h-3" /> Quick Switch:
            </span>
            <button
              type="button"
              onClick={() => handlePreset('20152691234567890')}
              className="px-2 py-1 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded text-xs transition"
              title="Student 1"
            >
              #1
            </button>
            <button
              type="button"
              onClick={() => handlePreset('20152691234567891')}
              className="px-2 py-1 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded text-xs transition"
              title="Student 2"
            >
              #2
            </button>
            <button
              type="button"
              onClick={() => handlePreset('20152691234567892')}
              className="px-2 py-1 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded text-xs transition"
              title="Student 3"
            >
              #3
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
