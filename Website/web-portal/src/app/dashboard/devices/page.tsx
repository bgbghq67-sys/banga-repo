"use client";

import { useState, useEffect } from "react";

interface Device {
  id: string;
  name: string;
  machineId: string | null;
  remainingSessions: number;
  activated: boolean;
  createdAt: number;
  lastSeen: number | null;
}

export default function DevicesPage() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editDevice, setEditDevice] = useState<Device | null>(null);
  const [formData, setFormData] = useState({ name: "", sessions: 100 });
  const [saving, setSaving] = useState(false);

  const fetchDevices = async () => {
    try {
      const res = await fetch("/api/devices");
      const data = await res.json();
      if (data.ok) {
        setDevices(data.devices);
      }
    } catch (e) {
      console.error("Failed to fetch devices", e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDevices();
  }, []);

  const openAddModal = () => {
    setEditDevice(null);
    setFormData({ name: "", sessions: 100 });
    setShowModal(true);
  };

  const openEditModal = (device: Device) => {
    setEditDevice(device);
    setFormData({ name: device.name, sessions: device.remainingSessions });
    setShowModal(true);
  };

  const handleSave = async () => {
    if (!formData.name.trim()) {
      alert("Device name is required");
      return;
    }

    setSaving(true);
    try {
      const url = editDevice ? `/api/devices/${editDevice.id}` : "/api/devices";
      const method = editDevice ? "PUT" : "POST";

      const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formData.name,
          remainingSessions: formData.sessions,
        }),
      });

      const data = await res.json();
      if (data.ok) {
        setShowModal(false);
        fetchDevices();
      } else {
        alert("Failed: " + data.message);
      }
    } catch (e) {
      alert("Error saving device");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (device: Device) => {
    if (!confirm(`Are you sure you want to delete "${device.name}"?`)) return;

    try {
      const res = await fetch(`/api/devices/${device.id}`, { method: "DELETE" });
      const data = await res.json();
      if (data.ok) {
        fetchDevices();
      } else {
        alert("Failed: " + data.message);
      }
    } catch (e) {
      alert("Error deleting device");
    }
  };

  const resetMachine = async (device: Device) => {
    if (!confirm(`Reset machine binding for "${device.name}"? This device will need to be re-activated.`)) return;

    try {
      const res = await fetch(`/api/devices/${device.id}/reset`, { method: "POST" });
      const data = await res.json();
      if (data.ok) {
        fetchDevices();
      } else {
        alert("Failed: " + data.message);
      }
    } catch (e) {
      alert("Error resetting machine");
    }
  };

  const getStatusBadge = (device: Device) => {
    if (!device.machineId) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-amber-100 text-amber-700">
          <span className="w-2 h-2 rounded-full bg-amber-400"></span>
          Pending
        </span>
      );
    }
    
    const isOnline = device.lastSeen && Date.now() - device.lastSeen < 5 * 60 * 1000; // 5 min
    if (isOnline) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">
          <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
          Online
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-600">
        <span className="w-2 h-2 rounded-full bg-slate-400"></span>
        Offline
      </span>
    );
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-amber-500"></div>
      </div>
    );
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-slate-800">Devices</h1>
          <p className="text-slate-500 mt-1">Manage your photobooth devices</p>
        </div>
        <button
          onClick={openAddModal}
          className="flex items-center gap-2 bg-gradient-to-r from-amber-500 to-orange-500 text-white px-6 py-3 rounded-xl font-semibold shadow-lg shadow-orange-500/30 hover:shadow-xl hover:shadow-orange-500/40 transition-all transform hover:scale-105"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Add Device
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-xl overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="bg-slate-50 border-b border-slate-200">
              <th className="text-left px-6 py-4 text-sm font-semibold text-slate-600">Name</th>
              <th className="text-left px-6 py-4 text-sm font-semibold text-slate-600">Machine ID</th>
              <th className="text-left px-6 py-4 text-sm font-semibold text-slate-600">Sessions</th>
              <th className="text-left px-6 py-4 text-sm font-semibold text-slate-600">Status</th>
              <th className="text-right px-6 py-4 text-sm font-semibold text-slate-600">Actions</th>
            </tr>
          </thead>
          <tbody>
            {devices.length === 0 ? (
              <tr>
                <td colSpan={5} className="text-center py-12 text-slate-400">
                  No devices yet. Devices will appear here when the app connects.
                </td>
              </tr>
            ) : (
              devices.map((device) => (
                <tr key={device.id} className="border-b border-slate-100 hover:bg-slate-50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="font-semibold text-slate-800">{device.name}</div>
                  </td>
                  <td className="px-6 py-4">
                    <code className="text-sm bg-slate-100 px-2 py-1 rounded text-slate-600">
                      {device.machineId ? `${device.machineId.substring(0, 16)}` : "Waiting..."}
                    </code>
                  </td>
                  <td className="px-6 py-4">
                    <span className="font-mono text-lg font-bold text-slate-700">{device.remainingSessions}</span>
                  </td>
                  <td className="px-6 py-4">{getStatusBadge(device)}</td>
                  <td className="px-6 py-4">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => openEditModal(device)}
                        className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                        title="Edit"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                      {device.machineId && (
                        <button
                          onClick={() => resetMachine(device)}
                          className="p-2 text-slate-400 hover:text-purple-600 hover:bg-purple-50 rounded-lg transition-colors"
                          title="Reset Machine Binding"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                          </svg>
                        </button>
                      )}
                      <button
                        onClick={() => handleDelete(device)}
                        className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                        title="Delete"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-8 w-full max-w-md shadow-2xl">
            <h2 className="text-2xl font-bold text-slate-800 mb-6">
              {editDevice ? "Edit Device" : "Add New Device"}
            </h2>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Device Name</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Seoul Store"
                  className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:border-amber-500 focus:ring-2 focus:ring-amber-500/20 outline-none transition-all"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Initial Sessions</label>
                <input
                  type="number"
                  value={formData.sessions}
                  onChange={(e) => setFormData({ ...formData, sessions: parseInt(e.target.value) || 0 })}
                  min={0}
                  className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:border-amber-500 focus:ring-2 focus:ring-amber-500/20 outline-none transition-all"
                />
              </div>
            </div>

            <div className="flex gap-3 mt-8">
              <button
                onClick={() => setShowModal(false)}
                className="flex-1 px-6 py-3 rounded-xl border border-slate-200 text-slate-600 font-medium hover:bg-slate-50 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleSave}
                disabled={saving}
                className="flex-1 px-6 py-3 rounded-xl bg-gradient-to-r from-amber-500 to-orange-500 text-white font-semibold shadow-lg shadow-orange-500/30 hover:shadow-xl transition-all disabled:opacity-50"
              >
                {saving ? "Saving..." : "Save"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

