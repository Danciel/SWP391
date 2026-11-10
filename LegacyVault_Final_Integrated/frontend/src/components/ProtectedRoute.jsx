import { Navigate } from "react-router-dom";
import { getToken } from "../api/client";

export default function ProtectedRoute({ children }) {
  const isAuthenticated = !!getToken();
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return children;
}
