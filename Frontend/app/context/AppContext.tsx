import React, { createContext, useContext, useState, type ReactNode } from 'react';
import type { MemberResponseDto } from '~/api';

interface AppContextType {
  boardGroupId: number | null;
  setBoardGroupId: (id: number) => void;
  candidateBoardGroupId: number | null;
  setCandidateBoardGroupId: (id: number) => void;
  member: MemberResponseDto | null;
  setMember: React.Dispatch<React.SetStateAction<MemberResponseDto | null>>;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export const AppProvider = ({ children }: { children: ReactNode }) => {
  const [boardGroupId, setBoardGroupId] = useState<number | null>(null);
  const [candidateBoardGroupId, setCandidateBoardGroupId] = useState<number | null>(null);
  const [member, setMember] = useState<MemberResponseDto | null>(null);

  return (
    <AppContext.Provider value={{ boardGroupId, setBoardGroupId, candidateBoardGroupId, setCandidateBoardGroupId, member, setMember }}>
      {children}
    </AppContext.Provider>
  );
};

export const useApp = () => {
  const context = useContext(AppContext);
  if (!context) throw new Error("useApp must be used within AppProvider");
  return context;
};