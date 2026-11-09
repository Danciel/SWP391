import { Link } from "react-router-dom";
import "./IdpDashboard.css";

export default function IdpDashboard() {
    return (
        <main className="idp-dashboard-page">
            <section className="idp-dashboard-card">
                <h1 className="idp-dashboard-title">Identity Provider Hub</h1>
                <p className="idp-dashboard-description">
                    Welcome to the Mock Identity Provider. Manage your profiles, verify faces, or access the admin panel.
                </p>

                <div className="idp-dashboard-grid">
                    <Link to="/idp/register" className="idp-dashboard-action">
                        <span className="idp-dashboard-icon">📝</span>
                        <div>
                            <h3>Register Profile</h3>
                            <p>Create a new identity profile with basic details.</p>
                        </div>
                    </Link>

                    <Link to="/idp/profile-lookup" className="idp-dashboard-action">
                        <span className="idp-dashboard-icon">🔍</span>
                        <div>
                            <h3>Lookup Profile</h3>
                            <p>Find an existing profile using its External ID.</p>
                        </div>
                    </Link>

                    <Link to="/idp/admin" className="idp-dashboard-action">
                        <span className="idp-dashboard-icon">⚙️</span>
                        <div>
                            <h3>Admin Dashboard</h3>
                            <p>Manage all IDP users, statuses, and facial enrollments.</p>
                        </div>
                    </Link>
                </div>
            </section>
        </main>
    );
}