import { useNavigate } from "react-router-dom";

export default function Forbidden() {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-6xl font-bold">403</h1>

        <h2 className="text-2xl font-semibold mt-4">
          Access Denied
        </h2>

        <p className="mt-2 text-gray-500">
          You do not have permission to access this page.
        </p>

        <button
          onClick={() => navigate("/dashboard")}
          className="mt-6 px-5 py-2 rounded-lg bg-black text-white"
        >
          Back to Dashboard
        </button>
      </div>
    </div>
  );
}