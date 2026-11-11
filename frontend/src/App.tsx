import React, { useState } from 'react';
import { StudentProvider } from './context/StudentContext';
import { Navbar, ActiveTab } from './components/Navbar';
import { StudentIdentityBar } from './components/StudentIdentityBar';
import { AdminCreateClass } from './components/AdminCreateClass';
import { ClassList } from './components/ClassList';
import { MyApplications } from './components/MyApplications';

export const AppContent: React.FC = () => {
  const [activeTab, setActiveTab] = useState<ActiveTab>('admission');
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  const handleClassCreated = () => {
    setRefreshTrigger((prev) => prev + 1);
  };

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <Navbar activeTab={activeTab} setActiveTab={setActiveTab} />

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <StudentIdentityBar />

        {activeTab === 'admission' && (
          <ClassList refreshTrigger={refreshTrigger} />
        )}

        {activeTab === 'admin' && (
          <div>
            <AdminCreateClass onClassCreated={handleClassCreated} />
            <ClassList refreshTrigger={refreshTrigger} />
          </div>
        )}

        {activeTab === 'my-applications' && (
          <MyApplications />
        )}
      </main>

      <footer className="bg-white border-t border-gray-200 py-6 text-center text-xs text-gray-400">
        School Admission System &bull; Built with .NET 10 LTS &bull; React 18 &bull; PostgreSQL 16
      </footer>
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <StudentProvider>
      <AppContent />
    </StudentProvider>
  );
};

export default App;
