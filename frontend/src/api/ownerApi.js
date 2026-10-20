import {getOwnerDashboardData,checkInDms,cancelDeathHandover,requestImmediateHandover} from "./vaultApi";
export const getOwnerDashboard=getOwnerDashboardData;
export const confirmStillActive=({userId,willId})=>checkInDms(userId,willId);
export const cancelHandover=({userId,willId})=>cancelDeathHandover(userId,willId);
export const requestHandover=({userId,willId})=>requestImmediateHandover(userId,willId);
