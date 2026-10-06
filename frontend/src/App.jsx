import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider, useAuth } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import { ROLES } from "./constants/roles";
import Forbidden from "./pages/Forbidden/Forbidden";
import Login from "./pages/Login/Login";
import Register from "./pages/Register/Register";
import VerifyOtp from "./pages/VerifyOtp/VerifyOtp";
import ForgotPassword from "./pages/ForgotPassword/ForgotPassword";
import ResetPassword from "./pages/ResetPassword/ResetPassword";
import AppLayout from "./pages/Layout/AppLayout";
import Assets from "./pages/Assets/Assets";
import Profile from "./pages/Profile/Profile";
import VerifyIdentity from "./pages/VerifyIdentity/VerifyIdentity";
import CreateWill from "./pages/CreateWill/CreateWill";
import MyWills from "./pages/MyWills/MyWills";
import WillDetails from "./pages/WillDetails/WillDetails";
import ViewWill from "./pages/ViewWill/ViewWill";
import InheritedWillDetails from "./pages/InheritedWillDetails/InheritedWillDetails";
import ExecutorDashboard from "./pages/ExecutorDashboard/ExecutorDashboard";
import LegalVerifierDashboard from "./pages/LegalVerifierDashboard/LegalVerifierDashboard";
import AdminDashboard from "./pages/AdminDashboard/AdminDashboard";

const ROLE_HOME = {
  [ROLES.OWNER]: "assets",
  [ROLES.EXECUTOR]: "executor",
  [ROLES.BENEFICIARY]: "view-will",
  [ROLES.LEGAL_VERIFIER]: "verifier",
  [ROLES.ADMIN]: "admin",
};

function DashboardIndex() {
  const { user } = useAuth();
  return <Navigate to={ROLE_HOME[user?.role] || "assets"} replace />;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/verify-otp" element={<VerifyOtp />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="/403" element={<Forbidden />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardIndex />} />
            <Route
              path="assets"
              element={
                <ProtectedRoute allowedRoles={[ROLES.OWNER]}>
                  <Assets />
                </ProtectedRoute>
              }
            />

            <Route
              path="profile"
              element={
                <ProtectedRoute>
                  <Profile />
                </ProtectedRoute>
              }
            />
            <Route
              path="verify-identity"
              element={
                <ProtectedRoute allowedRoles={[ROLES.OWNER]}>
                  <VerifyIdentity />
                </ProtectedRoute>
              }
            />

            <Route
              path="create-will"
              element={
                <ProtectedRoute allowedRoles={[ROLES.OWNER]}>
                  <CreateWill />
                </ProtectedRoute>
              }
            />

            <Route
              path="my-wills"
              element={
                <ProtectedRoute allowedRoles={[ROLES.OWNER]}>
                  <MyWills />
                </ProtectedRoute>
              }
            />

            <Route
              path="my-wills/:id"
              element={
                <ProtectedRoute allowedRoles={[ROLES.OWNER]}>
                  <WillDetails />
                </ProtectedRoute>
              }
            />

            <Route
              path="view-will"
              element={
                <ProtectedRoute allowedRoles={[ROLES.BENEFICIARY]}>
                  <ViewWill />
                </ProtectedRoute>
              }
            />

            <Route
              path="view-will/:id"
              element={
                <ProtectedRoute allowedRoles={[ROLES.BENEFICIARY]}>
                  <InheritedWillDetails />
                </ProtectedRoute>
              }
            />

            <Route
              path="executor"
              element={
                <ProtectedRoute allowedRoles={[ROLES.EXECUTOR]}>
                  <ExecutorDashboard />
                </ProtectedRoute>
              }
            />

            <Route
              path="verifier"
              element={
                <ProtectedRoute allowedRoles={[ROLES.LEGAL_VERIFIER]}>
                  <LegalVerifierDashboard />
                </ProtectedRoute>
              }
            />

            <Route
              path="admin"
              element={
                <ProtectedRoute allowedRoles={[ROLES.ADMIN]}>
                  <AdminDashboard />
                </ProtectedRoute>
              }
            />
          </Route>
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
