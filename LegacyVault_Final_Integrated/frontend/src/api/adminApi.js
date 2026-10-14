import { unsupported } from "./unsupported";
export const getAdminDashboard=async()=>({accounts:[],cases:[],payments:[],auditLog:[], stats:{totalUsers:0,totalWills:0,totalAssets:0},recentActivity:[]});
export const setAccountStatus=unsupported("Admin account status");
export const setAccountRole=unsupported("Admin role management");
