import React from 'react';
import { School, GraduationCap, ShieldCheck, History } from 'lucide-react';

export type ActiveTab = 'admission' | 'admin' | 'my-applications';

interface NavbarProps {
  activeTab: ActiveTab;
  setActiveTab: (tab: ActiveTab) => void;
}

export const Navbar: React.FC<NavbarProps> = ({ activeTab, setActiveTab }) => {
  return (
    <header className="bg-white border-b border-gray-200 sticky top-0 z-30 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-indigo-600 text-white rounded-xl shadow-sm">
              <School className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-lg font-bold text-gray-900 leading-tight">School Admission System</h1>
              <p className="text-xs text-gray-500 font-medium">Real-Time Seat Hold & Enrollment</p>
            </div>
          </div>

          <nav className="flex items-center gap-1 bg-gray-100 p-1 rounded-xl">
            <button
              type="button"
              onClick={() => setActiveTab('admission')}
              className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition ${
                activeTab === 'admission'
                  ? 'bg-white text-indigo-700 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-white/50'
              }`}
            >
              <GraduationCap className="w-4 h-4" />
              <span>Admission Portal</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('admin')}
              className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition ${
                activeTab === 'admin'
                  ? 'bg-white text-indigo-700 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-white/50'
              }`}
            >
              <ShieldCheck className="w-4 h-4" />
              <span>School Admin</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('my-applications')}
              className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition ${
                activeTab === 'my-applications'
                  ? 'bg-white text-indigo-700 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-white/50'
              }`}
            >
              <History className="w-4 h-4" />
              <span>My Applications</span>
            </button>
          </nav>
        </div>
      </div>
    </header>
  );
};
