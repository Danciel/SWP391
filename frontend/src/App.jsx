import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider, useAuth } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import Login from "./pages/Login/Login";
import Register from "./pages/Register/Register";
import VerifyOtp from "./pages/VerifyOtp/VerifyOtp";
import ForgotPassword from "./pages/ForgotPassword/ForgotPassword";
import ResetPassword from "./pages/ResetPassword/ResetPassword";
import AppLayout from "./pages/Layout/AppLayout";
import Assets from "./pages/Assets/Assets";
import Profile from "./pages/Profile/Profile";
import Security from "./pages/Security/Security";
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
  OWNER: "assets",
  EXECUTOR: "executor",
  LEGAL_VERIFIER: "verifier",
  ADMIN: "admin",
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
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardIndex />} />
            <Route path="assets" element={<Assets />} />
            <Route path="profile" element={<Profile />} />
            <Route path="security" element={<Security />} />
            <Route path="verify-identity" element={<VerifyIdentity />} />
            <Route path="create-will" element={<CreateWill />} />
            <Route path="my-wills" element={<MyWills />} />
            <Route path="my-wills/:id" element={<WillDetails />} />
            <Route path="view-will" element={<ViewWill />} />
            <Route path="view-will/:id" element={<InheritedWillDetails />} />
            <Route path="executor" element={<ExecutorDashboard />} />
            <Route path="verifier" element={<LegalVerifierDashboard />} />
            <Route path="admin" element={<AdminDashboard />} />
          </Route>
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
