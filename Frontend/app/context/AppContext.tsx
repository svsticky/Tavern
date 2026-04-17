import React, { createContext, useContext, useState, type ReactNode } from 'react';

interface AppContextType {
  boardGroupId: number | null;
  setBoardGroupId: (id: number) => void;
  candidateBoardGroupId: number | null;
  setCandidateBoardGroupId: (id: number) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export const AppProvider = ({ children }: { children: ReactNode }) => {
  const [boardGroupId, setBoardGroupId] = useState<number | null>(null);
  const [candidateBoardGroupId, setCandidateBoardGroupId] = useState<number | null>(null);

  return (
    <AppContext.Provider value={{ boardGroupId, setBoardGroupId, candidateBoardGroupId, setCandidateBoardGroupId }}>
      {children}
    </AppContext.Provider>
  );
};

export const useApp = () => {
  const context = useContext(AppContext);
  if (!context) throw new Error("useApp must be used within AppProvider");
  return context;
};