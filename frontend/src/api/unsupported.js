import { ApiError } from "./client";
// Missing backend functionality must never be simulated or saved as fake data.
export const unsupported = (feature) => async () => { throw new ApiError(`${feature} is not implemented in the backend yet.`,501); };
