import React, { createContext, useContext } from 'react';
import type { IAuthService } from '~/types/IAuthService';

const AuthContext = createContext<IAuthService | null>(null);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

export default AuthContext;