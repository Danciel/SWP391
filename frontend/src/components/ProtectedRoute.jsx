import { Navigate } from "react-router-dom";
import { getToken } from "../api/client";
import {useAuth} from "../context/AuthContext";


export default function ProtectedRoute({ 
  children,
   allowedRoles=[], 
  }) {
    const { user } = useAuth();
    
    const isAuthenticated = !!getToken();

  //Chua login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  //Da dang nhap nhung khong co user/role hop le
  if(!user){
    return <Navigate to="/login" replace />;
  }

  //Route khong yeu cau role cu the
  if (allowedRoles.length === 0) {
    return children;
  }

  //Kiem tra role 
  const hasPermission = allowedRoles.includes(user.role);

  //Khong dung role 
  if (!hasPermission) {
    return <Navigate to="/403" replace />;
  }
  
  return children;
}
