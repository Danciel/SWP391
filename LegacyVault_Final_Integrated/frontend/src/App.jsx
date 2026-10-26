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
import VerifyIdentity from "./pages/VerifyIdentity/VerifyIdentity";
import CreateWill from "./pages/CreateWill/CreateWill";
import MyWills from "./pages/MyWills/MyWills";
import WillDetails from "./pages/WillDetails/WillDetails";
import ViewWill from "./pages/ViewWill/ViewWill";
import Wallet from "./pages/Wallet/Wallet";
import InheritedWillDetails from "./pages/InheritedWillDetails/InheritedWillDetails";
import ExecutorDashboard from "./pages/ExecutorDashboard/ExecutorDashboard";
import LegalVerifierDashboard from "./pages/LegalVerifierDashboard/LegalVerifierDashboard";
import AdminDashboard from "./pages/AdminDashboard/AdminDashboard";
import FaceRecognitionTestPage from "./pages/FaceRecognitionTestPage/FaceRecognitionTestPage";
import IdpRegister from "./pages/IdpRegister/IdpRegister";
import IdpProfile from "./pages/IdpProfile/IdpProfile";
import IdpProfileLookup from "./pages/IdpProfileLookup/IdpProfileLookup";
import IdpFaceEnroll from "./pages/IdpFaceEnroll/IdpFaceEnroll";
import IdpAdminDashboard from "./pages/IdpAdminDashboard/IdpAdminDashboard";
import IdpDashboard from "./pages/IdpDashboard/IdpDashboard";

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

          <Route path="/idp" element={<IdpDashboard />} />
          <Route path="/idp/register" element={<IdpRegister />} />
          <Route path="/idp/profile/:id" element={<IdpProfile />}/>
          <Route path="/idp/profile-lookup" element={<IdpProfileLookup />}/>
          <Route path="/idp/face-enroll/:id" element={<IdpFaceEnroll />} />
          <Route path="/idp/admin" element={<IdpAdminDashboard />} />

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
            <Route path="verify-identity" element={<VerifyIdentity />} />
            <Route path="create-will" element={<CreateWill />} />
            <Route path="my-wills" element={<MyWills />} />
            <Route path="my-wills/:id" element={<WillDetails />} />
            <Route path="wallet" element={<Wallet />} />
            <Route path="view-will" element={<ViewWill />} />
            <Route path="view-will/:id" element={<InheritedWillDetails />} />
            <Route path="executor" element={<ExecutorDashboard />} />
            <Route path="verifier" element={<LegalVerifierDashboard />} />
            <Route path="admin" element={<AdminDashboard />} />
            <Route path="admin/face-test" element={<FaceRecognitionTestPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
