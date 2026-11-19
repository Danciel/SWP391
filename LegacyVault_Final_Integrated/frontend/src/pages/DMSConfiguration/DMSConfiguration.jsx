
import React, { useEffect, useState } from "react";
import {
  getAdminDmsRule,
  createAdminDmsRule,
  updateAdminDmsRule,
  validateDmsRuleInput,
} from "../../api/dmsRule";

import "./DMSConfiguration.css";

const INITIAL_CONFIG = {
  reminderBeforeDays: 3,
  maxMissedCheckIns: 2,
  graceDays: 0,
  minCheckInIntervalDays: 7,
  maxCheckInIntervalDays: 365,
};

export default function DMSConfiguration() {
  const [config, setConfig] = useState(INITIAL_CONFIG);
  const [ruleExists, setRuleExists] = useState(false);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  // Load global DMS configuration from Backend
  useEffect(() => {
    let mounted = true;

    const loadConfiguration = async () => {
      try {
        setLoading(true);
        setError("");

        const result = await getAdminDmsRule();

        if (!mounted) return;

        if (result) {
          setConfig({
            reminderBeforeDays: result.reminderBeforeDays,
            maxMissedCheckIns: result.maxMissedCheckIns,
            graceDays: result.graceDays,
            minCheckInIntervalDays:
              result.minCheckInIntervalDays,
            maxCheckInIntervalDays:
              result.maxCheckInIntervalDays,
          });

          setRuleExists(true);
        } else {
          // Backend returns null when no rule exists
          setConfig(INITIAL_CONFIG);
          setRuleExists(false);
        }
      } catch (err) {
        if (mounted) {
          setError(
            err?.message || "Unable to load DMS configuration."
          );
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    };

    loadConfiguration();

    return () => {
      mounted = false;
    };
  }, []);

  const handleChange = (field, value) => {
    setConfig((prev) => ({
      ...prev,
      [field]: value === "" ? "" : Number(value),
    }));

    setError("");
    setSuccess("");
  };

  // Create first rule or update existing rule
  const handleSave = async (e) => {
    e.preventDefault();

    setError("");
    setSuccess("");

    try {
      const payload = {
        reminderBeforeDays: config.reminderBeforeDays,
        maxMissedCheckIns: config.maxMissedCheckIns,
        graceDays: config.graceDays,
        minCheckInIntervalDays:
          config.minCheckInIntervalDays,
        maxCheckInIntervalDays:
          config.maxCheckInIntervalDays,
      };

      // Client-side validation
      validateDmsRuleInput(payload);

      setSaving(true);

      let result;

      if (ruleExists) {
        result = await updateAdminDmsRule(payload);
      } else {
        result = await createAdminDmsRule(payload);
      }

      // Update form with persisted Backend data
      if (result) {
        setConfig({
          reminderBeforeDays: result.reminderBeforeDays,
          maxMissedCheckIns: result.maxMissedCheckIns,
          graceDays: result.graceDays,
          minCheckInIntervalDays:
            result.minCheckInIntervalDays,
          maxCheckInIntervalDays:
            result.maxCheckInIntervalDays,
        });
      }

      setRuleExists(true);
      setSuccess("DMS configuration saved successfully.");
    } catch (err) {
      setError(
        err?.message || "Failed to save DMS configuration."
      );
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="dms-config-page">
        <div className="dms-loading">
          Loading DMS configuration...
        </div>
      </div>
    );
  }

  return (
    <div className="dms-config-page">
      <div className="dms-config-header">
        <div>
          <h2>DMS Configuration</h2>
          <p>
            Manage Dead Man's Switch settings for the entire system.
          </p>
        </div>

        <span className="dms-global-badge">
          Global Configuration
        </span>
      </div>

      <form
        className="dms-config-container"
        onSubmit={handleSave}
      >
        <div className="dms-global-notice">
          <span>ⓘ</span>
          These settings apply to all users and all Wills
          across the LegacyVault system.
        </div>

        <section className="dms-config-card">
          <div className="dms-section-heading">
            <h3>Dead Man's Switch</h3>
            <p>
              Configure global check-in policies and conditions
              for the entire system.
            </p>
          </div>

          <div className="dms-form-grid">
            <div className="dms-form-group">
              <label>Minimum Check-in Interval (days)</label>
              <input
                type="number"
                min="1"
                max="3650"
                step="1"
                required
                value={config.minCheckInIntervalDays}
                onChange={(e) =>
                  handleChange(
                    "minCheckInIntervalDays",
                    e.target.value
                  )
                }
              />
              <small>Minimum allowed check-in interval.</small>
            </div>

            <div className="dms-form-group">
              <label>Maximum Check-in Interval (days)</label>
              <input
                type="number"
                min="1"
                max="3650"
                step="1"
                required
                value={config.maxCheckInIntervalDays}
                onChange={(e) =>
                  handleChange(
                    "maxCheckInIntervalDays",
                    e.target.value
                  )
                }
              />
              <small>Maximum allowed check-in interval.</small>
            </div>

            <div className="dms-form-group">
              <label>Reminder Before Due Date (days)</label>
              <input
                type="number"
                min="0"
                max="3649"
                step="1"
                required
                value={config.reminderBeforeDays}
                onChange={(e) =>
                  handleChange(
                    "reminderBeforeDays",
                    e.target.value
                  )
                }
              />
            </div>

            <div className="dms-form-group">
              <label>Maximum Missed Check-ins</label>
              <input
                type="number"
                min="1"
                max="100"
                step="1"
                required
                value={config.maxMissedCheckIns}
                onChange={(e) =>
                  handleChange(
                    "maxMissedCheckIns",
                    e.target.value
                  )
                }
              />
            </div>

            <div className="dms-form-group">
              <label>Grace Period (days)</label>
              <input
                type="number"
                min="0"
                max="30"
                step="1"
                required
                value={config.graceDays}
                onChange={(e) =>
                  handleChange("graceDays", e.target.value)
                }
              />
              <small>Allowed range: 0 to 30 days.</small>
            </div>
          </div>

          <div className="dms-info-notice">
            <span>ⓘ</span>
            <p>
              Owners can select a check-in interval within the
              minimum and maximum values configured by Admin.
              The reminder period must be shorter than the
              minimum check-in interval.
              DMS does not authorize asset handover on its own.
            </p>
          </div>
        </section>

        {error && (
          <div className="dms-message dms-error">
            {error}
          </div>
        )}

        {success && (
          <div className="dms-message dms-success">
            {success}
          </div>
        )}

        <div className="dms-config-actions">
          <button
            type="submit"
            className="dms-save-button"
            disabled={saving}
          >
            {saving
              ? "Saving..."
              : ruleExists
                ? "Update Configuration"
                : "Create Configuration"}
          </button>
        </div>
      </form>
    </div>
  );
}
