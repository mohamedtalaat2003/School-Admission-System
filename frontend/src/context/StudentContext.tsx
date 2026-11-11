import React, { createContext, useContext, useState } from 'react';

interface StudentContextType {
  studentId: string;
  setStudentId: (id: string) => void;
  isValid: boolean;
  clearStudentId: () => void;
}

const StudentContext = createContext<StudentContextType | undefined>(undefined);

export const STUDENT_ID_REGEX = /^\d{17}$/;

export const StudentProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [studentId, setStudentIdState] = useState<string>(() => {
    return localStorage.getItem('school_admission_student_id') || '20152691234567890';
  });

  const isValid = STUDENT_ID_REGEX.test(studentId.trim());

  const setStudentId = (id: string) => {
    const trimmed = id.trim();
    setStudentIdState(trimmed);
    localStorage.setItem('school_admission_student_id', trimmed);
  };

  const clearStudentId = () => {
    setStudentIdState('');
    localStorage.removeItem('school_admission_student_id');
  };

  return (
    <StudentContext.Provider value={{ studentId, setStudentId, isValid, clearStudentId }}>
      {children}
    </StudentContext.Provider>
  );
};

export const useStudent = (): StudentContextType => {
  const context = useContext(StudentContext);
  if (!context) {
    throw new Error('useStudent must be used within a StudentProvider');
  }
  return context;
};
