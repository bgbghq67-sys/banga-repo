"use client";

import { useState, useEffect } from "react";

interface Device {
  id: string;
  name: string;
  machineId: string | null;
  remainingSessions: number;
  activated: boolean;
  lastSeen: number | null;
}

export default function ControlCenterPage() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [loading, setLoading] = useState(true);
  const [sessionAmounts, setSessionAmounts] = useState<{ [key: string]: number }>({});
  const [addingSession, setAddingSession] = useState<string | null>(null);

  const fetchDevices = async (): Promise<void> => {
    try {
      const res = await fetch("/api/devices");
      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }
      const data = await res.json();
      if (data.ok) {
        setDevices(data.devices);
        // Initialize session amounts (preserve existing values)
        setSessionAmounts((prev) => {
          const amounts: { [key: string]: number } = { ...prev };
          data.devices.forEach((d: Device) => {
            if (!(d.id in amounts)) {
              amounts[d.id] = 1;
            }
          });
          return amounts;
        });
      }
    } catch (e) {
      console.error("Failed to fetch devices", e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDevices();
    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchDevices, 30000);
    return () => clearInterval(interval);
  }, []);

  const adjustAmount = (deviceId: string, delta: number) => {
    setSessionAmounts((prev) => ({
      ...prev,
      [deviceId]: Math.max(1, (prev[deviceId] || 1) + delta),
    }));
  };

  const addSessions = async (device: Device) => {
    const amount = sessionAmounts[device.id] || 1;
    setAddingSession(device.id);

    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout

      const res = await fetch(`/api/devices/${device.id}/add-sessions`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessions: amount }),
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const data = await res.json();
      if (data.ok) {
        await fetchDevices(); // Wait for refresh to complete
      } else {
        alert("Failed: " + (data.message || "Unknown error"));
      }
    } catch (e: unknown) {
      if (e instanceof Error && e.name === 'AbortError') {
        alert("Request timed out. Please try again.");
      } else {
        console.error("Error adding sessions:", e);
        alert("Error adding sessions. Please refresh and try again.");
      }
    } finally {
      setAddingSession(null);
    }
  };

  const getStatusInfo = (device: Device) => {
    if (!device.machineId) {
      return {
        color: "amber",
        label: "Pending Activation",
        icon: (
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        ),
      };
    }

    const isOnline = device.lastSeen && Date.now() - device.lastSeen < 5 * 60 * 1000;
    if (isOnline) {
      return {
        color: "emerald",
        label: "Online",
        icon: (
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        ),
      };
    }

    return {
      color: "slate",
      label: "Offline",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 5.636a9 9 0 010 12.728m0 0l-2.829-2.829m2.829 2.829L21 21M15.536 8.464a5 5 0 010 7.072m0 0l-2.829-2.829m-4.243 2.829a4.978 4.978 0 01-1.414-2.83m-1.414 5.658a9 9 0 01-2.167-9.238m7.824 2.167a1 1 0 111.414 1.414m-1.414-1.414L3 3" />
        </svg>
      ),
    };
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
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-slate-800">Control Center</h1>
        <p className="text-slate-500 mt-1">Manage sessions for each device</p>
      </div>

      {/* Device Cards Grid */}
      {devices.length === 0 ? (
        <div className="bg-white rounded-2xl shadow-xl p-12 text-center">
          <div className="w-16 h-16 mx-auto mb-4 bg-slate-100 rounded-full flex items-center justify-center">
            <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-slate-700 mb-2">No devices yet</h3>
          <p className="text-slate-400">Add devices from the Devices page first.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {devices.map((device) => {
            const status = getStatusInfo(device);
            const amount = sessionAmounts[device.id] || 10;
            const isAdding = addingSession === device.id;

            return (
              <div
                key={device.id}
                className="bg-white rounded-2xl shadow-xl overflow-hidden hover:shadow-2xl transition-shadow"
              >
                {/* Card Header */}
                <div className={`px-6 py-4 bg-gradient-to-r ${
                  status.color === "emerald" ? "from-emerald-500 to-teal-500" :
                  status.color === "amber" ? "from-amber-500 to-orange-500" :
                  "from-slate-500 to-slate-600"
                }`}>
                  <div className="flex items-center justify-between text-white">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-white/20 rounded-xl flex items-center justify-center">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                        </svg>
                      </div>
                      <div>
                        <h3 className="font-bold text-lg">{device.name}</h3>
                        <div className="flex items-center gap-1.5 text-white/80 text-sm">
                          {status.icon}
                          <span>{status.label}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Card Body */}
                <div className="p-6">
                  {/* Waiting for App */}
                  {!device.machineId && (
                    <div className="mb-6 p-4 bg-amber-50 rounded-xl border border-amber-200">
                      <div className="text-sm text-amber-600 mb-1">Status</div>
                      <div className="text-lg font-semibold text-amber-700">
                        Waiting for app to connect...
                      </div>
                    </div>
                  )}

                  {/* Sessions Display */}
                  <div className="text-center mb-6">
                    <div className="text-sm text-slate-400 uppercase tracking-wider mb-1">Remaining Sessions</div>
                    <div className="text-5xl font-bold text-slate-800 font-mono">
                      {device.remainingSessions}
                    </div>
                  </div>

                  {/* Session Control */}
                  <div className="bg-slate-50 rounded-xl p-4 mb-4">
                    <div className="text-sm text-slate-400 text-center mb-3">Add Sessions</div>
                    <div className="flex items-center justify-center gap-4">
                      <button
                        onClick={() => adjustAmount(device.id, -1)}
                        className="w-12 h-12 rounded-full bg-white border border-slate-200 text-slate-600 text-xl font-bold hover:bg-slate-100 transition-colors flex items-center justify-center shadow-sm"
                      >
                        -
                      </button>
                      <div className="text-4xl font-mono font-bold text-amber-600 w-20 text-center">
                        {amount}
                      </div>
                      <button
                        onClick={() => adjustAmount(device.id, 1)}
                        className="w-12 h-12 rounded-full bg-white border border-slate-200 text-slate-600 text-xl font-bold hover:bg-slate-100 transition-colors flex items-center justify-center shadow-sm"
                      >
                        +
                      </button>
                    </div>
                  </div>

                  {/* Add Button */}
                  <button
                    onClick={() => addSessions(device)}
                    disabled={isAdding}
                    className="w-full py-4 rounded-xl bg-gradient-to-r from-emerald-500 to-teal-500 text-white font-bold text-lg shadow-lg shadow-emerald-500/30 hover:shadow-xl hover:shadow-emerald-500/40 transition-all transform active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2"
                  >
                    {isAdding ? (
                      <svg className="animate-spin h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                    ) : (
                      <>
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                        </svg>
                        ADD SESSIONS
                      </>
                    )}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

